const { test, expect } = require("@playwright/test");
const path = require("node:path");
const fs = require("node:fs/promises");
const os = require("node:os");
const { EventEmitter } = require("node:events");
const { PassThrough } = require("node:stream");
const { pathToFileURL } = require("node:url");
const { createWorkspace, isProtectedPath } = require("../electron/workspace.cjs");
const { createProviderBridge } = require("../electron/provider-bridge.cjs");
const { createSafeChildEnvironment } = require("../electron/child-environment.cjs");
const { resolveModelFilePath, ensureNoSymlinkComponents } = require("../electron/model-path-safety.cjs");
const { validateRequestId, validateChatGPTRequest, validateProviderName } = require("../electron/ipc-validation.cjs");
const { MAX_ENCRYPTED_STATE_BYTES, readTextFileLimited } = require("../electron/bounded-file.cjs");
const { createTerminalManager, MAX_OUTPUT_BYTES, MAX_INPUT_BYTES } = require("../electron/terminal-sessions.cjs");
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

test("renderer local-runtime readers reject oversized JSON and text responses", async ({ page }) => {
  await page.goto(appUrl);
  const result = await page.evaluate(async () => {
    let jsonError = "", textError = "";
    try { await readJsonResponseLimited(new Response("x".repeat(4097)), "Test JSON", 4096); }
    catch (error) { jsonError = error.message; }
    try { await readTextResponseLimited(new Response("x".repeat(4097)), "Test text", 4096); }
    catch (error) { textError = error.message; }
    return { jsonError, textError };
  });
  expect(result.jsonError).toContain("4 KB limit");
  expect(result.textError).toContain("4 KB limit");
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

test("terminal sessions stay inside authorized roots and replay bounded output", async () => {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-terminal-"));
  const children = [];
  try {
    const manager = createTerminalManager({
      resolveRoots: async () => [root],
      platform: "linux",
      environment: { SHELL: "/bin/sh", PATH: process.env.PATH, API_KEY: "must-not-leak" },
      now: () => 123,
      spawn: (_shell, _args, options) => {
        const child = new EventEmitter();
        child.stdout = new PassThrough();
        child.stderr = new PassThrough();
        child.stdin = new PassThrough();
        child.kill = () => child.emit("exit", null, "SIGTERM");
        child.spawnOptions = options;
        children.push(child);
        return child;
      },
    });
    const events = [];
    const session = await manager.start({ folders: [root], cwd: root, command: "printf hello", timeoutMs: 5000 }, (event) => events.push(event));
    expect(session.status).toBe("running");
    expect(children[0].spawnOptions.cwd).toBe(root);
    expect(children[0].spawnOptions.env.API_KEY).toBeUndefined();
    children[0].stdout.write("hello");
    children[0].emit("exit", 0, null);
    expect(manager.snapshot(session.id)).toMatchObject({ status: "exited", output: "hello" });
    let replay;
    manager.attach(session.id, (event) => { if (event.type === "snapshot") replay = event; });
    expect(replay).toMatchObject({ output: "hello", status: "exited" });
    await expect(manager.start({ folders: [root], cwd: path.dirname(root), command: "pwd" }, () => {})).rejects.toThrow("authorized project folder");

    const bounded = await manager.start({ folders: [root], cwd: root, command: "yes", timeoutMs: 5000 }, () => {});
    children[1].stdout.write(Buffer.alloc(MAX_OUTPUT_BYTES + 1, "x"));
    expect(manager.snapshot(bounded.id).truncated).toBe(true);
    const inputSession = await manager.start({ folders: [root], cwd: root, command: "cat", timeoutMs: 5000 }, () => {});
    expect(() => manager.write(inputSession.id, "x".repeat(MAX_INPUT_BYTES + 1))).toThrow("invalid or too large");
    manager.dispose();
    expect(events.some((event) => event.type === "exit")).toBe(true);
  } finally {
    await fs.rm(root, { recursive: true, force: true });
  }
});

test("GitHub push protection recognizes secret-shaped changed paths", () => {
  expect(isProtectedPath(".env")).toBe(true);
  expect(isProtectedPath("config/.env.production")).toBe(true);
  expect(isProtectedPath(".git/config")).toBe(true);
  expect(isProtectedPath(".github/workflows/release.yml")).toBe(false);
  expect(isProtectedPath("src/index.js")).toBe(false);
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

// Smoke-test item 4: installer names must let the GitHub release updater find
// the right file. GitHub turns spaces in asset names into dots, and
// chooseInstaller() needs the architecture in macOS and Linux file names.
test("installer names have no spaces and the updater picks the right one per platform", async () => {
  const { chooseInstaller } = require("../electron/github-updates.cjs");
  const pkg = JSON.parse(await fs.readFile(path.resolve(__dirname, "..", "package.json"), "utf8"));
  const patterns = [pkg.build.artifactName, pkg.build.nsis.artifactName, pkg.build.portable.artifactName];
  for (const pattern of patterns) expect(pattern).not.toMatch(/\s/);
  expect(new Set(patterns).size).toBe(3);

  // File names as electron-builder expands these patterns (checked with a local Linux build).
  const release = (names) => ({ assets: names.map((name, index) => ({ name, size: 1, id: index + 1, digest: "sha256:" + "a".repeat(64) })) });
  const ours = release([
    "Server-Rock-AI-Setup-0.1.2.exe", "Server-Rock-AI-0.1.2-portable.exe",
    "Server-Rock-AI-0.1.2-x64.dmg", "Server-Rock-AI-0.1.2-arm64.dmg",
    "Server-Rock-AI-0.1.2-x64.zip", "Server-Rock-AI-0.1.2-arm64.zip",
    "Server-Rock-AI-0.1.2-x86_64.AppImage", "Server-Rock-AI-0.1.2-arm64.AppImage",
    "Server-Rock-AI-0.1.2-amd64.deb", "Server-Rock-AI-0.1.2-x64.tar.gz",
  ]);
  expect(chooseInstaller(ours, "win32", "x64")?.name).toBe("Server-Rock-AI-Setup-0.1.2.exe");
  expect(chooseInstaller(ours, "darwin", "x64")?.name).toBe("Server-Rock-AI-0.1.2-x64.dmg");
  expect(chooseInstaller(ours, "darwin", "arm64")?.name).toBe("Server-Rock-AI-0.1.2-arm64.dmg");
  expect(chooseInstaller(ours, "linux", "x64")?.name).toBe("Server-Rock-AI-0.1.2-x86_64.AppImage");
  expect(chooseInstaller(ours, "linux", "arm64")?.name).toBe("Server-Rock-AI-0.1.2-arm64.AppImage");

  // The old default names (after GitHub's space-to-dot rename) leave Intel Macs
  // and x64 Linux without a match, because the x64 builds had no arch in the name.
  const defaults = release(["Server.Rock.AI-0.1.2.dmg", "Server.Rock.AI-0.1.2-arm64.dmg", "Server.Rock.AI-0.1.2.AppImage", "Server.Rock.AI-0.1.2-arm64.AppImage"]);
  expect(chooseInstaller(defaults, "darwin", "x64")).toBeNull();
  expect(chooseInstaller(defaults, "linux", "x64")).toBeNull();
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

// The Linux .deb build fails without a maintainer email ("Please specify
// author 'email'"), which stopped tagged releases at the Linux package jobs.
test("Linux packages declare a maintainer with an email address", async () => {
  const pkg = JSON.parse(await fs.readFile(path.resolve(__dirname, "..", "package.json"), "utf8"));
  expect(pkg.build.linux.maintainer).toMatch(/^[^<>]+ <[^@\s<>]+@[^@\s<>]+\.[^@\s<>]+>$/);
});

// Smoke-test item 10: the sidebar "Clear" button deleted every saved chat
// instantly, with no confirmation (the /clear command already asked).
test("sidebar Clear asks before deleting chat history", async ({ page }) => {
  await page.addInitScript(() => {
    if (sessionStorage.getItem("seeded")) return;
    sessionStorage.setItem("seeded", "1");
    localStorage.setItem("local-studio-v1", JSON.stringify({
      chats: [
        { id: "c1", title: "First chat", messages: [{ role: "user", content: "a" }] },
        { id: "c2", title: "Second chat", messages: [{ role: "user", content: "b" }] },
      ],
      projects: [],
      settings: { provider: "Ollama", endpoint: "http://localhost:11434/v1/chat/completions", model: "llama3.2", theme: "dark", temperature: 0.7 },
    }));
  });
  const dialogs = [];
  let answer = false;
  page.on("dialog", async (dialog) => {
    dialogs.push(dialog.message());
    if (answer) await dialog.accept(); else await dialog.dismiss();
  });
  await page.goto(appUrl);
  const savedChats = () => page.evaluate(() => JSON.parse(localStorage.getItem("local-studio-v1")).chats.length);
  await expect(page.locator("#history .chat-item")).toHaveCount(2);

  // Cancel keeps everything.
  await page.locator("#clear").click();
  expect(dialogs).toHaveLength(1);
  expect(dialogs[0]).toContain("2 chats");
  await expect(page.locator("#history .chat-item")).toHaveCount(2);
  expect(await savedChats()).toBe(2);

  // The /clear command uses the same confirmation.
  await page.locator("#prompt").fill("/clear");
  await page.locator("#prompt").press("Enter");
  await expect.poll(() => dialogs.length).toBe(2);
  expect(dialogs[1]).toContain("2 chats");
  expect(await savedChats()).toBe(2);

  // Confirm deletes the history.
  answer = true;
  await page.locator("#clear").click();
  await expect(page.locator("#history .chat-item")).toHaveCount(0);
  expect(await savedChats()).toBe(0);

  // With nothing saved, Clear doesn't ask a pointless question.
  await page.locator("#clear").click();
  expect(dialogs).toHaveLength(3);
  await expect(page.locator("#runtimeStatus")).toHaveText("There is no chat history to clear.");
});

// Smoke-test item 11: chat providers never receive attachments (only Hugging
// Face image/audio tasks do), but the app said nothing, and the user's message
// still read "[Image attached: ...]" as if the model had seen it.
test("attaching a file says when the selected provider won't receive it", async ({ page }) => {
  await page.addInitScript(() => {
    window.desktopModels = {
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: true }),
      streamProviderChat: async (request, onChunk) => { window.lastRequest = request; onChunk("I can't see images here."); },
      cancelProviderChat: () => {},
    };
  });
  await page.goto(appUrl);
  const png = Buffer.from("89504e470d0a1a0a", "hex");
  await page.locator("#file").setInputFiles({ name: "cat.png", mimeType: "image/png", buffer: png });
  await expect(page.locator("#runtimeStatus")).toContainText("cat.png isn't sent to Ollama");

  await page.locator("#prompt").fill("What is in this picture?");
  await page.locator("#send").click();
  await expect(page.locator(".bubble.user").last()).toContainText("cat.png — not sent to Ollama");
  const request = await page.evaluate(() => window.lastRequest);
  expect(JSON.stringify(request.messages)).not.toContain("data:image");

  // A Hugging Face image task does use the attachment, so no warning.
  await page.evaluate(() => { state.settings.provider = "Hugging Face"; state.settings.hfTask = "image-to-text"; });
  await page.locator("#runtimeStatus").evaluate((el) => (el.textContent = ""));
  await page.locator("#file").setInputFiles({ name: "dog.png", mimeType: "image/png", buffer: png });
  await expect(page.locator("#prompt")).toHaveAttribute("placeholder", "Ask about dog.png");
  await expect(page.locator("#runtimeStatus")).not.toContainText("isn't sent");
});

// CI flake (ui.spec "marketplace search restores keyboard focus"): closing the
// mobile drawer returned focus to the menu button one animation frame later,
// even if focus had already moved elsewhere. On a slow runner that frame
// landed after the test focused Search, so focus was taken away. Slowing
// animation frames reproduces it every time.
test("closing the mobile drawer doesn't take focus back from a control focused since", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.addInitScript(() => {
    const realRaf = window.requestAnimationFrame.bind(window);
    window.requestAnimationFrame = (callback) => setTimeout(() => realRaf(callback), 300);
    window.desktopModels = {
      searchHuggingFace: async () => [],
      getProviderSecretStatus: async () => ({ saved: false, secureStorage: true }),
    };
  });
  await page.goto(appUrl);

  // Focus moves on before the delayed frame: it must stay where it was put.
  await page.locator("#mobileMenu").click();
  await page.locator("#openModelLibrary").click();
  await page.locator("#marketSearch").focus();
  await page.waitForTimeout(700);
  await expect(page.locator("#marketSearch")).toBeFocused();

  // The exact steps of the flaky marketplace test: focus Search, press Enter,
  // the button is disabled while searching, then focus must come back to it.
  await page.evaluate(() => {
    window.desktopModels.searchHuggingFace = () => new Promise((resolve) => { window.finishSearch = () => resolve([]); });
  });
  await page.locator("#mobileMenu").click();
  await page.locator("#openModelLibrary").click();
  const searchButton = page.locator("#marketSearchButton");
  await searchButton.focus();
  await page.keyboard.press("Enter");
  await expect(searchButton).toBeDisabled();
  await page.waitForTimeout(700);
  await page.evaluate(() => window.finishSearch());
  await expect(searchButton).toBeEnabled();
  await expect(searchButton).toBeFocused();

  // Focus left inside the closed drawer would be lost, so it still returns to the menu button.
  await page.locator("#mobileMenu").click();
  await expect(page.locator(".sidebar")).toHaveClass(/mobile-open/);
  await page.locator("#openModelLibrary").focus();
  await page.keyboard.press("Enter");
  await expect(page.locator("#mobileMenu")).toBeFocused({ timeout: 3000 });
});
