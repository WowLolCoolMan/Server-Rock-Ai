const { app, BrowserWindow, dialog, ipcMain, session } = require("electron");
const { spawn } = require("node:child_process");
const fsSync = require("node:fs");
const fs = require("node:fs/promises");
const path = require("node:path");
const readline = require("node:readline");

const repoRoot = path.resolve(__dirname, "../../..");
const hostProject = path.join(repoRoot, "services", "agent-host", "ServerRock.AgentHost.csproj");
const hostBinary = process.platform === "win32" ? "ServerRock.AgentHost.exe" : "ServerRock.AgentHost";
const packagedRoot = path.join(process.resourcesPath, "app");
const packagedHost = path.join(process.resourcesPath, "agent-host", hostBinary);
const packagedRuntime = fsSync.existsSync(path.join(packagedRoot, "dist", "index.html")) && fsSync.existsSync(packagedHost);
const runtimeRoot = packagedRuntime ? packagedRoot : repoRoot;
const rendererRoot = packagedRuntime ? runtimeRoot : path.join(runtimeRoot, "apps", "desktop");
let host;
let hostReader;
let nextRequestId = 1;
const pending = new Map();
const standardRequestTimeoutMs = 30_000;
const chatRequestTimeoutMs = 5 * 60_000;
const longRunningMethods = new Set(["marketplace.models.download"]);
const chatHostDeadlineSeconds = 285;
let isQuitting = false;
let mainWindow;
let petWindow;
let petDragging = false;
let petDragOffset = { x: 0, y: 0 };
let petActivity = "idle";
let petWanderTimer;
let petWanderTarget;
let petWanderNextAt = 0;
let petSettings = {
  visible: true,
  wander: true,
  reduceMotion: false,
  petId: "byte",
  spritePath: "",
  manifestPath: "",
  position: { x: -1, y: -1 },
};
const petWindowSize = { width: 128, height: 146 };
const petAssetLimitBytes = 8 * 1024 * 1024;
const petManifestLimitBytes = 64 * 1024;
const petAssetRoot = () => path.join(app.getPath("userData"), "pet-assets");
const attachmentTextExtensions = new Set([".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".yaml", ".yml", ".toml", ".html", ".htm", ".css", ".js", ".ts", ".jsx", ".tsx", ".cs", ".py", ".java", ".go", ".rs", ".c", ".h", ".cpp", ".hpp", ".sh", ".sql", ".log"]);
const blockedAttachmentNames = new Set([".env", ".env.local", ".env.production", "id_rsa", "id_ed25519", "credentials", "secrets"]);
const isBlockedAttachmentName = (name) => blockedAttachmentNames.has(name.toLowerCase()) || name.toLowerCase().includes(".env.");
const isPng = (bytes) => bytes.length >= 8 && bytes.subarray(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]));
const isJpeg = (bytes) => bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;
const allowedMethods = new Set([
  "runtime.health", "models.list", "projects.list", "project.save", "project.files", "project.file.read", "tasks.list",
  "scheduled.list", "tasks.plan", "tasks.queue", "tasks.cancel", "tasks.retry", "conversations.list", "conversations.search", "conversation.save", "conversation.metadata.save", "conversations.export", "conversation.fork", "memory.list", "memory.search", "memory.save", "memory.remove", "memory.enabled", "memory.export", "settings.get",
  "settings.save", "credentials.status", "credentials.save", "credentials.clear", "image.generate", "project.file.write", "project.file.undo", "project.command.run", "chat.queue.list", "chat.queue.enqueue", "chat.queue.remove", "chat.complete", "workspace.attachment.extract", "voice.models", "voice.speak", "voice.speak.cloud", "voice.stop", "marketplace.models.search", "marketplace.models.download", "marketplace.skills.search", "marketplace.skills.inspect", "marketplace.tools.list", "marketplace.mcp.list", "skills.installed.list", "skills.installed.inspect", "skills.installed.remove", "skills.local.preview", "skills.local.install", "skills.enabled.set", "skills.rollback",
  "workspace.pickFolder",
]);

function startAgentHost() {
  const command = packagedRuntime ? packagedHost : "dotnet";
  const args = packagedRuntime ? ["--stdio"] : ["run", "--project", hostProject, "--no-launch-profile", "--", "--stdio"];
  host = spawn(command, args, {
    cwd: packagedRuntime ? path.dirname(packagedHost) : repoRoot,
    stdio: ["pipe", "pipe", "pipe"],
    windowsHide: true,
  });
  const startedHost = host;

  hostReader = readline.createInterface({ input: host.stdout });
  hostReader.on("line", (line) => {
    let message;
    try {
      message = JSON.parse(line);
    } catch {
      return;
    }
    const entry = pending.get(message.id);
    if (!entry) return;
    pending.delete(message.id);
    clearTimeout(entry.timer);
    entry.resolve(message);
  });

  host.stderr.on("data", (chunk) => {
    console.error(`[agent-host] ${chunk.toString().trimEnd()}`);
  });

  host.on("exit", (code, signal) => {
    const error = { v: 1, ok: false, error: { code: "HOST_EXITED", message: `Agent host exited (${code ?? signal}).` } };
    hostReader?.close();
    for (const entry of pending.values()) {
      clearTimeout(entry.timer);
      entry.resolve(error);
    }
    pending.clear();
    if (host === startedHost) host = undefined;
  });
}

function stopAgentHost(onStopped) {
  if (!host) {
    onStopped?.();
    return;
  }
  const currentHost = host;
  host = undefined;
  const error = { v: 1, ok: false, error: { code: "HOST_STOPPED", message: "Agent host is shutting down." } };
  for (const entry of pending.values()) {
    clearTimeout(entry.timer);
    entry.resolve(error);
  }
  pending.clear();
  if (!currentHost.stdin.destroyed) currentHost.stdin.end();
  const forceStop = setTimeout(() => {
    if (currentHost.exitCode !== null) return;
    if (process.platform === "win32" && currentHost.pid) {
      const killer = spawn("taskkill", ["/pid", String(currentHost.pid), "/T", "/F"], { windowsHide: true, stdio: "ignore" });
      killer.on("error", () => { if (!currentHost.killed) currentHost.kill(); });
    } else if (!currentHost.killed) currentHost.kill("SIGTERM");
  }, 1500);
  currentHost.once("exit", () => {
    clearTimeout(forceStop);
    onStopped?.();
  });
}

function restartAgentHost() {
  stopAgentHost(() => {
    if (!isQuitting) startAgentHost();
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
  const requestParams = method === "chat.complete"
    ? { ...params, timeoutSeconds: chatHostDeadlineSeconds }
    : params;
  return new Promise((resolve) => {
    const timeoutMs = method === "chat.complete" ? chatRequestTimeoutMs : longRunningMethods.has(method) ? 50 * 60_000 : standardRequestTimeoutMs;
    const timer = setTimeout(() => {
      if (!pending.has(id)) return;
      pending.delete(id);
      resolve({ v: 1, ok: false, error: { code: "HOST_TIMEOUT", message: `The local agent host did not answer ${method} within the allowed time.` } });
      restartAgentHost();
    }, timeoutMs);
    pending.set(id, { resolve, timer });
    try {
      host.stdin.write(`${JSON.stringify({ v: 1, id, method, params: requestParams })}\n`);
    } catch {
      clearTimeout(timer);
      pending.delete(id);
      resolve({ v: 1, ok: false, error: { code: "HOST_WRITE_FAILED", message: "The local agent host could not accept the request." } });
    }
  });
}

const allowedPetIds = new Set(["byte", "percy", "rocky", "sprout", "nova", "ember", "custom"]);
const allowedPetActivities = new Set(["idle", "walking", "thinking", "coding", "working", "success", "error"]);

function boundedText(value, fallback = "", max = 2_000) {
  if (typeof value !== "string" || value.length > max || value.includes("\0") || [...value].some((character) => character.charCodeAt(0) < 32 && character !== "\r" && character !== "\n" && character !== "\t")) return fallback;
  return value.trim();
}

function sanitizePetSettings(value) {
  const input = value && typeof value === "object" && !Array.isArray(value) ? value : {};
  const position = input.position && typeof input.position === "object" ? input.position : {};
  return {
    visible: input.visible !== false,
    wander: input.wander !== false,
    reduceMotion: input.reduceMotion === true,
    petId: allowedPetIds.has(input.petId) ? input.petId : "byte",
    spritePath: boundedText(input.spritePath, "", 1_024),
    manifestPath: boundedText(input.manifestPath, "", 1_024),
    position: {
      x: Number.isFinite(position.x) ? Math.round(position.x) : petSettings.position.x,
      y: Number.isFinite(position.y) ? Math.round(position.y) : petSettings.position.y,
    },
  };
}

function isWithin(directory, candidate) {
  const root = path.resolve(directory);
  const resolved = path.resolve(candidate);
  const relative = path.relative(root, resolved);
  return relative === "" || (relative && !relative.startsWith(`..${path.sep}`) && relative !== ".." && !path.isAbsolute(relative));
}

function petMimeType(filePath) {
  const extension = path.extname(filePath).toLowerCase();
  return extension === ".png" ? "image/png" : extension === ".jpg" || extension === ".jpeg" ? "image/jpeg" : extension === ".webp" ? "image/webp" : null;
}

function validatePetManifest(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) throw new Error("The pet manifest must be a JSON object.");
  const frameWidth = Number(value.frameWidth);
  const frameHeight = Number(value.frameHeight);
  const columns = Number(value.columns);
  const rows = Number(value.rows);
  if (![frameWidth, frameHeight, columns, rows].every(Number.isInteger) || frameWidth < 8 || frameWidth > 2_048 || frameHeight < 8 || frameHeight > 2_048 || columns < 1 || columns > 32 || rows < 1 || rows > 32) throw new Error("The pet manifest has invalid frame dimensions.");
  const states = {};
  if (!value.states || typeof value.states !== "object" || Array.isArray(value.states)) throw new Error("The pet manifest must define animation states.");
  for (const [name, rawState] of Object.entries(value.states).slice(0, 24)) {
    if (!/^[a-z][a-z0-9_-]{0,31}$/i.test(name) || !rawState || typeof rawState !== "object" || Array.isArray(rawState)) continue;
    const row = Number(rawState.row);
    const fps = Number(rawState.fps ?? 6);
    const frames = Array.isArray(rawState.frames) ? rawState.frames.filter(Number.isInteger).slice(0, 32) : [];
    if (!Number.isInteger(row) || row < 0 || row >= rows || !Number.isFinite(fps) || fps < 1 || fps > 30 || frames.length === 0 || frames.some((frame) => frame < 0 || frame >= columns)) continue;
    states[name.toLowerCase()] = { row, frames, fps };
  }
  if (Object.keys(states).length === 0) throw new Error("The pet manifest does not contain a usable state.");
  return { frameWidth, frameHeight, columns, rows, states };
}

async function readPetAssetData(filePath, maxBytes = petAssetLimitBytes) {
  const root = petAssetRoot();
  if (!filePath || !isWithin(root, filePath)) return null;
  const stat = await fs.stat(filePath).catch(() => null);
  if (!stat?.isFile() || stat.size > maxBytes) return null;
  const bytes = await fs.readFile(filePath);
  const mimeType = petMimeType(filePath);
  if (!mimeType) return null;
  return `data:${mimeType};base64,${bytes.toString("base64")}`;
}

async function readPetManifest(filePath) {
  const root = petAssetRoot();
  if (!filePath || !isWithin(root, filePath)) return undefined;
  const stat = await fs.stat(filePath).catch(() => null);
  if (!stat?.isFile() || stat.size > petManifestLimitBytes) return undefined;
  const raw = await fs.readFile(filePath, "utf8");
  return validatePetManifest(JSON.parse(raw));
}

async function buildPetWindowConfig() {
  let customSpriteDataUrl;
  let customManifest;
  try { customSpriteDataUrl = await readPetAssetData(petSettings.spritePath); } catch { customSpriteDataUrl = undefined; }
  try { customManifest = await readPetManifest(petSettings.manifestPath); } catch { customManifest = undefined; }
  return { ...petSettings, activity: petActivity, alwaysOnTop: true, ...(customSpriteDataUrl ? { customSpriteDataUrl } : {}), ...(customManifest ? { customManifest } : {}) };
}

function mainWindowBounds() {
  return mainWindow && !mainWindow.isDestroyed() ? mainWindow.getBounds() : null;
}

function clampPetPosition(position) {
  const bounds = mainWindowBounds();
  if (!bounds) return position;
  const maxX = Math.max(12, bounds.width - petWindowSize.width - 12);
  const maxY = Math.max(56, bounds.height - petWindowSize.height - 12);
  return { x: Math.max(12, Math.min(Math.round(position.x), maxX)), y: Math.max(56, Math.min(Math.round(position.y), maxY)) };
}

function syncPetWindowPosition() {
  if (!petWindow || petWindow.isDestroyed() || !mainWindow || mainWindow.isDestroyed()) return;
  const bounds = mainWindow.getBounds();
  if (petSettings.position.x < 0 || petSettings.position.y < 0) petSettings.position = { x: Math.max(12, bounds.width - petWindowSize.width - 28), y: Math.max(56, bounds.height - petWindowSize.height - 70) };
  petSettings.position = clampPetPosition(petSettings.position);
  petWindow.setPosition(bounds.x + petSettings.position.x, bounds.y + petSettings.position.y, false);
}

function sendPetPosition() {
  if (mainWindow && !mainWindow.isDestroyed()) mainWindow.webContents.send("pet:position", petSettings.position);
}

function sendPetConfig() {
  if (!petWindow || petWindow.isDestroyed() || !petWindow.webContents) return;
  void buildPetWindowConfig().then((config) => {
    if (petWindow && !petWindow.isDestroyed()) petWindow.webContents.send("pet:config", config);
  });
}

function stopPetWander() {
  if (petWanderTimer) clearInterval(petWanderTimer);
  petWanderTimer = undefined;
  petWanderTarget = undefined;
  petWanderNextAt = 0;
  if (petActivity === "walking") {
    petActivity = "idle";
    sendPetConfig();
  }
}

function stepPetWander() {
  if (!petSettings.visible || !petSettings.wander || petSettings.reduceMotion || petDragging || petActivity !== "idle" || !petWindow || petWindow.isDestroyed()) return;
  const bounds = mainWindowBounds();
  if (!bounds) return;
  const now = Date.now();
  if (!petWanderTarget && now >= petWanderNextAt) {
    const maxX = Math.max(12, bounds.width - petWindowSize.width - 12);
    const maxY = Math.max(56, bounds.height - petWindowSize.height - 12);
    petWanderTarget = { x: 12 + Math.random() * Math.max(1, maxX - 12), y: 56 + Math.random() * Math.max(1, maxY - 56) };
    petActivity = "walking";
    sendPetConfig();
  }
  if (!petWanderTarget) return;
  const current = petSettings.position;
  const dx = petWanderTarget.x - current.x;
  const dy = petWanderTarget.y - current.y;
  const distance = Math.max(1, Math.hypot(dx, dy));
  if (distance < 4) {
    petSettings.position = clampPetPosition(petWanderTarget);
    petWanderTarget = undefined;
    petWanderNextAt = now + 2_000 + Math.round(Math.random() * 4_000);
    petActivity = "idle";
    syncPetWindowPosition();
    sendPetPosition();
    sendPetConfig();
    return;
  }
  const step = Math.min(distance, 3.2);
  petSettings.position = clampPetPosition({ x: current.x + (dx / distance) * step, y: current.y + (dy / distance) * step });
  syncPetWindowPosition();
  sendPetPosition();
}

function configurePetWander() {
  stopPetWander();
  if (!petSettings.visible || !petSettings.wander || petSettings.reduceMotion) return;
  petWanderNextAt = Date.now() + 2_000;
  petWanderTimer = setInterval(stepPetWander, 120);
}

function createPetWindow() {
  if (petWindow && !petWindow.isDestroyed()) return petWindow;
  petWindow = new BrowserWindow({
    width: petWindowSize.width,
    height: petWindowSize.height,
    minWidth: petWindowSize.width,
    minHeight: petWindowSize.height,
    maxWidth: petWindowSize.width,
    maxHeight: petWindowSize.height,
    frame: false,
    transparent: true,
    hasShadow: false,
    resizable: false,
    movable: false,
    focusable: true,
    skipTaskbar: true,
    show: false,
    alwaysOnTop: true,
    backgroundColor: "#00000000",
    parent: mainWindow,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      preload: path.join(__dirname, "preload.cjs"),
    },
  });
  petWindow.setAlwaysOnTop(true, "floating");
  petWindow.setMenuBarVisibility(false);
  const devUrl = process.env.SERVER_ROCK_RENDERER_URL;
  if (devUrl) void petWindow.loadURL(`${devUrl}#pet`);
  else void petWindow.loadFile(path.join(rendererRoot, "dist", "index.html"), { hash: "pet" });
  petWindow.webContents.on("did-finish-load", () => { syncPetWindowPosition(); sendPetConfig(); if (petSettings.visible) petWindow.showInactive(); });
  petWindow.on("closed", () => { petWindow = undefined; petDragging = false; });
  return petWindow;
}

async function pickPetAsset(kind) {
  if (kind !== "sprite" && kind !== "manifest") throw new Error("Unknown pet asset type.");
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ["openFile"],
    filters: kind === "sprite"
      ? [{ name: "Pet sprites", extensions: ["png", "jpg", "jpeg", "webp"] }]
      : [{ name: "Pet manifests", extensions: ["json"] }],
  });
  if (result.canceled || !result.filePaths[0]) return null;
  const sourcePath = result.filePaths[0];
  const sourceStat = await fs.stat(sourcePath);
  const maxBytes = kind === "manifest" ? petManifestLimitBytes : petAssetLimitBytes;
  if (!sourceStat.isFile() || sourceStat.size > maxBytes) throw new Error(`The selected ${kind} is too large.`);
  const destinationRoot = petAssetRoot();
  await fs.mkdir(destinationRoot, { recursive: true });
  const destination = kind === "manifest" ? path.join(destinationRoot, "custom-manifest.json") : path.join(destinationRoot, `custom-sprite${path.extname(sourcePath).toLowerCase()}`);
  if (kind === "sprite") {
    if (!petMimeType(sourcePath)) throw new Error("Choose a PNG, JPEG, or WebP sprite image.");
    await fs.copyFile(sourcePath, destination);
  } else {
    const manifest = validatePetManifest(JSON.parse(await fs.readFile(sourcePath, "utf8")));
    await fs.writeFile(destination, JSON.stringify(manifest, null, 2), "utf8");
  }
  return { path: destination, fileName: path.basename(sourcePath) };
}

function createWindow() {
  mainWindow = new BrowserWindow({
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
  if (devUrl) mainWindow.loadURL(devUrl);
  else mainWindow.loadFile(path.join(rendererRoot, "dist", "index.html"));
  mainWindow.on("move", syncPetWindowPosition);
  mainWindow.on("resize", syncPetWindowPosition);
  mainWindow.on("closed", () => {
    stopPetWander();
    if (petWindow && !petWindow.isDestroyed()) petWindow.close();
    mainWindow = undefined;
  });
  createPetWindow();
  return mainWindow;
}

function applyProductionContentSecurityPolicy() {
  session.defaultSession.webRequest.onHeadersReceived((details, callback) => {
    callback({
      responseHeaders: {
        ...details.responseHeaders,
        "Content-Security-Policy": [
          "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob: http://127.0.0.1:8188 http://localhost:8188; media-src 'self' blob:; connect-src 'self'; font-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'",
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
      const result = await dialog.showOpenDialog(window, { properties: ["openFile", "multiSelections"], filters: [{ name: "Chat files", extensions: ["txt", "md", "markdown", "csv", "json", "xml", "yaml", "yml", "toml", "html", "css", "js", "ts", "jsx", "tsx", "cs", "py", "java", "go", "rs", "c", "h", "cpp", "hpp", "sh", "sql", "log", "pdf", "png", "jpg", "jpeg"] }] });
      if (result.canceled) return { v: 1, ok: true, result: { attachments: [] } };
      const attachments = [];
      let textCharacters = 0;
      let imageBytes = 0;
      for (const filePath of result.filePaths.slice(0, 5)) {
        const fileName = path.basename(filePath);
        const extension = path.extname(fileName).toLowerCase();
        if (isBlockedAttachmentName(fileName)) throw new Error(`That file name cannot be attached: ${fileName}`);
        const imageType = extension === ".png" ? "image/png" : [".jpg", ".jpeg"].includes(extension) ? "image/jpeg" : null;
        const isPdf = extension === ".pdf";
        if (!imageType && !isPdf && !attachmentTextExtensions.has(extension)) throw new Error(`That file type cannot be attached: ${fileName}`);
        const bytes = await fs.readFile(filePath);
        if (bytes.length > (isPdf ? 20 * 1024 * 1024 : 2 * 1024 * 1024)) throw new Error(`The selected file is too large: ${fileName}`);
        if (imageType) {
          if (!(imageType === "image/png" ? isPng(bytes) : isJpeg(bytes))) throw new Error(`The selected image is not valid: ${fileName}`);
          imageBytes += bytes.length;
          if (imageBytes > 6 * 1024 * 1024) throw new Error("Selected images exceed the combined size limit.");
          attachments.push({ fileName, mediaType: imageType, content: bytes.toString("base64") });
        } else if (isPdf) {
          attachments.push({ fileName, mediaType: null, content: bytes.toString("base64") });
        } else {
          const content = bytes.toString("utf8").slice(0, 16000);
          textCharacters += content.length;
          if (textCharacters > 48_000) throw new Error("Selected text files exceed the combined character limit.");
          attachments.push({ fileName, mediaType: null, content });
        }
      }
      return { v: 1, ok: true, result: { attachments } };
    }
    return requestAgent(method, params);
  });
  ipcMain.handle("pet:get-config", () => buildPetWindowConfig());
  ipcMain.handle("pet:pick-asset", async (_event, kind) => pickPetAsset(kind));
  ipcMain.on("pet:set-position", (_event, position) => {
    if (!position || !Number.isFinite(position.x) || !Number.isFinite(position.y)) return;
    petSettings.position = clampPetPosition({ x: position.x, y: position.y });
    syncPetWindowPosition();
  });
  ipcMain.on("pet:configure", (_event, settings) => {
    petSettings = sanitizePetSettings(settings);
    configurePetWander();
    syncPetWindowPosition();
    if (petWindow && !petWindow.isDestroyed()) {
      if (petSettings.visible) {
        petWindow.setAlwaysOnTop(true, "floating");
        if (petWindow.webContents.isLoading() === false) petWindow.showInactive();
      } else petWindow.hide();
    }
    sendPetConfig();
  });
  ipcMain.on("pet:activity", (_event, activity) => {
    petActivity = allowedPetActivities.has(activity) ? activity : "idle";
    if (petActivity !== "idle") petWanderTarget = undefined;
    sendPetConfig();
  });
  ipcMain.on("pet:drag-start", (_event, position) => {
    if (!petWindow || petWindow.isDestroyed() || !mainWindow || !Number.isFinite(position?.screenX) || !Number.isFinite(position?.screenY)) return;
    const bounds = petWindow.getBounds();
    petDragging = true;
    petDragOffset = { x: position.screenX - bounds.x, y: position.screenY - bounds.y };
  });
  ipcMain.on("pet:drag-move", (_event, position) => {
    if (!petDragging || !petWindow || petWindow.isDestroyed() || !mainWindow || !Number.isFinite(position?.screenX) || !Number.isFinite(position?.screenY)) return;
    const bounds = mainWindow.getBounds();
    petSettings.position = clampPetPosition({ x: position.screenX - bounds.x - petDragOffset.x, y: position.screenY - bounds.y - petDragOffset.y });
    syncPetWindowPosition();
    sendPetPosition();
  });
  ipcMain.on("pet:drag-end", () => { petDragging = false; });
  ipcMain.on("pet:hide", () => {
    petSettings.visible = false;
    stopPetWander();
    petWindow?.hide();
    if (mainWindow && !mainWindow.isDestroyed()) mainWindow.webContents.send("pet:visibility", false);
    sendPetConfig();
  });
  createWindow();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") stopAgentHost();
  if (process.platform !== "darwin") app.quit();
});

app.on("before-quit", () => {
  isQuitting = true;
  stopAgentHost();
});
