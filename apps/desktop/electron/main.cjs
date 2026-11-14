const { app, BrowserWindow, dialog, ipcMain, screen, session } = require("electron");
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
const chatHostDeadlineSeconds = 285;
let isQuitting = false;
let mainWindow;
let petWindow;
let petReady = false;
let petDragOffset = null;
let petWanderTarget = null;
let petWanderPauseTimer;
let petWanderStepTimer;
let petWanderMoving = false;
let petMotion = { wander: true, reduceMotion: false };
const pendingPetMessages = [];
const PET_WINDOW_WIDTH = 220;
const PET_WINDOW_HEIGHT = 190;
const PET_WINDOW_MARGIN = 16;
const attachmentTextExtensions = new Set([".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".yaml", ".yml", ".toml", ".html", ".htm", ".css", ".js", ".ts", ".jsx", ".tsx", ".cs", ".py", ".java", ".go", ".rs", ".c", ".h", ".cpp", ".hpp", ".sh", ".sql", ".log"]);
const blockedAttachmentNames = new Set([".env", ".env.local", ".env.production", "id_rsa", "id_ed25519", "credentials", "secrets"]);
const isBlockedAttachmentName = (name) => blockedAttachmentNames.has(name.toLowerCase()) || name.toLowerCase().includes(".env.");
const isPng = (bytes) => bytes.length >= 8 && bytes.subarray(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]));
const isJpeg = (bytes) => bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff;

function decodeDroppedAttachment(attachment) {
  if (!attachment || typeof attachment.fileName !== "string" || typeof attachment.content !== "string") throw new Error("Dropped files must include a name and content.");
  const rawName = attachment.fileName.trim();
  const fileName = path.basename(rawName).slice(0, 240);
  if (!fileName || fileName !== rawName || isBlockedAttachmentName(fileName)) throw new Error(`That file name cannot be attached: ${rawName.slice(0, 80)}.`);
  if (attachment.content.length > 28_000_000 || attachment.content.length % 4 === 1 || !/^[A-Za-z0-9+/]*={0,2}$/.test(attachment.content)) throw new Error(`The dropped file is not a valid local payload: ${fileName}`);
  const extension = path.extname(fileName).toLowerCase();
  const imageType = extension === ".png" ? "image/png" : [".jpg", ".jpeg"].includes(extension) ? "image/jpeg" : null;
  const isPdf = extension === ".pdf";
  if (!imageType && !isPdf && !attachmentTextExtensions.has(extension)) throw new Error(`That file type cannot be attached: ${fileName}`);
  const bytes = Buffer.from(attachment.content, "base64");
  if (bytes.length > (isPdf ? 20 * 1024 * 1024 : 2 * 1024 * 1024)) throw new Error(`The dropped file is too large: ${fileName}`);
  if (imageType) {
    if (!(imageType === "image/png" ? isPng(bytes) : isJpeg(bytes))) throw new Error(`The dropped image is not valid: ${fileName}`);
    return { fileName, mediaType: imageType, content: bytes.toString("base64"), byteLength: bytes.length, textLength: 0 };
  }
  if (isPdf) return { fileName, mediaType: null, content: bytes.toString("base64"), byteLength: bytes.length, textLength: 0 };
  const content = bytes.toString("utf8").slice(0, 16_000);
  return { fileName, mediaType: null, content, byteLength: bytes.length, textLength: content.length };
}

function petStatePath() {
  return path.join(app.getPath("userData"), "pet-window.json");
}

function readPetState() {
  try {
    const parsed = JSON.parse(fsSync.readFileSync(petStatePath(), "utf8"));
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function petBounds() {
  const stored = readPetState().bounds;
  const width = PET_WINDOW_WIDTH;
  const height = PET_WINDOW_HEIGHT;
  const workArea = screen.getPrimaryDisplay().workArea;
  if (!stored || ![stored.x, stored.y].every(Number.isFinite)) {
    return { x: workArea.x + workArea.width - width - 28, y: workArea.y + workArea.height - height - 28, width, height };
  }

  // A saved position can outlive a monitor being disconnected. Keep Percy
  // recoverable instead of creating an invisible companion window.
  const display = screen.getAllDisplays().find(({ workArea: area }) => stored.x < area.x + area.width && stored.x + width > area.x && stored.y < area.y + area.height && stored.y + height > area.y) ?? screen.getPrimaryDisplay();
  const area = display.workArea;
  return {
    x: Math.min(Math.max(stored.x, area.x + PET_WINDOW_MARGIN), area.x + area.width - width - PET_WINDOW_MARGIN),
    y: Math.min(Math.max(stored.y, area.y + PET_WINDOW_MARGIN), area.y + area.height - height - PET_WINDOW_MARGIN),
    width,
    height,
  };
}

function stopPetWander() {
  if (petWanderPauseTimer) clearTimeout(petWanderPauseTimer);
  if (petWanderStepTimer) clearTimeout(petWanderStepTimer);
  petWanderPauseTimer = undefined;
  petWanderStepTimer = undefined;
  petWanderTarget = null;
  petWanderMoving = false;
}

function petWorkArea() {
  if (!petWindow || petWindow.isDestroyed()) return screen.getPrimaryDisplay().workArea;
  const bounds = petWindow.getBounds();
  return screen.getDisplayNearestPoint({ x: bounds.x + bounds.width / 2, y: bounds.y + bounds.height / 2 }).workArea;
}

function clampPetPosition(point) {
  const area = screen.getDisplayNearestPoint(point).workArea;
  return {
    x: Math.min(Math.max(Math.round(point.x), area.x + PET_WINDOW_MARGIN), area.x + area.width - PET_WINDOW_WIDTH - PET_WINDOW_MARGIN),
    y: Math.min(Math.max(Math.round(point.y), area.y + PET_WINDOW_MARGIN), area.y + area.height - PET_WINDOW_HEIGHT - PET_WINDOW_MARGIN),
  };
}

function schedulePetWander() {
  if (!petWindow || petWindow.isDestroyed() || !petWindow.isVisible() || !petMotion.wander || petMotion.reduceMotion || petDragOffset) return;
  if (petWanderPauseTimer || petWanderStepTimer) return;
  petWanderPauseTimer = setTimeout(() => {
    petWanderPauseTimer = undefined;
    if (!petWindow || petWindow.isDestroyed() || !petWindow.isVisible() || !petMotion.wander || petMotion.reduceMotion || petDragOffset) return;
    const area = petWorkArea();
    const bounds = petWindow.getBounds();
    const minX = area.x + PET_WINDOW_MARGIN;
    const maxX = Math.max(minX, area.x + area.width - PET_WINDOW_WIDTH - PET_WINDOW_MARGIN);
    const y = Math.min(Math.max(bounds.y, area.y + PET_WINDOW_MARGIN), area.y + area.height - PET_WINDOW_HEIGHT - PET_WINDOW_MARGIN);
    let x = Math.round(minX + Math.random() * Math.max(1, maxX - minX));
    if (Math.abs(x - bounds.x) < 96) x = bounds.x < (minX + maxX) / 2 ? maxX : minX;
    petWanderTarget = { x, y };
    movePetTowardTarget();
  }, 1800 + Math.round(Math.random() * 2600));
}

function movePetTowardTarget() {
  if (!petWindow || petWindow.isDestroyed() || !petWindow.isVisible() || !petWanderTarget || !petMotion.wander || petMotion.reduceMotion || petDragOffset) {
    petWanderStepTimer = undefined;
    petWanderMoving = false;
    return;
  }
  const bounds = petWindow.getBounds();
  const dx = petWanderTarget.x - bounds.x;
  const dy = petWanderTarget.y - bounds.y;
  if (Math.abs(dx) <= 3 && Math.abs(dy) <= 3) {
    petWindow.setPosition(petWanderTarget.x, petWanderTarget.y);
    petWanderTarget = null;
    petWanderStepTimer = undefined;
    petWanderMoving = false;
    schedulePetWander();
    return;
  }
  const step = Math.min(3, Math.max(Math.abs(dx), Math.abs(dy)));
  petWanderMoving = true;
  petWindow.setPosition(Math.round(bounds.x + Math.sign(dx) * Math.min(Math.abs(dx), step)), Math.round(bounds.y + Math.sign(dy) * Math.min(Math.abs(dy), step)));
  petWanderStepTimer = setTimeout(movePetTowardTarget, 55);
}

function persistPetState() {
  if (!petWindow || petWindow.isDestroyed()) return;
  const state = readPetState();
  state.visible = petWindow.isVisible();
  state.bounds = petWindow.getBounds();
  void fs.mkdir(app.getPath("userData"), { recursive: true }).then(() => fs.writeFile(petStatePath(), JSON.stringify(state, null, 2), "utf8")).catch(() => {});
}

function sendPetVisibility(visible) {
  if (mainWindow && !mainWindow.isDestroyed()) mainWindow.webContents.send("pet:visibility", visible);
}

function setPetVisible(visible) {
  if (!petWindow || petWindow.isDestroyed()) return;
  if (visible) {
    petWindow.showInactive();
    schedulePetWander();
  } else {
    stopPetWander();
    petWindow.hide();
  }
  if (visible && petReady) flushPetMessages();
  persistPetState();
  sendPetVisibility(visible);
}

function flushPetMessages() {
  if (!petWindow || petWindow.isDestroyed() || !petReady || !petWindow.isVisible()) return;
  for (const event of pendingPetMessages.splice(0)) petWindow.webContents.send("pet:message", event);
}

function sendPetMessage(text, kind = "success") {
  if (!petWindow || petWindow.isDestroyed()) return;
  const event = { text, kind, at: Date.now() };
  if (!petReady) {
    pendingPetMessages.push(event);
    while (pendingPetMessages.length > 3) pendingPetMessages.shift();
    return;
  }
  if (!petWindow.isVisible()) {
    pendingPetMessages.push(event);
    while (pendingPetMessages.length > 3) pendingPetMessages.shift();
    return;
  }
  petWindow.webContents.send("pet:message", event);
}

function petTaskLabel(method) {
  const labels = {
    "chat.complete": "The local reply",
    "project.save": "The local project save",
    "project.file.write": "The reviewed file save",
    "project.file.undo": "The file restore",
    "project.command.run": "The local project command",
    "tasks.plan": "The local task plan",
    "tasks.queue": "The local task queue",
    "tasks.retry": "The local task retry",
    "tasks.cancel": "The local task stop",
    "scheduled.create": "The local schedule",
    "scheduled.pause": "The scheduled task pause",
    "scheduled.resume": "The scheduled task resume",
    "scheduled.cancel": "The scheduled task cancellation",
    "scheduled.runNow": "The scheduled task run",
    "memory.save": "The approved memory note",
    "image.generate": "The local image workflow",
  };
  return labels[method] ?? null;
}

function petCompletionText(method, response) {
  const task = petTaskLabel(method);
  if (!task) return null;
  if (!response?.ok) return { text: `${task} needs attention. Open Skai for details.`, kind: "error" };
  const messages = {
    "chat.complete": "Finished your local reply.",
    "project.save": "Saved the local project.",
    "project.file.write": "Saved the reviewed file.",
    "project.file.undo": "Restored the previous file version.",
    "project.command.run": "Finished the local project command.",
    "tasks.plan": "Created a local task plan.",
    "tasks.queue": "Queued the local task.",
    "tasks.retry": "Retried the local task.",
    "tasks.cancel": "Stopped the local task.",
    "scheduled.create": "Scheduled the local task for review.",
    "scheduled.pause": "Paused the local scheduled task.",
    "scheduled.resume": "Resumed the local scheduled task.",
    "scheduled.cancel": "Canceled the scheduled local task.",
    "scheduled.runNow": "Marked the local scheduled task due.",
    "memory.save": "Saved the approved memory note.",
    "image.generate": "Finished the local image workflow.",
  };
  const text = messages[method];
  return text ? { text, kind: "success" } : null;
}

function createPetWindow() {
  if (petWindow && !petWindow.isDestroyed()) return petWindow;
  const savedState = readPetState();
  petReady = false;
  petWindow = new BrowserWindow({
    ...petBounds(),
    transparent: true,
    frame: false,
    resizable: false,
    movable: true,
    minimizable: false,
    maximizable: false,
    closable: false,
    skipTaskbar: true,
    alwaysOnTop: true,
    hasShadow: false,
    backgroundColor: "#00000000",
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      preload: path.join(__dirname, "preload.cjs"),
    },
  });
  petWindow.setAlwaysOnTop(true, "floating");
  petWindow.on("moved", () => { if (!petWanderMoving) persistPetState(); });
  petWindow.webContents.on("did-finish-load", () => {
    petReady = true;
    flushPetMessages();
  });
  petWindow.on("closed", () => { petReady = false; petWindow = undefined; });
  const devUrl = process.env.SERVER_ROCK_RENDERER_URL;
  if (devUrl) {
    const url = new URL(devUrl);
    url.searchParams.set("pet", "1");
    void petWindow.loadURL(url.toString());
  } else {
    void petWindow.loadFile(path.join(rendererRoot, "dist", "index.html"), { query: { pet: "1" } });
  }
  petWindow.once("ready-to-show", () => {
    persistPetState();
    if (savedState.visible !== false) {
      petWindow?.showInactive();
      schedulePetWander();
    }
  });
  return petWindow;
}
const allowedMethods = new Set([
  "runtime.health", "models.list", "projects.list", "project.save", "project.files", "project.file.read", "tasks.list",
  "scheduled.list", "scheduled.create", "scheduled.pause", "scheduled.resume", "scheduled.cancel", "scheduled.runNow", "tasks.plan", "tasks.queue", "tasks.cancel", "tasks.retry", "conversations.list", "conversations.search", "conversation.save", "conversation.metadata.save", "conversations.export", "conversation.fork", "memory.list", "memory.search", "memory.save", "memory.remove", "memory.enabled", "memory.export", "settings.get",
  "settings.save", "image.generate", "project.file.write", "project.file.undo", "project.command.run", "chat.queue.list", "chat.queue.enqueue", "chat.queue.remove", "chat.complete", "workspace.attachment.extract", "voice.models", "voice.speak", "voice.stop",
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
    const completion = petCompletionText(entry.method, message);
    if (completion) sendPetMessage(completion.text, completion.kind);
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
      const completion = petCompletionText(entry.method, error);
      if (completion) sendPetMessage(completion.text, completion.kind);
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
    const response = { v: 1, ok: false, error: { code: "HOST_UNAVAILABLE", message: "Agent host is not running." } };
    const completion = petCompletionText(method, response);
    if (completion) sendPetMessage(completion.text, completion.kind);
    return Promise.resolve(response);
  }
  const id = `desktop-${nextRequestId++}`;
  const requestParams = method === "chat.complete"
    ? { ...params, timeoutSeconds: chatHostDeadlineSeconds }
    : params;
  return new Promise((resolve) => {
    const timeoutMs = method === "chat.complete" ? chatRequestTimeoutMs : standardRequestTimeoutMs;
    const timer = setTimeout(() => {
      if (!pending.has(id)) return;
      pending.delete(id);
      const response = { v: 1, ok: false, error: { code: "HOST_TIMEOUT", message: `The local agent host did not answer ${method} within the allowed time.` } };
      const completion = petCompletionText(method, response);
      if (completion) sendPetMessage(completion.text, completion.kind);
      resolve(response);
      restartAgentHost();
    }, timeoutMs);
    pending.set(id, { resolve, timer, method });
    try {
      host.stdin.write(`${JSON.stringify({ v: 1, id, method, params: requestParams })}\n`);
    } catch {
      clearTimeout(timer);
      pending.delete(id);
      const response = { v: 1, ok: false, error: { code: "HOST_WRITE_FAILED", message: "The local agent host could not accept the request." } };
      const completion = petCompletionText(method, response);
      if (completion) sendPetMessage(completion.text, completion.kind);
      resolve(response);
    }
  });
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
  mainWindow.on("closed", () => { mainWindow = undefined; if (petWindow && !petWindow.isDestroyed()) petWindow.close(); });
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
    if (method === "workspace.importDroppedFiles") {
      if (!params || !Array.isArray(params.attachments)) throw new Error("Dropped attachments were not provided.");
      const attachments = [];
      let textCharacters = 0;
      let imageBytes = 0;
      for (const candidate of params.attachments.slice(0, 5)) {
        const attachment = decodeDroppedAttachment(candidate);
        textCharacters += attachment.textLength;
        imageBytes += attachment.mediaType ? attachment.byteLength : 0;
        if (textCharacters > 48_000) throw new Error("Dropped text files exceed the combined character limit.");
        if (imageBytes > 6 * 1024 * 1024) throw new Error("Dropped images exceed the combined size limit.");
        const { byteLength, textLength, ...safeAttachment } = attachment;
        attachments.push(safeAttachment);
      }
      return { v: 1, ok: true, result: { attachments } };
    }
    return requestAgent(method, params);
  });
  ipcMain.on("pet:set-visible", (event, visible) => {
    if (BrowserWindow.fromWebContents(event.sender)) setPetVisible(Boolean(visible));
  });
  ipcMain.on("pet:set-motion", (event, settings) => {
    if (!BrowserWindow.fromWebContents(event.sender) || !settings || typeof settings !== "object") return;
    petMotion = { wander: Boolean(settings.wander), reduceMotion: Boolean(settings.reduceMotion) };
    stopPetWander();
    petWindow?.webContents.send("pet:motion", petMotion);
    schedulePetWander();
  });
  ipcMain.on("pet:drag-start", (event, point) => {
    if (BrowserWindow.fromWebContents(event.sender) !== petWindow || !point || !Number.isFinite(point.x) || !Number.isFinite(point.y)) return;
    stopPetWander();
    const bounds = petWindow.getBounds();
    petDragOffset = { x: point.x - bounds.x, y: point.y - bounds.y };
  });
  ipcMain.on("pet:drag-move", (event, point) => {
    if (BrowserWindow.fromWebContents(event.sender) !== petWindow || !petDragOffset || !point || !Number.isFinite(point.x) || !Number.isFinite(point.y)) return;
    const position = clampPetPosition({ x: point.x - petDragOffset.x, y: point.y - petDragOffset.y });
    petWindow.setPosition(position.x, position.y);
  });
  ipcMain.on("pet:drag-end", (event) => {
    if (BrowserWindow.fromWebContents(event.sender) !== petWindow) return;
    petDragOffset = null;
    persistPetState();
    schedulePetWander();
  });
  createWindow();
  createPetWindow();
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
  stopPetWander();
  if (petWindow && !petWindow.isDestroyed()) petWindow.destroy();
  stopAgentHost();
});
