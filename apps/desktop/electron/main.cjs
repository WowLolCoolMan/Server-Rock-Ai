const { app, BrowserWindow, dialog, ipcMain, session } = require("electron");
const { spawn } = require("node:child_process");
const fs = require("node:fs/promises");
const path = require("node:path");
const readline = require("node:readline");

const repoRoot = path.resolve(__dirname, "../../..");
const hostProject = path.join(repoRoot, "services", "agent-host", "ServerRock.AgentHost.csproj");
let host;
let hostReader;
let nextRequestId = 1;
const pending = new Map();
const allowedMethods = new Set([
  "runtime.health", "models.list", "projects.list", "project.save", "project.files", "project.file.read", "tasks.list",
  "scheduled.list", "conversations.list", "conversations.search", "conversation.save", "conversation.metadata.save", "conversations.export", "conversation.fork", "settings.get",
  "settings.save", "image.generate", "project.file.write", "project.file.undo", "project.command.run", "chat.queue.list", "chat.queue.enqueue", "chat.queue.remove", "chat.complete",
  "workspace.pickFolder",
]);

function startAgentHost() {
  host = spawn("dotnet", ["run", "--project", hostProject, "--no-launch-profile", "--", "--stdio"], {
    cwd: repoRoot,
    stdio: ["pipe", "pipe", "pipe"],
    windowsHide: true,
  });

  hostReader = readline.createInterface({ input: host.stdout });
  hostReader.on("line", (line) => {
    let message;
    try {
      message = JSON.parse(line);
    } catch {
      return;
    }
    const resolve = pending.get(message.id);
    if (!resolve) return;
    pending.delete(message.id);
    resolve(message);
  });

  host.stderr.on("data", (chunk) => {
    console.error(`[agent-host] ${chunk.toString().trimEnd()}`);
  });

  host.on("exit", (code, signal) => {
    const error = { v: 1, ok: false, error: { code: "HOST_EXITED", message: `Agent host exited (${code ?? signal}).` } };
    for (const resolve of pending.values()) resolve(error);
    pending.clear();
  });
}

function requestAgent(method, params) {
  if (typeof method !== "string" || !allowedMethods.has(method)) {
    return Promise.resolve({ v: 1, ok: false, error: { code: "METHOD_NOT_ALLOWED", message: "That desktop operation is not exposed by the renderer bridge." } });
  }
  if (params !== undefined && (params === null || typeof params !== "object" || Array.isArray(params))) {
    return Promise.resolve({ v: 1, ok: false, error: { code: "INVALID_PARAMS", message: "Agent parameters must be an object." } });
  }
  if (!host || host.stdin.destroyed) {
    return Promise.resolve({ v: 1, ok: false, error: { code: "HOST_UNAVAILABLE", message: "Agent host is not running." } });
  }
  const id = `desktop-${nextRequestId++}`;
  host.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve) => pending.set(id, resolve));
}

function createWindow() {
  const window = new BrowserWindow({
    width: 1440,
    height: 900,
    minWidth: 960,
    minHeight: 640,
    backgroundColor: "#10141d",
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      preload: path.join(__dirname, "preload.cjs"),
    },
  });

  const devUrl = process.env.SERVER_ROCK_RENDERER_URL;
  if (devUrl) window.loadURL(devUrl);
  else window.loadFile(path.join(repoRoot, "apps", "desktop", "dist", "index.html"));
}

function applyProductionContentSecurityPolicy() {
  session.defaultSession.webRequest.onHeadersReceived((details, callback) => {
    callback({
      responseHeaders: {
        ...details.responseHeaders,
        "Content-Security-Policy": [
          "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob: http://127.0.0.1:8188 http://localhost:8188; connect-src 'self'; font-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'",
        ],
      },
    });
  });
}

app.whenReady().then(() => {
  startAgentHost();
  if (!process.env.SERVER_ROCK_RENDERER_URL) applyProductionContentSecurityPolicy();
  ipcMain.handle("agent:request", async (event, method, params) => {
    if (method === "workspace.pickFolder") {
      const window = BrowserWindow.fromWebContents(event.sender);
      const result = await dialog.showOpenDialog(window, { properties: ["openDirectory"] });
      return { v: 1, ok: true, result: { path: result.canceled ? null : result.filePaths[0] ?? null } };
    }
    if (method === "workspace.pickFiles") {
      const window = BrowserWindow.fromWebContents(event.sender);
      const result = await dialog.showOpenDialog(window, { properties: ["openFile", "multiSelections"], filters: [{ name: "Chat files", extensions: ["txt", "md", "markdown", "csv", "json", "xml", "yaml", "yml", "toml", "html", "css", "js", "ts", "jsx", "tsx", "cs", "py", "java", "go", "rs", "c", "h", "cpp", "hpp", "sh", "sql", "log", "png", "jpg", "jpeg"] }] });
      if (result.canceled) return { v: 1, ok: true, result: { attachments: [] } };
      const attachments = [];
      for (const filePath of result.filePaths.slice(0, 5)) {
        const fileName = path.basename(filePath);
        const extension = path.extname(fileName).toLowerCase();
        const bytes = await fs.readFile(filePath);
        if (bytes.length > 2 * 1024 * 1024) throw new Error(`The selected file is too large: ${fileName}`);
        const imageType = extension === ".png" ? "image/png" : [".jpg", ".jpeg"].includes(extension) ? "image/jpeg" : null;
        attachments.push({ fileName, mediaType: imageType, content: imageType ? bytes.toString("base64") : bytes.toString("utf8").slice(0, 16000) });
      }
      return { v: 1, ok: true, result: { attachments } };
    }
    return requestAgent(method, params);
  });
  createWindow();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  if (host && !host.killed) host.kill();
  if (process.platform !== "darwin") app.quit();
});
