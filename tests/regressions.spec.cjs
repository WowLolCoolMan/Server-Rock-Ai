const { test, expect } = require("@playwright/test");
const path = require("node:path");
const fs = require("node:fs/promises");
const os = require("node:os");
const { pathToFileURL } = require("node:url");
const { createWorkspace } = require("../electron/workspace.cjs");
const { createProviderBridge } = require("../electron/provider-bridge.cjs");
const { createSafeChildEnvironment } = require("../electron/child-environment.cjs");
const { resolveModelFilePath, ensureNoSymlinkComponents } = require("../electron/model-path-safety.cjs");
const { validateRequestId, validateChatGPTRequest, validateProviderName } = require("../electron/ipc-validation.cjs");
const { MAX_ENCRYPTED_STATE_BYTES, readTextFileLimited } = require("../electron/bounded-file.cjs");
const {
  MAX_JSON_BODY_BYTES,
  MAX_TEXT_BODY_BYTES,
  MAX_BINARY_BODY_BYTES,
  MAX_STREAM_TEXT_LENGTH,
  readJsonBodyLimited,
  readTextBodyLimited,
  readBinaryBodyLimited,
} = require("../electron/response-limits.cjs");

const appUrl = pathToFileURL(path.resolve(__dirname, "..", "index.html")).href;

test("malformed local state is preserved and the app still opens", async ({ page }) => {
  await page.goto(appUrl);
  await page.evaluate(() => localStorage.setItem("local-studio-v1", "{broken"));
  await page.reload();

  await expect(page.locator("#welcome")).toBeVisible();
  await expect.poll(() => page.evaluate(() => Boolean(localStorage.getItem("local-studio-v1-corrupt")))).toBe(true);
});

test("oversized local state is rejected before parsing", async ({ page }) => {
  await page.goto(appUrl);
  await page.evaluate(() => localStorage.setItem("local-studio-v1", "x".repeat(4_000_001)));
  await page.reload();
  await expect(page.locator("#welcome")).toBeVisible();
  await expect.poll(() => page.evaluate(() => Boolean(localStorage.getItem("local-studio-v1-corrupt")))).toBe(true);
});

test("local history falls back to text-only data when storage is full", async ({ page }) => {
  await page.goto(appUrl);
  const result = await page.evaluate(() => {
    const original = Storage.prototype.setItem;
    let firstWrite = true;
    Storage.prototype.setItem = function (key, value) {
      if (key === "local-studio-v1" && firstWrite) {
        firstWrite = false;
        throw new DOMException("full", "QuotaExceededError");
      }
      return original.call(this, key, value);
    };
    const saved = window.persist();
    return {
      saved,
      stored: Boolean(localStorage.getItem("local-studio-v1")),
      status: document.querySelector("#runtimeStatus")?.textContent || "",
    };
  });

  expect(result.saved).toBe(false);
  expect(result.stored).toBe(true);
  expect(result.status).toContain("without generated media");
});

test("translate slash commands expose the translation action", async () => {
  const { parseSlashCommands } = require("../commands.js");
  expect(parseSlashCommands("/translate hello")).toMatchObject({
    text: "hello",
    translate: true,
  });
});

test("workspace reads fail cleanly when an authorized folder disappears", async () => {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-root-"));
  const profile = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-profile-"));
  try {
    const workspace = createWorkspace(profile);
    const approved = await workspace.approveFolder(root);
    await fs.rm(root, { recursive: true, force: true });
    await expect(workspace.readFiles([approved])).rejects.toThrow("Use Add a folder");
  } finally {
    await fs.rm(root, { recursive: true, force: true });
    await fs.rm(profile, { recursive: true, force: true });
  }
});

test("malformed authorized-folder data is quarantined", async () => {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-root-"));
  const profile = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-profile-"));
  try {
    const workspace = createWorkspace(profile);
    const approved = await workspace.approveFolder(root);
    await fs.writeFile(path.join(profile, "authorized-project-folders.json"), "{broken", "utf8");
    const reloadedWorkspace = createWorkspace(profile);
    await expect(reloadedWorkspace.readFiles([approved])).rejects.toThrow("Use Add a folder");
    const profileFiles = await fs.readdir(profile);
    expect(profileFiles.some((name) => name.startsWith("authorized-project-folders.json.corrupt-"))).toBe(true);
  } finally {
    await fs.rm(root, { recursive: true, force: true });
    await fs.rm(profile, { recursive: true, force: true });
  }
});

test("oversized authorized-folder data is quarantined before parsing", async () => {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-root-"));
  const profile = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-profile-"));
  try {
    const workspace = createWorkspace(profile);
    const approved = await workspace.approveFolder(root);
    await fs.writeFile(path.join(profile, "authorized-project-folders.json"), "x".repeat(64 * 1024 + 1), "utf8");
    const reloadedWorkspace = createWorkspace(profile);
    await expect(reloadedWorkspace.readFiles([approved])).rejects.toThrow("Use Add a folder");
    const profileFiles = await fs.readdir(profile);
    expect(profileFiles.some((name) => name.startsWith("authorized-project-folders.json.corrupt-"))).toBe(true);
  } finally {
    await fs.rm(root, { recursive: true, force: true });
    await fs.rm(profile, { recursive: true, force: true });
  }
});

test("provider JSON responses are bounded before parsing", async () => {
  const bridge = createProviderBridge({
    net: {
      fetch: async () => new Response("x".repeat(8 * 1024 * 1024 + 1), {
        headers: { "content-type": "application/json" },
      }),
    },
    getProviderSecret: async () => "",
  });
  await expect(bridge({
    provider: "Test provider",
    endpoint: "https://example.com/v1/chat/completions",
    model: "test-model",
    messages: [{ role: "user", content: "hello" }],
  })).rejects.toThrow("8 MB JSON limit");
});

test("provider request messages are bounded individually and in aggregate", async () => {
  const bridge = createProviderBridge({
    net: { fetch: async () => { throw new Error("oversized provider input must be rejected before network access"); } },
    getProviderSecret: async () => { throw new Error("oversized provider input must be rejected before secret lookup"); },
  });
  await expect(bridge({
    provider: "Test provider",
    endpoint: "https://example.com/v1/chat/completions",
    model: "test-model",
    messages: [{ role: "user", content: "x".repeat(1_000_001) }],
  })).rejects.toThrow("message is too large");
  await expect(bridge({
    provider: "Test provider",
    endpoint: "https://example.com/v1/chat/completions",
    model: "test-model",
    messages: Array.from({ length: 9 }, () => ({ role: "user", content: "x".repeat(900_000) })),
  })).rejects.toThrow("request is too large");
});

test("shared JSON reader rejects an oversized declared response", async () => {
  const response = new Response("{}", {
    headers: {
      "content-type": "application/json",
      "content-length": String(MAX_JSON_BODY_BYTES + 1),
    },
  });
  await expect(readJsonBodyLimited(response, "Test metadata")).rejects.toThrow("8 MB JSON limit");
});

test("shared response readers reject unbounded or oversized error bodies", async () => {
  await expect(readJsonBodyLimited({ headers: { get: () => "" } }, "Test JSON")).rejects.toThrow("body is not readable");
  const response = new Response("x".repeat(MAX_TEXT_BODY_BYTES + 1), {
    headers: { "content-type": "text/plain" },
  });
  await expect(readTextBodyLimited(response, "Test provider")).rejects.toThrow("16 KB limit");
});

test("binary media responses are rejected from their declared size", async () => {
  const response = new Response("small", {
    headers: {
      "content-type": "image/png",
      "content-length": String(MAX_BINARY_BODY_BYTES + 1),
    },
  });
  await expect(readBinaryBodyLimited(response, MAX_BINARY_BODY_BYTES, "Generated media")).rejects.toThrow("50 MB limit");
});

test("provider streams stop before unbounded text accumulation", async () => {
  const body = new ReadableStream({
    start(controller) {
      const chunk = "x".repeat(64 * 1024);
      const frame = `data: ${JSON.stringify({ choices: [{ delta: { content: chunk } }] })}\n\n`;
      for (let index = 0; index < Math.ceil(MAX_STREAM_TEXT_LENGTH / chunk.length) + 1; index++)
        controller.enqueue(new TextEncoder().encode(frame));
      controller.close();
    },
  });
  const bridge = createProviderBridge({
    net: { fetch: async () => new Response(body, { headers: { "content-type": "text/event-stream" } }) },
    getProviderSecret: async () => "",
  });
  await expect(bridge({
    provider: "Test provider",
    endpoint: "https://example.com/v1/chat/completions",
    model: "test-model",
    messages: [{ role: "user", content: "hello" }],
  })).rejects.toThrow("2 MB stream limit");
});

test("Electron child environments omit secret-shaped variables", async () => {
  const safe = createSafeChildEnvironment({ PATH: "tools", API_KEY: "hidden", GITHUB_TOKEN: "hidden", SAFE_FLAG: "kept" });
  expect(safe).toEqual({ PATH: "tools", SAFE_FLAG: "kept" });
});

test("model paths reject traversal and alternate separators", async () => {
  const root = path.join(os.tmpdir(), "server-rock-models");
  expect(resolveModelFilePath(root, "onnx/model.onnx")).toBe(path.resolve(root, "onnx", "model.onnx"));
  expect(() => resolveModelFilePath(root, "../outside.onnx")).toThrow("Invalid file path");
  expect(() => resolveModelFilePath(root, "onnx\\outside.onnx")).toThrow("Invalid file path");
  await expect(ensureNoSymlinkComponents(root, path.join(root, "onnx"), fs)).resolves.toBeUndefined();
});

test("renderer IPC validation rejects malformed or oversized ChatGPT requests", async () => {
  expect(() => validateRequestId("bad id", "ChatGPT request ID")).toThrow("Invalid ChatGPT request ID");
  expect(() => validateChatGPTRequest([{ role: "user", content: "x" }], "", "chat-1")).toThrow("Invalid ChatGPT model");
  expect(() => validateChatGPTRequest([{ role: "user", content: "x".repeat(1_000_001) }], "gpt", "chat-1")).toThrow("message is too large");
  expect(validateChatGPTRequest([{ role: "user", content: "hello" }], "gpt", "chat-1").requestId).toBe("chat-1");
});

test("renderer IPC validation rejects malformed provider names", async () => {
  expect(validateProviderName("Ollama")).toBe("Ollama");
  expect(() => validateProviderName("provider\u0000secret")).toThrow("valid provider name");
  expect(() => validateProviderName("x".repeat(65))).toThrow("valid provider name");
});

test("encrypted state files are bounded before reading", async () => {
  const filePath = path.join(await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-state-")), "state.enc");
  try {
    await fs.writeFile(filePath, "x".repeat(MAX_ENCRYPTED_STATE_BYTES + 1), "utf8");
    await expect(readTextFileLimited(filePath, MAX_ENCRYPTED_STATE_BYTES, "Test state")).rejects.toThrow("byte limit");
  } finally {
    await fs.rm(path.dirname(filePath), { recursive: true, force: true });
  }
});

// Smoke-test item 1: saving an edited project must update it, not add a copy.
test("editing a project updates it instead of creating a duplicate", async ({ page }) => {
  await page.addInitScript(() => {
    if (sessionStorage.getItem("seeded")) return;
    sessionStorage.setItem("seeded", "1");
    localStorage.setItem("local-studio-v1", JSON.stringify({
      chats: [],
      projects: [{ id: "p1", name: "Alpha", folders: [], instructions: "Old notes" }],
      settings: { provider: "Ollama", endpoint: "http://localhost:11434/v1/chat/completions", model: "llama3.2", theme: "dark", temperature: 0.7 },
    }));
  });
  await page.goto(appUrl);

  await page.locator("#projectList .project-item").dblclick();
  await expect(page.locator("#projectDialog")).toBeVisible();
  await expect(page.locator("#projectDialog h2")).toHaveText("Edit project");
  await expect(page.locator("#editProject")).toBeHidden();
  await expect(page.locator("#projectForm .save")).toHaveText("Save changes");

  await page.locator("#projectName").fill("Alpha renamed");
  await page.locator("#projectInstructions").fill("New notes");
  await page.locator("#projectForm .save").click();

  await expect(page.locator("#projectDialog")).toBeHidden();
  await expect(page.locator("#projectList .project-item")).toHaveCount(1);
  await expect(page.locator("#projectList")).toContainText("Alpha renamed");
  const saved = await page.evaluate(() => JSON.parse(localStorage.getItem("local-studio-v1")).projects);
  expect(saved).toEqual([expect.objectContaining({ id: "p1", name: "Alpha renamed", instructions: "New notes" })]);

  // Pressing Enter in the name field while editing also updates in place.
  await page.locator("#projectList .project-item").dblclick();
  await page.locator("#projectName").fill("Alpha final");
  await page.locator("#projectName").press("Enter");
  await expect(page.locator("#projectList .project-item")).toHaveCount(1);
  await expect(page.locator("#projectList")).toContainText("Alpha final");

  // Creating afterwards still creates a new project with the create heading.
  await page.locator("#newProject").click();
  await expect(page.locator("#projectDialog h2")).toHaveText("Create project");
  await page.locator("#projectName").fill("Beta");
  await page.locator("#projectForm .save").click();
  await expect(page.locator("#projectList .project-item")).toHaveCount(2);
});

// Smoke-test item 2: Enter in the commit message must not close the push
// review without pushing.
test("Enter in the push review commit message pushes instead of closing silently", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("local-studio-v1", JSON.stringify({
      chats: [],
      projects: [{ id: "g", name: "Repo", folders: ["/repo"], github: { fullName: "octocat/repo" } }],
      activeProjectId: "g",
      settings: { provider: "Ollama", endpoint: "http://localhost:11434/v1/chat/completions", model: "llama3.2", theme: "dark", temperature: 0.7 },
    }));
    window.reviewHasChanges = true;
    window.desktopModels = {
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: true }),
      githubStatus: async () => ({ connected: true, login: "octocat" }),
      githubProjectStatus: async () => window.reviewHasChanges
        ? { branch: "main", changedFiles: 1, ahead: 0, changes: [" M README.md"], changeToken: "t".repeat(64) }
        : { branch: "main", changedFiles: 0, ahead: 0, changes: [], changeToken: "t".repeat(64) },
      githubPushProject: async (folders, message, fullName) => {
        window.pushRequest = { message, fullName };
        return { pushed: true, branch: "main", commit: "abc1234", changedFiles: 1 };
      },
    };
  });
  await page.goto(appUrl);

  // Nothing to push: Enter leaves the review open instead of closing it.
  await page.evaluate(() => { window.reviewHasChanges = false; });
  await page.locator("#githubSync").click();
  await expect(page.locator("#githubPushConfirm")).toBeDisabled();
  await page.locator("#githubCommitMessage").press("Enter");
  await expect(page.locator("#githubPushDialog")).toBeVisible();
  expect(await page.evaluate(() => window.pushRequest)).toBeUndefined();
  await page.locator("#githubPushCancel").click();
  await expect(page.locator("#githubPushDialog")).toBeHidden();

  // With changes: the message box gets focus and Enter commits and pushes.
  await page.evaluate(() => { window.reviewHasChanges = true; });
  await page.locator("#githubSync").click();
  await expect(page.locator("#githubPushConfirm")).toBeEnabled();
  await expect(page.locator("#githubCommitMessage")).toBeFocused();
  await page.locator("#githubCommitMessage").fill("Docs update");
  await page.locator("#githubCommitMessage").press("Enter");
  await expect(page.locator("#githubPushDialog")).toBeHidden();
  expect(await page.evaluate(() => window.pushRequest)).toEqual({ message: "Docs update", fullName: "octocat/repo" });
});

// Smoke-test item 3: /models must work in Electron, where window.prompt()
// throws "prompt() is not supported".
test("/models opens the model picker instead of using window.prompt", async ({ page }) => {
  const dialogs = [];
  page.on("dialog", async (dialog) => { dialogs.push(dialog.message()); await dialog.dismiss(); });
  await page.addInitScript(() => {
    window.prompt = () => { throw new Error("prompt() is not supported."); };
    window.hostScans = 0;
    window.desktopModels = {
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: true }),
      scanHosts: async () => {
        window.hostScans++;
        return { models: [{ provider: "Ollama", model: "qwen3:4b", endpoint: "http://127.0.0.1:11434/v1/chat/completions", local: true }], hosts: ["Ollama"] };
      },
    };
  });
  await page.goto(appUrl);

  for (const sendWith of ["click", "Enter"]) {
    await page.locator("#prompt").fill("/models");
    if (sendWith === "click") await page.locator("#send").click();
    else await page.locator("#prompt").press("Enter");
    await expect(page.locator("#modelPicker")).toBeVisible();
    await expect(page.locator("#modelOptions")).toContainText("qwen3:4b");
    await expect(page.locator("#prompt")).toHaveValue("");
    await page.mouse.click(5, 5);
    await expect(page.locator("#modelPicker")).toBeHidden();
  }
  expect(await page.evaluate(() => window.hostScans)).toBeGreaterThanOrEqual(2);
  expect(dialogs).toEqual([]);
});

// Smoke-test item 4: the in-app updater needs its metadata on the release,
// asset names it can find, and an honest message where updates can't install.
test("release uploads updater metadata and installer names have no spaces", async () => {
  const pkg = JSON.parse(await fs.readFile(path.resolve(__dirname, "..", "package.json"), "utf8"));
  const names = [pkg.build.artifactName, pkg.build.nsis.artifactName, pkg.build.portable.artifactName];
  // GitHub turns spaces in asset names into dots, while latest*.yml uses
  // dashes, so a name with spaces makes the updater's download 404.
  for (const name of names) expect(name).not.toMatch(/\s/);
  expect(new Set(names).size).toBe(3);
  expect(pkg.build.artifactName).toContain("${arch}");

  const workflow = await fs.readFile(path.resolve(__dirname, "..", ".github", "workflows", "release.yml"), "utf8");
  const uploads = workflow.slice(workflow.indexOf("path: |"), workflow.indexOf("if-no-files-found"));
  for (const pattern of ["dist/latest.yml", "dist/latest-linux*.yml", "dist/*.blockmap"])
    expect(uploads).toContain(pattern);
  expect(uploads).not.toContain("latest-mac");
});

test("builds that can't update in place report a manual update", async () => {
  const { manualUpdateReason } = require("../electron/update-support.cjs");
  const none = () => false;
  expect(manualUpdateReason({ platform: "darwin", env: {}, resourcesPath: "/r", fileExists: none })).toContain("isn't code-signed");
  expect(manualUpdateReason({ platform: "win32", env: { PORTABLE_EXECUTABLE_DIR: "C:\\x" }, resourcesPath: "C:\\r", fileExists: none })).toContain("portable app");
  expect(manualUpdateReason({ platform: "win32", env: {}, resourcesPath: "C:\\r", fileExists: none })).toBe("");
  expect(manualUpdateReason({ platform: "linux", env: {}, resourcesPath: "/r", fileExists: none })).toContain("tar.gz");
  expect(manualUpdateReason({ platform: "linux", env: { APPIMAGE: "/a.AppImage" }, resourcesPath: "/r", fileExists: none })).toBe("");
  expect(manualUpdateReason({ platform: "linux", env: {}, resourcesPath: "/r", fileExists: (p) => p === path.join("/r", "package-type") })).toBe("");
});

test("Settings shows how to update when this build can't update itself", async ({ page }) => {
  await page.addInitScript(() => {
    window.desktopModels = {
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: true }),
      updateState: async () => ({ status: "idle", version: "0.1.2" }),
      onUpdateState: () => () => {},
      checkForUpdates: async () => ({ status: "manual", version: "0.1.2", message: "The portable app can't update itself. Download the new version from GitHub Releases instead." }),
    };
  });
  await page.goto(appUrl);
  await page.locator("#settings").click();
  await page.locator("#settingsDialog details.settings-group:not(#hfDownloads) summary").click();
  await page.locator("#checkUpdates").click();
  await expect(page.locator("#updateStatus")).toHaveText(/portable app can't update itself.*GitHub Releases/);
  await expect(page.locator("#downloadUpdate")).toBeHidden();
  await expect(page.locator("#installUpdate")).toBeHidden();
});

// Smoke-test item 7: every installed package with an install script must have
// an explicit allowScripts decision, so a fresh `npm install` on npm 11+ is
// deterministic and doesn't block anything the app needs without saying so.
test("every dependency install script has an explicit allowScripts decision", async () => {
  const root = path.resolve(__dirname, "..");
  const pkg = JSON.parse(await fs.readFile(path.join(root, "package.json"), "utf8"));
  const decided = pkg.allowScripts || {};
  const withScripts = new Set();
  async function scan(dir) {
    let entries;
    try { entries = await fs.readdir(dir, { withFileTypes: true }); } catch { return; }
    for (const entry of entries) {
      if (!entry.isDirectory() || entry.name.startsWith(".")) continue;
      const full = path.join(dir, entry.name);
      if (entry.name.startsWith("@")) { await scan(full); continue; }
      try {
        const meta = JSON.parse(await fs.readFile(path.join(full, "package.json"), "utf8"));
        const scripts = meta.scripts || {};
        if (scripts.preinstall || scripts.install || scripts.postinstall) withScripts.add(meta.name);
      } catch {}
      await scan(path.join(full, "node_modules"));
    }
  }
  await scan(path.join(root, "node_modules"));
  const undecided = [...withScripts].filter((name) => !(name in decided)).sort();
  expect(undecided).toEqual([]);
  // Electron 45 downloads its runtime on first launch, so it needs no install script.
  expect(withScripts.has("electron")).toBe(false);
});
