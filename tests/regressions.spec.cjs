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
