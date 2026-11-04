const {
  app,
  BrowserWindow,
  protocol,
  net,
  ipcMain,
  dialog,
  shell,
  safeStorage,
} = require("electron");
const path = require("node:path");
const fs = require("node:fs/promises");
const fsSync = require("node:fs");
const { spawn } = require("node:child_process");
const http = require("node:http");
const crypto = require("node:crypto");
const { execFile } = require("node:child_process");
const { promisify } = require("node:util");
const execFileAsync = promisify(execFile);
const { createWorkspace, isProtectedPath, readUtf8Limited } = require("./workspace.cjs");
const { createProviderBridge, validateEndpoint } = require("./provider-bridge.cjs");
const {
  readJsonBodyLimited,
  readTextBodyLimited,
  readBinaryBodyLimited,
  MAX_STREAM_TEXT_LENGTH,
  MAX_STREAM_BUFFER_LENGTH,
} = require("./response-limits.cjs");
const { createSafeChildEnvironment } = require("./child-environment.cjs");
const { resolveModelFilePath, ensureNoSymlinkComponents } = require("./model-path-safety.cjs");
const { validateRequestId, validateChatGPTRequest, validateProviderName } = require("./ipc-validation.cjs");
const { readTextFileLimited } = require("./bounded-file.cjs");
const { createTerminalManager } = require("./terminal-sessions.cjs");
const { fetchReleaseStatus, releasePageUrl } = require("./github-updates.cjs");
const MAX_MODEL_BYTES = 10 * 1024 * 1024 * 1024;
const MAX_GITHUB_DIFF_BYTES = 256 * 1024;
const localPipelines = new Map();
const activeGenerations = new Map();
const activeChatGPTRequests = new Map();
const activeProviderRequests = new Map();
const githubDeviceRequests = new Map();
let terminalManager = null;
let chatGPTJwks = null;
const CHATGPT_AUTH = "https://auth.openai.com/api/accounts/oauth";
const CHATGPT_RESOURCE = "https://api.openai.com/v1";
const chatGPTStorePath = () =>
  path.join(app.getPath("userData"), "chatgpt-plan.enc");
const providerSecretsPath = () =>
  path.join(app.getPath("userData"), "provider-secrets.enc");
function chatGPTEncryptionReady() {
  try {
    if (!safeStorage.isEncryptionAvailable()) return false;
    return (
      process.platform !== "linux" ||
      safeStorage.getSelectedStorageBackend() !== "basic_text"
    );
  } catch {
    return false;
  }
}
async function writeProtectedFile(destination, content) {
  await fs.mkdir(path.dirname(destination), { recursive: true });
  const temp = path.join(
    path.dirname(destination),
    `.${path.basename(destination)}-${crypto.randomUUID()}.tmp`,
  );
  try {
    await fs.writeFile(temp, content, { encoding: "utf8", mode: 0o600, flag: "wx" });
    await fs.rename(temp, destination);
  } finally {
    await fs.rm(temp, { force: true }).catch(() => {});
  }
}
async function quarantineUnreadableFile(filePath) {
  try {
    await fs.access(filePath);
    const recoveryPath = `${filePath}.corrupt-${crypto.randomUUID()}`;
    await fs.rename(filePath, recoveryPath);
    console.warn(`Preserved unreadable credential data at ${recoveryPath}`);
  } catch {
    // Missing or inaccessible files cannot be quarantined; callers still get a clean state.
  }
}
async function readChatGPTState() {
  try {
    if (!chatGPTEncryptionReady())
      return { profiles: [], hostId: "urn:uuid:" + crypto.randomUUID() };
    const encoded = await readTextFileLimited(chatGPTStorePath(), undefined, "ChatGPT credential store");
    return JSON.parse(
      safeStorage.decryptString(Buffer.from(encoded, "base64")),
    );
  } catch (error) {
    if (error?.code !== "ENOENT") await quarantineUnreadableFile(chatGPTStorePath());
    return { profiles: [], hostId: "urn:uuid:" + crypto.randomUUID() };
  }
}
async function saveChatGPTState(data) {
  if (!chatGPTEncryptionReady())
    throw Error(
      "Secure OS credential storage is unavailable. Configure a system keychain and try again.",
    );
  const output = safeStorage
      .encryptString(JSON.stringify(data))
      .toString("base64"),
    dest = chatGPTStorePath();
  await writeProtectedFile(dest, output);
}
async function readProviderSecrets() {
  if (!chatGPTEncryptionReady()) return {};
  try {
    const encoded = await readTextFileLimited(providerSecretsPath(), undefined, "Provider credential store");
    return JSON.parse(
      safeStorage.decryptString(Buffer.from(encoded, "base64")),
    );
  } catch (error) {
    if (error?.code !== "ENOENT") await quarantineUnreadableFile(providerSecretsPath());
    return {};
  }
}
async function writeProviderSecret(provider, secret) {
  if (!chatGPTEncryptionReady())
    throw Error("Secure OS credential storage is unavailable.");
  if (typeof provider !== "string" || !/^[\w -]{1,64}$/.test(provider))
    throw Error("Choose a valid provider.");
  if (typeof secret !== "string" || secret.length > 4096)
    throw Error("Provider keys must be 4096 characters or fewer.");
  const secrets = await readProviderSecrets();
  if (secret.trim()) secrets[provider] = secret.trim();
  else delete secrets[provider];
  const dest = providerSecretsPath(),
    encoded = safeStorage
      .encryptString(JSON.stringify(secrets))
      .toString("base64");
  await writeProtectedFile(dest, encoded);
  return { saved: Boolean(secret.trim()) };
}

async function checkGitHubRelease(channel) {
  return fetchReleaseStatus({ channel, currentVersion: app.getVersion(), platform: process.platform, arch: process.arch });
}
async function getChatGPTProfile() {
  const data = await readChatGPTState();
  return data.profiles?.[0] || null;
}
async function chatGPTStatus() {
  const profile = await getChatGPTProfile();
  return profile
    ? {
        connected: true,
        sharing: profile.scopes?.includes("chatgpt.tokens.use.direct") === true,
        email: profile.email || "",
        name: profile.name || "",
      }
    : { connected: false, sharing: false, email: "" };
}
async function chatGPTSignIn() {
  if (!chatGPTEncryptionReady())
    throw Error(
      "Secure OS credential storage is unavailable. Configure a system keychain and try again.",
    );
  const saved = await readChatGPTState(),
    profile = saved.profiles?.[0] || null,
    state = crypto.randomBytes(24).toString("base64url"),
    nonce = crypto.randomBytes(24).toString("base64url"),
    verifier = crypto.randomBytes(48).toString("base64url"),
    challenge = crypto
      .createHash("sha256")
      .update(verifier)
      .digest("base64url");
  let resolveCallback, rejectCallback;
  const callback = new Promise((resolve, reject) => {
    resolveCallback = resolve;
    rejectCallback = reject;
  });
  const server = http.createServer((req, res) => {
    const incoming = new URL(req.url, "http://127.0.0.1");
    if (incoming.pathname !== "/auth/callback") {
      res.writeHead(404);
      res.end("Not found");
      return;
    }
    if (incoming.searchParams.get("state") !== state) {
      res.writeHead(400, { "Content-Type": "text/plain" });
      res.end("Sign-in state did not match. You can close this tab.");
      rejectCallback(Error("ChatGPT sign-in state did not match."));
      return;
    }
    if (incoming.searchParams.get("error")) {
      res.writeHead(200, { "Content-Type": "text/html" });
      res.end(
        "<h2>Connection not completed</h2><p>You can return to Server Rock: AI.</p>",
      );
      rejectCallback(
        Error(
          incoming.searchParams.get("error_description") ||
            incoming.searchParams.get("error"),
        ),
      );
      return;
    }
    res.writeHead(200, {
      "Content-Type": "text/html; charset=utf-8",
      "Cache-Control": "no-store",
    });
    res.end(
      '<!doctype html><meta charset="utf-8"><title>Connected</title><p style="font:16px sans-serif">ChatGPT connected. Return to Server Rock: AI.</p>',
    );
    resolveCallback(incoming.searchParams);
  });
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  const port = server.address().port,
    redirectUri = "http://127.0.0.1:" + port + "/auth/callback",
    clientId = profile?.clientId || "dynamic_agent_client",
    authorize = new URL(CHATGPT_AUTH + "/authorize");
  for (const [k, v] of Object.entries({
    client_id: clientId,
    response_type: "code",
    redirect_uri: redirectUri,
    scope:
      "openid profile email offline_access resource.invoke chatgpt.tokens.use.direct",
    resource: CHATGPT_RESOURCE,
    state,
    nonce,
    code_challenge_method: "S256",
    code_challenge: challenge,
    ext_agent_host_id: saved.hostId || "urn:uuid:" + crypto.randomUUID(),
    ...(profile
      ? {
          id_token_hint: profile.idToken,
          ...(profile.email ? { login_hint: profile.email } : {}),
        }
      : { agent_name_hint: "Server Rock: AI" }),
  }))
    authorize.searchParams.set(k, v);
  let timeout;
  try {
    saved.hostId = authorize.searchParams.get("ext_agent_host_id");
    await saveChatGPTState(saved);
    await shell.openExternal(authorize.href);
    timeout = setTimeout(
      () => rejectCallback(Error("Sign-in timed out. Try again.")),
      5 * 60 * 1000,
    );
    const query = await callback;
    const code = query.get("code");
    if (!code) throw Error("ChatGPT did not return an authorization code.");
    const issuedClientId = query.get("client_id") || profile?.clientId;
    if (!issuedClientId || issuedClientId === "dynamic_agent_client")
      throw Error("ChatGPT did not return a registered client ID.");
    if (profile && issuedClientId !== profile.clientId)
      throw Error(
        "The returning account did not match the selected connection.",
      );
    const form = new URLSearchParams({
      grant_type: "authorization_code",
      client_id: issuedClientId,
      code,
      code_verifier: verifier,
      redirect_uri: redirectUri,
      resource: CHATGPT_RESOURCE,
    });
    const tokenResponse = await net.fetch(CHATGPT_AUTH + "/token", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: form.toString(),
    });
    if (!tokenResponse.ok)
      throw Error(
        "Token exchange failed: " +
          tokenResponse.status +
          " " +
          (await tokenResponse.text()).slice(0, 240),
      );
    const tokens = await readJsonBodyLimited(tokenResponse, "ChatGPT sign-in token");
    if (!tokens.id_token || !tokens.access_token)
      throw Error("ChatGPT returned an incomplete sign-in response.");
    const discoveryResponse = await net.fetch(
      "https://auth.openai.com/.well-known/openid-configuration",
    );
    if (!discoveryResponse.ok)
      throw Error("Could not verify ChatGPT identity metadata.");
    const metadata = await readJsonBodyLimited(discoveryResponse, "ChatGPT sign-in metadata");
    if (!metadata.jwks_uri)
      throw Error(
        "ChatGPT identity metadata did not include a signing-key URL.",
      );
    if (!chatGPTJwks)
      chatGPTJwks = (await import("jose")).createRemoteJWKSet(
        new URL(metadata.jwks_uri),
      );
    const jose = await import("jose"),
      verified = await jose.jwtVerify(tokens.id_token, chatGPTJwks, {
        issuer: metadata.issuer || "https://auth.openai.com",
        audience: issuedClientId,
        nonce,
      });
    const identity = verified.payload;
    if (!identity.sub)
      throw Error("ChatGPT identity did not contain a subject.");
    const scopes = (tokens.scope || query.get("scope") || "")
      .split(/\s+/)
      .filter(Boolean);
    if (profile && profile.subject !== identity.sub)
      throw Error(
        "The signed-in ChatGPT account did not match the saved profile.",
      );
    const next = {
      ...saved,
      profiles: [
        {
          clientId: issuedClientId,
          subject: identity.sub,
          email: identity.email || "",
          name: identity.name || "",
          idToken: tokens.id_token,
          accessToken: tokens.access_token,
          refreshToken: tokens.refresh_token || profile?.refreshToken || "",
          expiresAt: Date.now() + Number(tokens.expires_in || 3600) * 1000,
          scopes,
          savedAt: Date.now(),
        },
      ],
    };
    await saveChatGPTState(next);
    return await chatGPTStatus();
  } finally {
    clearTimeout(timeout);
    server.close();
  }
}
async function getChatGPTAccessToken() {
  const data = await readChatGPTState(),
    profile = data.profiles?.[0];
  if (!profile) throw Error("Connect your ChatGPT account in Settings first.");
  if (!profile.scopes?.includes("chatgpt.tokens.use.direct"))
    throw Error(
      "This ChatGPT connection did not grant plan usage. Reconnect and allow model requests.",
    );
  if (profile.expiresAt > Date.now() + 120000) return profile;
  const form = new URLSearchParams({
    grant_type: "refresh_token",
    client_id: profile.clientId,
    refresh_token: profile.refreshToken,
    resource: CHATGPT_RESOURCE,
  });
  const response = await net.fetch(CHATGPT_AUTH + "/token", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: form.toString(),
  });
  if (!response.ok)
    throw Error("ChatGPT session expired. Reconnect your account in Settings.");
  const tokens = await readJsonBodyLimited(response, "ChatGPT token");
  const updated = {
    ...profile,
    accessToken: tokens.access_token,
    refreshToken: tokens.refresh_token || profile.refreshToken,
    expiresAt: Date.now() + Number(tokens.expires_in || 3600) * 1000,
    scopes: (tokens.scope || profile.scopes.join(" "))
      .split(/\s+/)
      .filter(Boolean),
    savedAt: Date.now(),
  };
  data.profiles = [updated];
  await saveChatGPTState(data);
  return updated;
}
async function chatGPTModels() {
  const profile = await getChatGPTAccessToken(),
    response = await net.fetch(CHATGPT_RESOURCE + "/models", {
      headers: { Authorization: "Bearer " + profile.accessToken },
    });
  if (!response.ok)
    throw Error("Could not list ChatGPT plan models: " + response.status);
  const result = await readJsonBodyLimited(response, "ChatGPT model list");
  return (result.data || []).map((x) => x.id).filter(Boolean);
}
async function chatGPTStream(event, messages, model, requestId) {
  validateChatGPTRequest(messages, model, requestId);
  const profile = await getChatGPTAccessToken(),
    controller = new AbortController();
  activeChatGPTRequests.set(requestId, controller);
  try {
    const response = await net.fetch(CHATGPT_RESOURCE + "/responses", {
      method: "POST",
      signal: controller.signal,
      headers: {
        Authorization: "Bearer " + profile.accessToken,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        model,
        input: messages.map((m) => ({ role: m.role, content: m.content })),
        stream: true,
        store: false,
      }),
    });
    if (!response.ok)
      throw Error(
        "ChatGPT request failed: " +
          response.status +
          " " +
          (await readTextBodyLimited(response, "ChatGPT" )).slice(0, 300),
      );
    const reader = response.body.getReader(),
      decoder = new TextDecoder();
    let carry = "";
    let streamTextLength = 0;
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      carry += decoder.decode(value, { stream: true });
      const lines = carry.split("\n");
      carry = lines.pop();
      if (carry.length > MAX_STREAM_BUFFER_LENGTH)
        throw Error("ChatGPT response stream frame exceeded the 256 KB limit.");
      for (const line of lines) {
        if (!line.startsWith("data:")) continue;
        const raw = line.slice(5).trim();
        if (!raw || raw === "[DONE]") continue;
        let item;
        try {
          item = JSON.parse(raw);
        } catch {
          continue;
        }
        if (
          item.type === "response.output_text.delta" &&
          typeof item.delta === "string"
        ) {
          streamTextLength += item.delta.length;
          if (streamTextLength > MAX_STREAM_TEXT_LENGTH)
            throw Error("ChatGPT response exceeded the 2 MB stream limit.");
          event.sender.send("chatgpt:chunk", { requestId, text: item.delta });
        }
        if (item.type === "error" || item.type === "response.failed")
          throw Error(
            item.error?.message ||
              item.response?.error?.message ||
              "ChatGPT response failed.",
          );
      }
    }
    return { done: true };
  } finally {
    activeChatGPTRequests.delete(requestId);
  }
}
let transformers;
async function getTransformers() {
  return (transformers ??= await import("@huggingface/transformers"));
}
function modelDirectory(repoId) {
  if (typeof repoId !== "string" || !/^[\w.-]+\/[\w.-]+$/.test(repoId))
    throw Error("Enter a valid Hugging Face model ID.");
  return path.join(
    app.getPath("userData"),
    "models",
    repoId.replaceAll("/", "--"),
  );
}
async function scanModelHosts() {
  const targets = [
    {
      name: "Ollama",
      kind: "ollama",
      base: "http://127.0.0.1:11434",
      path: "/api/tags",
      endpoint: "http://127.0.0.1:11434/v1/chat/completions",
    },
    {
      name: "GPT4All",
      kind: "openai",
      base: "http://127.0.0.1:4891",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:4891/v1/chat/completions",
    },
    {
      name: "LM Studio",
      kind: "openai",
      base: "http://127.0.0.1:1234",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:1234/v1/chat/completions",
    },
    {
      name: "Jan",
      kind: "openai",
      base: "http://127.0.0.1:1337",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:1337/v1/chat/completions",
    },
    {
      name: "LocalAI",
      kind: "openai",
      base: "http://127.0.0.1:8080",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:8080/v1/chat/completions",
    },
    {
      name: "vLLM",
      kind: "openai",
      base: "http://127.0.0.1:8000",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:8000/v1/chat/completions",
    },
    {
      name: "llama.cpp",
      kind: "openai",
      base: "http://127.0.0.1:8080",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:8080/v1/chat/completions",
    },
    {
      name: "KoboldCpp",
      kind: "openai",
      base: "http://127.0.0.1:5001",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:5001/v1/chat/completions",
    },
    {
      name: "Text Generation WebUI",
      kind: "openai",
      base: "http://127.0.0.1:5000",
      path: "/v1/models",
      endpoint: "http://127.0.0.1:5000/v1/chat/completions",
    },
  ];
  const found = [];
  const results = await Promise.all(
    targets.map(async (target) => {
      const controller = new AbortController(),
        timer = setTimeout(() => controller.abort(), 1400);
      try {
        const response = await net.fetch(target.base + target.path, {
          signal: controller.signal,
        });
        if (!response.ok) return;
        const data = await readJsonBodyLimited(response, `${target.name} model list`);
        const rows =
          target.kind === "ollama" ? data.models || [] : data.data || [];
        return { target, rows };
      } catch {
        return;
      } finally {
        clearTimeout(timer);
      }
    }),
  );
  for (const result of results) {
    if (!result) continue;
    const { target, rows } = result;
    for (const row of rows) {
      const id = target.kind === "ollama" ? row.name : row.id;
      if (!id) continue;
      found.push({
        provider: target.name,
        model: id,
        endpoint: target.endpoint,
        host: target.base,
        local: true,
        size: row.size || null,
        modified: row.modified_at || null,
        details: row.details || null,
      });
    }
  }
  return { models: found, hosts: [...new Set(found.map((m) => m.provider))] };
}
function createWindow() {
  const win = new BrowserWindow({
    width: 1440,
    height: 920,
    minWidth: 800,
    minHeight: 580,
    autoHideMenuBar: true,
    backgroundColor: "#111210",
    title: "Server Rock: AI",
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      preload: path.join(__dirname, "preload.cjs"),
    },
  });
  win.setMenuBarVisibility(false);
  win.loadFile(path.join(__dirname, "..", "index.html"));
}
app.whenReady().then(() => {
  const modelRoot = path.join(app.getPath("userData"), "models"),
    workspace = createWorkspace(app.getPath("userData"));
  terminalManager = createTerminalManager({ resolveRoots: (folders) => workspace.getRoots(folders) });
  ipcMain.handle("projects:read-files", (_event, folders) =>
    workspace.readFiles(folders),
  );
  ipcMain.handle(
    "projects:write-file",
    (_event, folders, relativePath, content) =>
      workspace.writeFile(folders, relativePath, content),
  );
  ipcMain.handle("terminal:start", (event, options) =>
    terminalManager.start(options, (data) => event.sender.send("terminal:event", data)),
  );
  ipcMain.handle("terminal:write", (_event, id, input) => terminalManager.write(id, input));
  ipcMain.handle("terminal:stop", (_event, id, reason) => terminalManager.stop(id, reason));
  ipcMain.handle("terminal:list", () => terminalManager.list());
  ipcMain.handle("terminal:snapshot", (_event, id) => terminalManager.snapshot(id));
  ipcMain.handle("terminal:attach", (event, id) => {
    const unsubscribe = terminalManager.attach(id, (data) => event.sender.send("terminal:event", data));
    event.sender.once("destroyed", unsubscribe);
    return terminalManager.snapshot(id);
  });
  ipcMain.handle("chatgpt:status", () => chatGPTStatus());
  ipcMain.handle("chatgpt:signin", () => chatGPTSignIn());
  ipcMain.handle("chatgpt:models", () => chatGPTModels());
  ipcMain.handle("github-updates:check", (_event, channel) => checkGitHubRelease(channel));
  ipcMain.handle("github-updates:open-release", async (_event, tag) => {
    const url = releasePageUrl(tag);
    await shell.openExternal(url);
    return { opened: true, url };
  });
  ipcMain.handle("chatgpt:stream", (event, messages, model, requestId) =>
    chatGPTStream(event, messages, model, requestId),
  );
  ipcMain.on("chatgpt:cancel", (_event, requestId) =>
    activeChatGPTRequests.get(requestId)?.abort(),
  );
  ipcMain.handle("models:scan-hosts", () => scanModelHosts());
  ipcMain.handle("models:open-hub", async (_event, repoId) => {
    if (typeof repoId !== "string" || !/^[\w.-]+\/[\w.-]+$/.test(repoId) || repoId.length > 200)
      throw Error("Enter a valid Hugging Face repository ID.");
    const url = "https://huggingface.co/" + repoId;
    await shell.openExternal(url);
    return { opened: true, url };
  });
  const readSecureProviderSecret = async (provider) => {
    validateProviderName(provider);
    const secrets = await readProviderSecrets();
    return secrets[provider] || "";
  };
  const readGithubClientId = async () => {
    if (typeof process.env.SERVER_ROCK_GITHUB_APP_CLIENT_ID === "string" &&
        /^[A-Za-z0-9._-]{10,80}$/.test(process.env.SERVER_ROCK_GITHUB_APP_CLIENT_ID))
      return process.env.SERVER_ROCK_GITHUB_APP_CLIENT_ID;
    try {
      const config = JSON.parse(await readUtf8Limited(path.join(__dirname, "github-app.json"), 16 * 1024));
      return typeof config.clientId === "string" && /^[A-Za-z0-9._-]{10,80}$/.test(config.clientId)
        ? config.clientId
        : "";
    } catch {
      return "";
    }
  };
  const githubFetch = async (apiPath, options = {}) => {
    const token = await getGithubAccessToken();
    if (!token) throw Error("Connect your GitHub account in Settings first.");
    const response = await net.fetch("https://api.github.com" + apiPath, {
      ...options,
      headers: {
        Accept: "application/vnd.github+json",
        Authorization: "Bearer " + token,
        "X-GitHub-Api-Version": "2022-11-28",
        "Content-Type": "application/json",
        ...(options.headers || {}),
      },
      redirect: "manual",
      credentials: "omit",
      cache: "no-store",
    });
    if (response.status >= 300 && response.status < 400)
      throw Error("GitHub redirected the request; redirects are blocked.");
    if (!response.ok)
      throw Error("GitHub request failed (" + response.status + "): " + (await readTextBodyLimited(response, "GitHub")).slice(0, 300));
    return response;
  };
  const githubAuthHeaders = (token) => ({
    GIT_CONFIG_COUNT: "2",
    GIT_CONFIG_KEY_0: "core.hooksPath",
    GIT_CONFIG_VALUE_0: path.join(app.getPath("userData"), "git-hooks-disabled"),
    GIT_CONFIG_KEY_1: "http.extraheader",
    GIT_CONFIG_VALUE_1: "AUTHORIZATION: basic " + Buffer.from("x-access-token:" + token).toString("base64"),
    GCM_INTERACTIVE: "never",
  });
  const readGithubCredential = async () => {
    const value = await readSecureProviderSecret("GitHub");
    if (!value) return null;
    try {
      const credential = JSON.parse(value);
      if (credential && typeof credential.accessToken === "string") return credential;
    } catch {}
    return { accessToken: value, expiresAt: 0, refreshToken: "" };
  };
  const saveGithubCredential = (credential) =>
    writeProviderSecret("GitHub", JSON.stringify(credential));
  let githubRefreshPromise = null;
  const getGithubAccessToken = async () => {
    const credential = await readGithubCredential();
    if (!credential) throw Error("Connect your GitHub account in Settings first.");
    if (!credential.expiresAt || credential.expiresAt > Date.now() + 60_000)
      return credential.accessToken;
    if (!credential.refreshToken || !credential.clientId)
      throw Error("Your GitHub session expired. Connect GitHub again in Settings.");
    if (credential.refreshExpiresAt && credential.refreshExpiresAt <= Date.now())
      throw Error("Your GitHub refresh session expired. Connect GitHub again in Settings.");
    if (!githubRefreshPromise) {
      githubRefreshPromise = (async () => {
        const response = await net.fetch("https://github.com/login/oauth/access_token", {
          method: "POST",
          headers: { Accept: "application/json", "Content-Type": "application/x-www-form-urlencoded" },
          body: new URLSearchParams({
            client_id: credential.clientId,
            grant_type: "refresh_token",
            refresh_token: credential.refreshToken,
          }).toString(),
          redirect: "manual", credentials: "omit", cache: "no-store",
        });
        const body = await readJsonBodyLimited(response, "GitHub token refresh");
        if (!response.ok || !body.access_token)
          throw Error(body.error_description || body.error || "GitHub session refresh failed. Reconnect your account.");
        const updated = {
          clientId: credential.clientId,
          accessToken: body.access_token,
          refreshToken: body.refresh_token || credential.refreshToken,
          expiresAt: Number(body.expires_in) > 0 ? Date.now() + Number(body.expires_in) * 1000 : 0,
          refreshExpiresAt: Number(body.refresh_token_expires_in) > 0 ? Date.now() + Number(body.refresh_token_expires_in) * 1000 : 0,
        };
        await saveGithubCredential(updated);
        return updated.accessToken;
      })().finally(() => {
        githubRefreshPromise = null;
      });
    }
    return githubRefreshPromise;
  };
  const runGit = async (args, cwd, token, options = {}) => {
    const result = await execFileAsync("git", args, {
      cwd,
      windowsHide: true,
      timeout: 120000,
      maxBuffer: 2 * 1024 * 1024,
      env: {
        ...createSafeChildEnvironment(),
        GIT_CONFIG_COUNT: token ? "2" : "1",
        GIT_CONFIG_KEY_0: "core.hooksPath",
        GIT_CONFIG_VALUE_0: path.join(app.getPath("userData"), "git-hooks-disabled"),
        ...(token ? {
          GIT_CONFIG_KEY_1: "http.extraheader",
          GIT_CONFIG_VALUE_1: githubAuthHeaders(token).GIT_CONFIG_VALUE_1,
          GCM_INTERACTIVE: "never",
        } : {}),
      },
    });
    return {
      stdout: options.preserveStdout ? result.stdout : result.stdout.trim(),
      stderr: result.stderr.trim(),
    };
  };
  const githubRepoFullName = (value) => {
    if (typeof value !== "string" || value.length > 300)
      throw Error("Enter a repository as owner/name or a github.com repository URL.");
    let fullName = value.trim();
    const isUrl = /^https?:\/\//i.test(fullName);
    if (isUrl) {
      let url;
      try { url = new URL(fullName); } catch { throw Error("Enter a valid GitHub repository URL."); }
      if (url.hostname.toLowerCase() !== "github.com" || url.username || url.password || url.port)
        throw Error("Only github.com repository URLs are supported.");
      fullName = url.pathname.replace(/^\/+|\/+$/g, "");
    }
    if (isUrl) fullName = fullName.replace(/\.git$/i, "");
    if (!/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(fullName))
      throw Error("Enter a repository as owner/name or a github.com repository URL.");
    return fullName;
  };
  const githubRepoName = (repo) => {
    if (!repo || typeof repo !== "object" || typeof repo.full_name !== "string" ||
        githubRepoFullName(repo.full_name) !== repo.full_name)
      throw Error("Choose a valid GitHub repository.");
    if (typeof repo.name !== "string" || repo.name !== repo.full_name.split("/")[1] ||
        repo.name === "." || repo.name === ".." || path.basename(repo.name) !== repo.name)
      throw Error("The repository name is invalid.");
    return repo.full_name;
  };
  const githubRepoUrl = (repo) => "https://github.com/" + githubRepoName(repo) + ".git";
  const normalizeGithubRepository = (repo) => {
    if (!repo || typeof repo !== "object") throw Error("GitHub returned an invalid repository.");
    const fullName = githubRepoFullName(repo.full_name);
    const name = typeof repo.name === "string" && repo.name === fullName.split("/")[1] ? repo.name : "";
    if (!name) throw Error("GitHub returned an inconsistent repository name.");
    const defaultBranch = typeof repo.default_branch === "string" && /^[^\u0000-\u001f\u007f]{1,256}$/.test(repo.default_branch)
      ? repo.default_branch
      : "main";
    const description = typeof repo.description === "string" ? repo.description.slice(0, 1000) : "";
    const updatedAt = typeof repo.updated_at === "string" ? repo.updated_at.slice(0, 64) : "";
    return {
      id: Number.isSafeInteger(repo.id) ? repo.id : 0,
      fullName,
      name,
      private: repo.private === true,
      defaultBranch,
      description,
      updatedAt,
      cloneUrl: githubRepoUrl({ full_name: fullName, name }),
    };
  };
  const getGitRoot = async (folders) => {
    const { roots } = await workspace.readFiles(folders);
    if (!roots.length) throw Error("Select an authorized project folder first.");
    const root = roots[0];
    const result = await runGit(["rev-parse", "--show-toplevel"], root);
    const canonical = await fs.realpath(result.stdout);
    if (canonical !== root) throw Error("The selected folder must be the repository root.");
    return root;
  };
  const githubChangeSnapshot = async (root) => {
    const { stdout: porcelain } = await runGit(
      ["status", "--porcelain=v1", "--untracked-files=all", "-z"],
      root,
      undefined,
      { preserveStdout: true },
    );
    const changes = porcelain ? porcelain.split("\0").filter(Boolean) : [];
    const paths = new Set();
    for (const entry of changes) {
      if (entry.length >= 3 && entry[2] === " ") paths.add(entry.slice(3));
      else paths.add(entry);
    }
    const protectedPaths = [...paths].filter((relativePath) => isProtectedPath(relativePath.replaceAll("\\", "/")));
    let diff = "";
    if (!protectedPaths.length) {
      try {
        const result = await runGit(
          ["diff", "HEAD", "--no-ext-diff", "--no-color", "--unified=3", "--"],
          root,
          undefined,
          { preserveStdout: true },
        );
        const bytes = Buffer.from(result.stdout || "", "utf8");
        diff = bytes.length > MAX_GITHUB_DIFF_BYTES
          ? bytes.subarray(0, MAX_GITHUB_DIFF_BYTES).toString("utf8") + "\n[patch preview truncated]\n"
          : result.stdout || "";
      } catch {
        diff = "";
      }
    }
    const snapshot = crypto.createHash("sha256").update(porcelain || "");
    for (const relativePath of paths) {
      const absolutePath = path.resolve(root, relativePath);
      const relativeFromRoot = path.relative(root, absolutePath);
      if (!relativeFromRoot || relativeFromRoot === ".." || relativeFromRoot.startsWith(".." + path.sep) || path.isAbsolute(relativeFromRoot))
        throw Error("Git reported a changed path outside this repository.");
      snapshot.update("\0" + relativePath + "\0");
      try {
        const stat = await fs.lstat(absolutePath);
        snapshot.update(String(stat.mode & 0o7777) + "\0");
        if (stat.isSymbolicLink()) {
          snapshot.update("symlink\0" + await fs.readlink(absolutePath));
        } else if (stat.isFile()) {
          const noFollow = fsSync.constants.O_NOFOLLOW || 0;
          const handle = await fs.open(absolutePath, fsSync.constants.O_RDONLY | noFollow);
          try {
            for await (const chunk of handle.createReadStream({ autoClose: false })) snapshot.update(chunk);
          } finally {
            await handle.close();
          }
        } else {
          snapshot.update(stat.isDirectory() ? "directory" : "other");
        }
      } catch (error) {
        if (error.code !== "ENOENT") throw error;
        snapshot.update("deleted");
      }
    }
    return { changes, protectedPaths, diff, changeToken: snapshot.digest("hex") };
  };
  const streamProviderChat = createProviderBridge({
    net,
    getProviderSecret: readSecureProviderSecret,
  });
  ipcMain.handle("secrets:status", async (_event, provider) => ({
    saved: Boolean(await readSecureProviderSecret(provider)),
    secureStorage: chatGPTEncryptionReady(),
  }));
  ipcMain.handle("secrets:set", (_event, provider, secret) =>
    (validateProviderName(provider), writeProviderSecret(provider, secret)),
  );
  ipcMain.handle("github:default-client-id", () => readGithubClientId());
  ipcMain.handle("github:begin-signin", async (_event, clientId) => {
    if (typeof clientId !== "string" || !/^[A-Za-z0-9._-]{10,80}$/.test(clientId))
      throw Error("Add a valid GitHub App client ID in Settings.");
    const response = await net.fetch("https://github.com/login/device/code", {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({ client_id: clientId, scope: "offline_access" }).toString(),
      redirect: "manual",
      credentials: "omit",
      cache: "no-store",
    });
    const body = await readJsonBodyLimited(response, "GitHub device authorization");
    if (!response.ok || !body.device_code || !body.user_code)
      throw Error(body.error_description || body.error || "GitHub device authorization could not start.");
    const id = crypto.randomUUID();
    githubDeviceRequests.set(id, {
      clientId,
      deviceCode: body.device_code,
      interval: Math.max(5, Number(body.interval) || 5),
      expiresAt: Date.now() + Math.min(900, Number(body.expires_in) || 900) * 1000,
      nextPollAt: Date.now() + Math.max(5, Number(body.interval) || 5) * 1000,
    });
    await shell.openExternal("https://github.com/login/device");
    return { id, userCode: body.user_code, verificationUri: "https://github.com/login/device", interval: Math.max(5, Number(body.interval) || 5) };
  });
  ipcMain.handle("github:poll-signin", async (_event, id) => {
    const flow = githubDeviceRequests.get(id);
    if (!flow) throw Error("GitHub sign-in expired. Start again.");
    if (Date.now() > flow.expiresAt) {
      githubDeviceRequests.delete(id);
      throw Error("GitHub sign-in code expired. Start again.");
    }
    if (Date.now() < flow.nextPollAt) return { pending: true };
    const response = await net.fetch("https://github.com/login/oauth/access_token", {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({ client_id: flow.clientId, device_code: flow.deviceCode, grant_type: "urn:ietf:params:oauth:grant-type:device_code" }).toString(),
      redirect: "manual",
      credentials: "omit",
      cache: "no-store",
    });
    const body = await readJsonBodyLimited(response, "GitHub device token");
    if (body.error === "authorization_pending") {
      flow.nextPollAt = Date.now() + flow.interval * 1000;
      return { pending: true, interval: flow.interval };
    }
    if (body.error === "slow_down") {
      flow.interval += 5;
      flow.nextPollAt = Date.now() + flow.interval * 1000;
      return { pending: true, interval: flow.interval };
    }
    if (!response.ok || !body.access_token) {
      githubDeviceRequests.delete(id);
      throw Error(body.error_description || body.error || "GitHub authorization failed.");
    }
    await saveGithubCredential({
      clientId: flow.clientId,
      accessToken: body.access_token,
      refreshToken: body.refresh_token || "",
      expiresAt: Number(body.expires_in) > 0 ? Date.now() + Number(body.expires_in) * 1000 : 0,
      refreshExpiresAt: Number(body.refresh_token_expires_in) > 0 ? Date.now() + Number(body.refresh_token_expires_in) * 1000 : 0,
    });
    githubDeviceRequests.delete(id);
    const user = await readJsonBodyLimited(await githubFetch("/user"), "GitHub user profile");
    return { connected: true, login: user.login, name: user.name || user.login, avatarUrl: user.avatar_url || "" };
  });
  ipcMain.on("github:cancel-signin", (_event, id) => {
    if (typeof id === "string") githubDeviceRequests.delete(id);
  });
  ipcMain.handle("github:status", async () => {
    if (!(await readGithubCredential()))
      return { connected: false, secureStorage: chatGPTEncryptionReady() };
    const user = await readJsonBodyLimited(await githubFetch("/user"), "GitHub user profile");
    return { connected: true, secureStorage: chatGPTEncryptionReady(), login: user.login, name: user.name || user.login, avatarUrl: user.avatar_url || "" };
  });
  ipcMain.handle("github:disconnect", () => writeProviderSecret("GitHub", ""));
  ipcMain.handle("github:repos", async () => {
    const repos = [];
    for (let page = 1; page <= 10; page++) {
      const response = await githubFetch(
        "/user/repos?sort=updated&per_page=100&page=" + page +
        "&affiliation=owner,collaborator,organization_member",
      );
      const rows = await readJsonBodyLimited(response, "GitHub repository list");
      if (!Array.isArray(rows)) throw Error("GitHub returned an invalid repository list.");
      repos.push(...rows.slice(0, 100));
      if (rows.length < 100) break;
    }
    return repos.map(normalizeGithubRepository);
  });
  ipcMain.handle("github:resolve-repo", async (_event, value) => {
    const fullName = githubRepoFullName(value);
    const [owner, name] = fullName.split("/");
    const repo = await readJsonBodyLimited(await githubFetch("/repos/" + encodeURIComponent(owner) + "/" + encodeURIComponent(name)), "GitHub repository");
    return normalizeGithubRepository(repo);
  });
  ipcMain.handle("github:clone", async (_event, repo) => {
    const fullName = githubRepoName(repo);
    const parent = await dialog.showOpenDialog(BrowserWindow.getFocusedWindow(), {
      title: "Choose where to clone " + fullName,
      properties: ["openDirectory", "createDirectory"],
    });
    if (parent.canceled || !parent.filePaths[0]) return null;
    const destination = path.join(parent.filePaths[0], repo.name);
    if (await fs.stat(destination).then(() => true).catch(() => false))
      throw Error("A folder named " + repo.name + " already exists there.");
    const token = await getGithubAccessToken();
    if (!token) throw Error("Connect GitHub before cloning a repository.");
    await runGit(["clone", "--", githubRepoUrl(repo), destination], parent.filePaths[0], token);
    const folder = await workspace.approveFolder(destination);
    return { folder, repo: { fullName, name: repo.name, defaultBranch: repo.defaultBranch } };
  });
  ipcMain.handle("github:status-project", async (_event, folders) => {
    const root = await getGitRoot(folders);
    const [{ stdout: branch }, snapshot, { stdout: ahead }, { stdout: remoteUrl }] = await Promise.all([
      runGit(["branch", "--show-current"], root),
      githubChangeSnapshot(root),
      runGit(["rev-list", "--count", "@{upstream}..HEAD"], root).catch(() => ({ stdout: "0" })),
      runGit(["remote", "get-url", "origin"], root).catch(() => ({ stdout: "" })),
    ]);
    return {
      branch,
      changedFiles: snapshot.changes.length,
      changes: snapshot.changes,
      protectedPaths: snapshot.protectedPaths,
      changeToken: snapshot.changeToken,
      ahead: Number(ahead) || 0,
      remoteUrl,
    };
  });
  ipcMain.handle("github:push-project", async (_event, folders, message, expectedFullName, expectedChangeToken) => {
    const root = await getGitRoot(folders);
    const token = await getGithubAccessToken();
    if (!token) throw Error("Connect GitHub before pushing changes.");
    const { stdout: remote } = await runGit(["remote", "get-url", "origin"], root).catch(() => ({ stdout: "" }));
    if (!remote || !/^https:\/\/github\.com\/[\w.-]+\/[\w.-]+(?:\.git)?$/i.test(remote))
      throw Error("This project does not have a GitHub origin. Choose or clone a GitHub repository first.");
    const remoteName = remote.replace(/^https:\/\/github\.com\//i, "").replace(/\.git$/i, "").replace(/\/$/, "");
    if (typeof expectedFullName === "string" && expectedFullName && remoteName.toLowerCase() !== expectedFullName.toLowerCase())
      throw Error("The GitHub origin is " + remoteName + ", but this project is attached to " + expectedFullName + ".");
    if (typeof expectedChangeToken !== "string" || !/^[a-f0-9]{64}$/.test(expectedChangeToken))
      throw Error("Review the current repository changes in the app before pushing.");
    const { stdout: branch } = await runGit(["branch", "--show-current"], root);
    if (!branch) throw Error("This repository is in a detached HEAD state. Check out a named branch before pushing.");
    const snapshot = await githubChangeSnapshot(root);
    if (snapshot.changeToken !== expectedChangeToken)
      throw Error("Repository files changed after your review. Close this dialog and review the updated changes before pushing.");
    if (snapshot.protectedPaths.length)
      throw Error("Remove protected files before pushing: " + snapshot.protectedPaths.slice(0, 5).join(", ") + (snapshot.protectedPaths.length > 5 ? " …" : ""));
    let commit = "";
    if (snapshot.changes.length) {
      await runGit(["add", "--all"], root);
      const commitMessage = typeof message === "string" && message.trim()
        ? message.trim().slice(0, 160)
        : "Update from Server Rock: AI";
      await runGit(["-c", "user.name=Server Rock AI", "-c", "user.email=server-rock-ai@users.noreply.github.com", "commit", "-m", commitMessage], root);
      commit = (await runGit(["rev-parse", "--short", "HEAD"], root)).stdout;
    }
    await runGit(["push", "origin", branch], root, token);
    return { pushed: true, branch, commit, changedFiles: snapshot.changes.length };
  });
  ipcMain.handle("hf:search", async (_event, query, task) => {
    if (typeof query !== "string" || query.length > 200)
      throw Error("Hugging Face search text must be under 200 characters.");
    if (typeof task !== "string" || !/^[a-z0-9-]{1,64}$/.test(task))
      throw Error("Choose a valid Hugging Face task.");
    const url = new URL("https://huggingface.co/api/models");
    url.searchParams.set("search", query.trim());
    if (task !== "all") url.searchParams.set("pipeline_tag", task);
    url.searchParams.set("sort", "downloads");
    url.searchParams.set("limit", "30");
    url.searchParams.set("full", "true");
    url.searchParams.append("expand[]", "siblings");
    const token = await readSecureProviderSecret("Hugging Face");
    const response = await net.fetch(url.href, {
      headers: token ? { Authorization: "Bearer " + token } : {},
      redirect: "manual",
      credentials: "omit",
      cache: "no-store",
    });
    if (!response.ok) throw Error("Hub search failed: " + response.status);
    const rows = await readJsonBodyLimited(response, "Hugging Face model catalog");
    if (!Array.isArray(rows)) throw Error("Hugging Face returned an invalid model catalog.");
    return rows.slice(0, 30).map((model) => ({
      id: typeof model?.id === "string" && /^[\w.-]+\/[\w.-]+$/.test(model.id) && model.id.length <= 200 ? model.id : "",
      cardData: model?.cardData && typeof model.cardData === "object" ? model.cardData : {},
      pipeline_tag: typeof model?.pipeline_tag === "string" && model.pipeline_tag.length <= 64 ? model.pipeline_tag : "",
      siblings: Array.isArray(model?.siblings)
        ? model.siblings.slice(0, 300).filter((file) => typeof file?.rfilename === "string" && file.rfilename.length <= 512).map((file) => ({ rfilename: file.rfilename }))
        : [],
    })).filter((model) => model.id);
  });
  ipcMain.handle("hf:inference", async (_event, task, model, prompt, mediaData) => {
    const supportedTasks = [
      "text-to-image", "image-to-image", "image-to-text", "image-classification",
      "zero-shot-image-classification", "image-segmentation", "object-detection",
      "depth-estimation", "background-removal", "image-feature-extraction",
      "document-question-answering", "visual-question-answering", "automatic-speech-recognition",
      "audio-classification", "zero-shot-audio-classification", "text2text-generation",
      "summarization", "translation", "text-classification", "token-classification",
      "zero-shot-classification", "question-answering", "fill-mask", "feature-extraction",
    ];
    if (!supportedTasks.includes(task)) throw Error("Unsupported hosted Hugging Face task.");
    if (typeof model !== "string" || !/^[\w.-]+\/[\w.-]+$/.test(model) || model.length > 200)
      throw Error("Enter a valid Hugging Face repository ID.");
    if (typeof prompt !== "string" || prompt.length > 100000)
      throw Error("Prompt is too long for hosted inference.");
    const token = await readSecureProviderSecret("Hugging Face");
    if (!token) throw Error("Save a Hugging Face token with Inference Providers permission in Settings.");
    const isImageOutput = ["text-to-image", "image-to-image"].includes(task);
    const needsMedia = [
      "image-to-image", "image-to-text", "image-classification", "zero-shot-image-classification",
      "image-segmentation", "object-detection", "depth-estimation", "background-removal",
      "image-feature-extraction", "document-question-answering", "visual-question-answering",
      "automatic-speech-recognition", "audio-classification", "zero-shot-audio-classification",
    ].includes(task);
    if (needsMedia && !mediaData) throw Error("Attach a source image or audio file for this task.");
    let inputs = prompt, parameters = {};
    if (mediaData) {
      const match = /^data:([^;,]+)(?:;[^,]*)?;base64,([A-Za-z0-9+/=]+)$/.exec(mediaData);
      if (!match || match[2].length > 34_000_000) throw Error("The attached media is invalid or exceeds 25 MB.");
      const mime = match[1];
      if (mime.startsWith("image/")) {
        inputs = match[2];
        if (["visual-question-answering", "document-question-answering"].includes(task))
          parameters.question = prompt;
        if (task === "image-to-image") parameters.prompt = prompt;
      } else if (mime.startsWith("audio/")) {
        const response = await net.fetch(
          "https://router.huggingface.co/hf-inference/models/" + encodeURIComponent(model),
          {
            method: "POST",
            headers: { Authorization: "Bearer " + token, "Content-Type": mime, Accept: "application/json" },
            body: Buffer.from(match[2], "base64"),
            redirect: "manual", credentials: "omit", cache: "no-store",
          },
        );
        if (!response.ok) throw Error("Hugging Face inference failed: " + response.status + " " + (await readTextBodyLimited(response, "Hugging Face")).slice(0, 300));
        const contentType = response.headers.get("content-type") || "application/json";
        if (contentType.startsWith("audio/")) {
          const data = await readBinaryBodyLimited(response, 50 * 1024 * 1024, "Generated media");
          return { kind: "audio", mime: contentType, data: data.toString("base64") };
        }
        return { kind: "json", result: JSON.stringify(await readJsonBodyLimited(response, "Hugging Face inference"), null, 2).slice(0, 16000) };
      } else throw Error("This hosted task accepts image or audio attachments only.");
    } else {
      try {
        const structured = JSON.parse(prompt);
        if (structured && typeof structured === "object") inputs = structured;
      } catch {}
    }
    const response = await net.fetch(
      "https://router.huggingface.co/hf-inference/models/" + encodeURIComponent(model),
      {
        method: "POST",
        headers: {
          Authorization: "Bearer " + token,
          "Content-Type": "application/json",
          Accept: isImageOutput ? "image/*" : "application/json, image/*, audio/*",
        },
        body: JSON.stringify({ inputs, parameters }),
        redirect: "manual", credentials: "omit", cache: "no-store",
      },
    );
    if (!response.ok) throw Error("Hugging Face inference failed: " + response.status + " " + (await readTextBodyLimited(response, "Hugging Face")).slice(0, 300));
    const mime = response.headers.get("content-type") || "application/json";
    if (mime.startsWith("image/") || mime.startsWith("audio/")) {
      const data = await readBinaryBodyLimited(response, 50 * 1024 * 1024, "Generated media");
      return { kind: mime.startsWith("image/") ? "image" : "audio", mime, data: data.toString("base64") };
    }
    return { kind: "json", result: JSON.stringify(await readJsonBodyLimited(response, "Hugging Face inference"), null, 2).slice(0, 16000) };
  });
  ipcMain.handle("provider:stream", async (event, request) => {
    if (!request || typeof request !== "object") throw Error("Invalid provider request.");
    validateRequestId(request.requestId, "provider request ID");
    const controller = new AbortController();
    activeProviderRequests.set(request.requestId, controller);
    try {
      const protocol = request.provider === "Anthropic API" ? "anthropic" : "openai";
      const result = await streamProviderChat({
        provider: request.provider,
        protocol,
        endpoint: request.endpoint,
        model: request.model,
        messages: request.messages,
        temperature: request.temperature,
        maxTokens: request.maxTokens,
        signal: controller.signal,
        onChunk: (text) => event.sender.send("provider:chunk", { requestId: request.requestId, text }),
      });
      return result;
    } finally {
      activeProviderRequests.delete(request.requestId);
    }
  });
  ipcMain.on("provider:cancel", (_event, requestId) =>
    activeProviderRequests.get(requestId)?.abort(),
  );
  ipcMain.handle(
    "models:scan-provider",
    async (_event, provider, endpoint) => {
      validateProviderName(provider);
      const url = validateEndpoint(endpoint);
      url.pathname = url.pathname.replace(
        /\/(chat\/completions|messages)\/?$/,
        "/models",
      );
      if (!url.pathname.endsWith("/models"))
        url.pathname = url.pathname.replace(/\/$/, "") + "/models";
      url.search = "";
      const headers = { Accept: "application/json" };
      const apiKey = await readSecureProviderSecret(provider);
      if (apiKey) {
        if (provider === "Anthropic API") {
          headers["x-api-key"] = apiKey;
          headers["anthropic-version"] = "2023-06-01";
        } else headers.Authorization = "Bearer " + apiKey;
      }
      const controller = new AbortController(),
        timer = setTimeout(() => controller.abort(), 5000);
      try {
        const response = await net.fetch(url.href, {
          headers,
          signal: controller.signal,
        });
        if (!response.ok)
          throw Error("Provider model-list request failed: " + response.status);
        const body = await readJsonBodyLimited(response, "Provider model list");
        const rows = body.data || body.models || [];
        if (!Array.isArray(rows)) throw Error("Provider returned an invalid model list.");
        return {
          models: rows.slice(0, 500)
            .map((row) => ({
              id: typeof row?.id === "string" ? row.id : typeof row?.name === "string" ? row.name : "",
              owned_by: typeof row?.owned_by === "string" ? row.owned_by.slice(0, 128) : typeof row?.publisher === "string" ? row.publisher.slice(0, 128) : "",
              size: row.size || row.parameter_size || null,
            }))
            .filter((row) => row.id && row.id.length <= 512 && !/[\u0000-\u001f\u007f]/.test(row.id)),
          endpoint:
            provider === "Anthropic API"
              ? endpoint.replace(/\/messages\/?$/, "/messages")
              : endpoint,
        };
      } finally {
        clearTimeout(timer);
      }
    },
  );
  ipcMain.handle("models:download", async (_event, repoId) => {
    if (typeof repoId !== "string" || !/^[\w.-]+\/[\w.-]+$/.test(repoId) || repoId.length > 200)
      throw Error("Enter a valid Hugging Face repository ID.");
    const base = modelDirectory(repoId);
    await fs.mkdir(base, { recursive: true });
    const token = await readSecureProviderSecret("Hugging Face");
    const headers = token ? { Authorization: "Bearer " + token } : {};
    const info = await net.fetch(
      "https://huggingface.co/api/models/" + repoId + "?expand[]=siblings",
      { headers },
    );
    if (!info.ok) throw Error("Hub metadata request failed: " + info.status);
    const meta = await readJsonBodyLimited(info, "Hugging Face model metadata"),
      files = (meta.siblings || [])
        .map((f) => f.rfilename)
        .filter((f) => f && !f.split("/").some((p) => p === ".."));
    if (!files.some((f) => f.startsWith("onnx/")))
      throw Error("This repository has no ONNX weights for Transformers.js.");
    const wanted = files.filter((f) =>
      /^(onnx\/|config\.json$|tokenizer|processor|preprocessor|generation_config|special_tokens|vocab|merges|added_tokens)/i.test(
        f,
      ),
    );
    if (wanted.length > 300)
      throw Error(
        "This model has too many files for the built-in downloader. Use another local runtime.",
      );
    let done = 0,
      total = 0;
    for (const f of wanted) {
      const dest = resolveModelFilePath(base, f);
      await fs.mkdir(path.dirname(dest), { recursive: true });
      await ensureNoSymlinkComponents(base, path.dirname(dest), fs);
      const res = await net.fetch(
        "https://huggingface.co/" + repoId + "/resolve/main/" + f,
        { headers },
      );
      if (!res.ok) throw Error("Download failed for " + f + ": " + res.status);
      const expected = Number(res.headers.get("content-length") || 0);
      if (expected > MAX_MODEL_BYTES || total + expected > MAX_MODEL_BYTES)
        throw Error(
          "Model download exceeds the 10 GB app limit. Use another local runtime.",
        );
      const temporaryPath = `${dest}.${crypto.randomUUID()}.download`;
      let output;
      let reader;
      try {
        output = await fs.open(temporaryPath, "wx");
        reader = res.body?.getReader();
        if (!reader) throw Error("Empty download response for " + f);
        let size = 0;
        while (true) {
          const { done: finished, value } = await reader.read();
          if (finished) break;
          size += value.byteLength;
          total += value.byteLength;
          if (size > MAX_MODEL_BYTES || total > MAX_MODEL_BYTES)
            throw Error("Model download exceeds the 10 GB app limit.");
          await output.write(value);
        }
        await reader.cancel().catch(() => {});
        await output.close();
        output = null;
        await fs.rename(temporaryPath, dest);
        done++;
      } catch (error) {
        if (reader) await reader.cancel().catch(() => {});
        if (output) await output.close().catch(() => {});
        await fs.rm(temporaryPath, { force: true });
        throw error;
      }
    }
    return { repoId, downloaded: done, path: base };
  });
  ipcMain.on("models:cancel", (_event, requestId) => {
    const flag = activeGenerations.get(requestId);
    if (flag) flag.cancelled = true;
  });
  ipcMain.handle(
    "models:generate",
    async (event, repoId, task, prompt, requestId, mediaData) => {
      const supported = [
        "text-generation",
        "text2text-generation",
        "summarization",
        "translation",
        "text-classification",
        "token-classification",
        "zero-shot-classification",
        "question-answering",
        "fill-mask",
        "feature-extraction",
        "image-to-text",
        "image-to-image",
        "image-classification",
        "zero-shot-image-classification",
        "image-segmentation",
        "object-detection",
        "depth-estimation",
        "background-removal",
        "image-feature-extraction",
        "document-question-answering",
        "visual-question-answering",
        "automatic-speech-recognition",
        "audio-classification",
        "zero-shot-audio-classification",
        "text-to-speech",
      ];
      const imageTasks = [
        "image-to-text",
        "image-to-image",
        "image-classification",
        "zero-shot-image-classification",
        "image-segmentation",
        "object-detection",
        "depth-estimation",
        "background-removal",
        "image-feature-extraction",
        "document-question-answering",
        "visual-question-answering",
      ];
      const audioTasks = [
        "automatic-speech-recognition",
        "audio-classification",
        "zero-shot-audio-classification",
      ];
      if (!supported.includes(task))
        throw Error(
          "This local Transformers.js task is not supported: " + task,
        );
      if (typeof prompt !== "string" || prompt.length > 100000)
        throw Error("Prompt is empty or too long.");
      validateRequestId(requestId, "generation ID");
      if (
        (imageTasks.includes(task) || audioTasks.includes(task)) &&
        !mediaData
      )
        throw Error("Attach an image or audio file for this task.");
      const flag = { cancelled: false };
      activeGenerations.set(requestId, flag);
      try {
        if (flag.cancelled) return { cancelled: true };
        const dir = modelDirectory(repoId);
        if (
          !(await fs.stat(dir).then(
            () => true,
            () => false,
          ))
        )
          throw Error("Download this ONNX model before using it offline.");
        const runtimeTask = task === "text-to-speech" ? "text-to-audio" : task,
          key = runtimeTask + ":" + dir;
        if (!localPipelines.has(key)) {
          const promise = (async () => {
            const { pipeline, env } = await getTransformers();
            env.allowRemoteModels = false;
            env.allowLocalModels = true;
            env.useFSCache = true;
            env.useBrowserCache = false;
            return pipeline(runtimeTask, dir, { device: "cpu" });
          })();
          localPipelines.set(key, promise);
          promise.catch(() => localPipelines.delete(key));
        }
        const pipe = await localPipelines.get(key);
        if (flag.cancelled) return { cancelled: true };
        let parsedPrompt = prompt;
        try {
          parsedPrompt = JSON.parse(prompt);
        } catch {}
        if (task === "text-generation") {
          const { TextStreamer, StoppingCriteria } = await getTransformers();
          class UserStopCriteria extends StoppingCriteria {
            _call(input_ids) {
              return input_ids.map(() => flag.cancelled);
            }
          }
          const streamer = new TextStreamer(pipe.tokenizer, {
            skip_prompt: true,
            callback_function: (text) => {
              if (!flag.cancelled)
                event.sender.send("models:chunk", { requestId, text });
            },
          });
          await pipe(prompt, {
            max_new_tokens: 512,
            streamer,
            stopping_criteria: new UserStopCriteria(),
          });
          return { cancelled: flag.cancelled };
        }
        if (imageTasks.includes(task) || audioTasks.includes(task)) {
          const match =
            typeof mediaData === "string" &&
            mediaData.match(
              /^data:([^;,]+)(?:;[^,]*)?;base64,([A-Za-z0-9+/=]+)$/,
            );
          if (!match || mediaData.length > 28 * 1024 * 1024)
            throw Error("Attach an image or audio file under 20 MB.");
          const mime = match[1].toLowerCase(),
            isImage = mime.startsWith("image/"),
            isAudio = mime.startsWith("audio/");
          if (imageTasks.includes(task) && !isImage)
            throw Error("This task needs an image attachment.");
          if (audioTasks.includes(task) && !isAudio)
            throw Error("This task needs an audio attachment.");
          const extensions = {
            "image/png": "png",
            "image/jpeg": "jpg",
            "image/webp": "webp",
            "audio/wav": "wav",
            "audio/x-wav": "wav",
            "audio/mpeg": "mp3",
            "audio/mp3": "mp3",
            "audio/ogg": "ogg",
            "audio/webm": "webm",
            "audio/flac": "flac",
            "audio/mp4": "m4a",
          };
          const extension = extensions[mime];
          if (!extension) throw Error("Unsupported attachment format: " + mime);
          const mediaPath = path.join(
            app.getPath("temp"),
            "server-rock-" + crypto.randomUUID() + "." + extension,
          );
          await fs.writeFile(mediaPath, Buffer.from(match[2], "base64"));
          try {
            const options = {};
            if (
              task === "visual-question-answering" ||
              task === "document-question-answering"
            ) {
              options.question =
                typeof parsedPrompt === "object"
                  ? parsedPrompt.question || ""
                  : prompt;
              if (!options.question)
                throw Error("Enter a question for the attached image.");
            }
            if (
              task === "zero-shot-image-classification" ||
              task === "zero-shot-audio-classification"
            ) {
              const labels = parsedPrompt?.candidate_labels;
              if (Array.isArray(labels) && labels.length)
                options.candidate_labels = labels;
              else
                throw Error(
                  'Enter JSON with candidate_labels, for example {"candidate_labels":["speech","music"]}.',
                );
            }
            const result = await pipe(mediaPath, options);
            if (task === "image-to-text")
              return {
                result: Array.isArray(result)
                  ? result.map((x) => x.generated_text || "").join("\n")
                  : String(result?.generated_text || JSON.stringify(result)),
              };
            if (task === "automatic-speech-recognition")
              return { result: result?.text || JSON.stringify(result) };
            let rawImages = [];
            const collect = (value) => {
              if (!value) return;
              if (value.data && value.width && value.height && value.channels) {
                rawImages.push(value);
                return;
              }
              if (Array.isArray(value)) {
                for (const item of value) collect(item);
                return;
              }
              if (typeof value === "object") {
                for (const item of Object.values(value)) collect(item);
              }
            };
            collect(result);
            if (rawImages.length) {
              const sharp = require("sharp"),
                first = rawImages[0],
                png = await sharp(Buffer.from(first.data), {
                  raw: {
                    width: first.width,
                    height: first.height,
                    channels: first.channels,
                  },
                })
                  .png()
                  .toBuffer();
              const summary = Array.isArray(result)
                ? result
                    .map((item) => ({ label: item.label, score: item.score }))
                    .filter((item) => item.label)
                : task === "depth-estimation"
                  ? "Depth map generated"
                  : task === "background-removal"
                    ? "Background removed"
                    : "Image output generated";
              return {
                result:
                  typeof summary === "string"
                    ? summary
                    : JSON.stringify(summary),
                imageData: "data:image/png;base64," + png.toString("base64"),
              };
            }
            return {
              result: JSON.stringify(
                result,
                (key, value) =>
                  ArrayBuffer.isView(value)
                    ? "[" +
                      value.constructor.name +
                      " " +
                      value.length +
                      " values]"
                    : value,
                null,
                2,
              ).slice(0, 16000),
            };
          } finally {
            await fs.rm(mediaPath, { force: true });
          }
        }
        if (task === "text-to-speech") {
          const audio = resultAudio(
            await pipe(prompt, { max_new_tokens: 512 }),
          );
          return { audioData: audio };
        }
        let input =
            typeof parsedPrompt === "object" && parsedPrompt !== null
              ? parsedPrompt
              : prompt,
          options = {};
        if (task === "zero-shot-classification" && typeof input === "object") {
          options.candidate_labels = input.candidate_labels || [];
          input = input.text || input.sequence || "";
          if (!options.candidate_labels.length)
            throw Error(
              "For zero-shot classification, provide JSON with text and candidate_labels.",
            );
        }
        const result = await pipe(input, options);
        return {
          result: JSON.stringify(
            result,
            (key, value) =>
              ArrayBuffer.isView(value)
                ? "[" + value.constructor.name + " " + value.length + " values]"
                : value,
            null,
            2,
          ).slice(0, 16000),
        };
      } finally {
        activeGenerations.delete(requestId);
      }
    },
  );
  function resultAudio(result) {
    const waveform = result?.audio || result;
    if (!waveform || !waveform.length)
      throw Error("The selected model returned no audio samples.");
    const samples = Float32Array.from(waveform),
      data = Buffer.alloc(samples.length * 2);
    for (let i = 0; i < samples.length; i++) {
      const value = Math.max(-1, Math.min(1, samples[i]));
      data.writeInt16LE(value < 0 ? value * 32768 : value * 32767, i * 2);
    }
    const header = Buffer.alloc(44),
      sampleRate = Number(
        result?.sampling_rate || result?.samplingRate || 16000,
      );
    header.write("RIFF", 0);
    header.writeUInt32LE(36 + data.length, 4);
    header.write("WAVE", 8);
    header.write("fmt ", 12);
    header.writeUInt32LE(16, 16);
    header.writeUInt16LE(1, 20);
    header.writeUInt16LE(1, 22);
    header.writeUInt32LE(sampleRate, 24);
    header.writeUInt32LE(sampleRate * 2, 28);
    header.writeUInt16LE(2, 32);
    header.writeUInt16LE(16, 34);
    header.write("data", 36);
    header.writeUInt32LE(data.length, 40);
    return (
      "data:audio/wav;base64," +
      Buffer.concat([header, data]).toString("base64")
    );
  }
  ipcMain.handle("projects:pick-folder", async () => {
    const result = await dialog.showOpenDialog(
      BrowserWindow.getFocusedWindow(),
      {
        title: "Add a project source folder",
        properties: ["openDirectory", "createDirectory"],
      },
    );
    if (result.canceled || !result.filePaths[0]) return null;
    return workspace.approveFolder(result.filePaths[0]);
  });
  createWindow();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    terminalManager?.dispose();
    app.quit();
  }
});
app.on("before-quit", () => terminalManager?.dispose());
