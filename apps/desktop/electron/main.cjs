const { app, BrowserWindow, ipcMain } = require("electron");
const { spawn } = require("node:child_process");
const path = require("node:path");
const readline = require("node:readline");

const repoRoot = path.resolve(__dirname, "../../..");
const hostProject = path.join(repoRoot, "services", "agent-host", "ServerRock.AgentHost.csproj");
let host;
let hostReader;
let nextRequestId = 1;
const pending = new Map();

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

app.whenReady().then(() => {
  startAgentHost();
  ipcMain.handle("agent:request", (_event, method, params) => requestAgent(method, params));
  createWindow();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  if (host && !host.killed) host.kill();
  if (process.platform !== "darwin") app.quit();
});
