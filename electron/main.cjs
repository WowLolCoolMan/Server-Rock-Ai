const {
  app,
  BrowserWindow,
  Menu,
  protocol,
  net,
  ipcMain,
  dialog,
  shell,
  safeStorage,
} = require("electron");
const path = require("node:path");
const fs = require("node:fs/promises");
const http = require("node:http");
const crypto = require("node:crypto");
const { createWorkspace } = require("./workspace.cjs");
const MAX_MODEL_BYTES = 10 * 1024 * 1024 * 1024;
const localPipelines = new Map();
const activeGenerations = new Map();
const activeChatGPTRequests = new Map();
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
async function readChatGPTState() {
  try {
    if (!chatGPTEncryptionReady())
      return { profiles: [], hostId: "urn:uuid:" + crypto.randomUUID() };
    const encoded = await fs.readFile(chatGPTStorePath(), "utf8");
    return JSON.parse(
      safeStorage.decryptString(Buffer.from(encoded, "base64")),
    );
  } catch {
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
    dest = chatGPTStorePath(),
    temp = dest + ".tmp";
  await fs.mkdir(path.dirname(dest), { recursive: true });
  await fs.writeFile(temp, output, { mode: 0o600 });
  await fs.rename(temp, dest);
}
async function readProviderSecrets() {
  if (!chatGPTEncryptionReady()) return {};
  try {
    const encoded = await fs.readFile(providerSecretsPath(), "utf8");
    return JSON.parse(
      safeStorage.decryptString(Buffer.from(encoded, "base64")),
    );
  } catch {
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
    temp = dest + ".tmp",
    encoded = safeStorage
      .encryptString(JSON.stringify(secrets))
      .toString("base64");
  await fs.mkdir(path.dirname(dest), { recursive: true });
  await fs.writeFile(temp, encoded, { mode: 0o600 });
  await fs.rename(temp, dest);
  return { saved: Boolean(secret.trim()) };
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
    const tokens = await tokenResponse.json();
    if (!tokens.id_token || !tokens.access_token)
      throw Error("ChatGPT returned an incomplete sign-in response.");
    const discoveryResponse = await net.fetch(
      "https://auth.openai.com/.well-known/openid-configuration",
    );
    if (!discoveryResponse.ok)
      throw Error("Could not verify ChatGPT identity metadata.");
    const metadata = await discoveryResponse.json();
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
  const tokens = await response.json();
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
  const result = await response.json();
  return (result.data || []).map((x) => x.id).filter(Boolean);
}
async function chatGPTStream(event, messages, model, requestId) {
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
          (await response.text()).slice(0, 300),
      );
    const reader = response.body.getReader(),
      decoder = new TextDecoder();
    let carry = "";
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      carry += decoder.decode(value, { stream: true });
      const lines = carry.split("\n");
      carry = lines.pop();
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
        )
          event.sender.send("chatgpt:chunk", { requestId, text: item.delta });
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
        const data = await response.json();
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
    backgroundColor: "#111210",
    title: "Server Rock: AI",
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      preload: path.join(__dirname, "preload.cjs"),
    },
  });
  win.loadFile(path.join(__dirname, "..", "index.html"));
}
app.whenReady().then(() => {
  const modelRoot = path.join(app.getPath("userData"), "models"),
    workspace = createWorkspace(app.getPath("userData"));
  ipcMain.handle("projects:read-files", (_event, folders) =>
    workspace.readFiles(folders),
  );
  ipcMain.handle(
    "projects:write-file",
    (_event, folders, relativePath, content) =>
      workspace.writeFile(folders, relativePath, content),
  );
  ipcMain.handle("chatgpt:status", () => chatGPTStatus());
  ipcMain.handle("chatgpt:signin", () => chatGPTSignIn());
  ipcMain.handle("chatgpt:models", () => chatGPTModels());
  ipcMain.handle("chatgpt:stream", (event, messages, model, requestId) =>
    chatGPTStream(event, messages, model, requestId),
  );
  ipcMain.on("chatgpt:cancel", (_event, requestId) =>
    activeChatGPTRequests.get(requestId)?.abort(),
  );
  ipcMain.handle("models:scan-hosts", () => scanModelHosts());
  ipcMain.handle("secrets:get", async (_event, provider) => {
    if (!chatGPTEncryptionReady())
      throw Error("Secure OS credential storage is unavailable.");
    const secrets = await readProviderSecrets();
    return secrets[provider] || "";
  });
  ipcMain.handle("secrets:set", (_event, provider, secret) =>
    writeProviderSecret(provider, secret),
  );
  ipcMain.handle(
    "models:scan-provider",
    async (_event, provider, endpoint, apiKey) => {
      if (typeof endpoint !== "string" || endpoint.length > 2048)
        throw Error("Enter a valid provider endpoint.");
      let url;
      try {
        url = new URL(endpoint);
      } catch {
        throw Error("Enter a valid provider endpoint.");
      }
      if (!["http:", "https:"].includes(url.protocol))
        throw Error("Provider endpoint must use HTTP or HTTPS.");
      url.pathname = url.pathname.replace(
        /\/(chat\/completions|messages)\/?$/,
        "/models",
      );
      if (!url.pathname.endsWith("/models"))
        url.pathname = url.pathname.replace(/\/$/, "") + "/models";
      url.search = "";
      const headers = { Accept: "application/json" };
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
        const body = await response.json();
        return {
          models: (body.data || body.models || [])
            .map((row) => ({
              id: row.id || row.name,
              owned_by: row.owned_by || row.publisher,
              size: row.size || row.parameter_size || null,
            }))
            .filter((row) => row.id),
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
  ipcMain.handle("models:download", async (_event, repoId, token) => {
    const base = modelDirectory(repoId);
    await fs.mkdir(base, { recursive: true });
    const headers = token ? { Authorization: "Bearer " + token } : {};
    const info = await net.fetch(
      "https://huggingface.co/api/models/" + repoId + "?expand[]=siblings",
      { headers },
    );
    if (!info.ok) throw Error("Hub metadata request failed: " + info.status);
    const meta = await info.json(),
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
      const dest = path.resolve(base, f);
      if (!dest.startsWith(path.resolve(base) + path.sep))
        throw Error("Invalid file path in model repository.");
      await fs.mkdir(path.dirname(dest), { recursive: true });
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
      const output = await fs.open(dest, "w");
      try {
        const reader = res.body?.getReader();
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
        done++;
      } catch (error) {
        await output.close();
        await fs.rm(dest, { force: true });
        throw error;
      }
      await output.close();
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
      if (typeof requestId !== "string" || requestId.length > 100)
        throw Error("Invalid generation ID.");
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
  Menu.setApplicationMenu(
    Menu.buildFromTemplate([
      {
        label: "File",
        submenu: [
          {
            label: "New chat",
            accelerator: "CmdOrCtrl+N",
            click: () => {
              const w = BrowserWindow.getFocusedWindow();
              w?.webContents.executeJavaScript(
                "document.querySelector('#newChat').click()",
              );
            },
          },
          { type: "separator" },
          { role: "quit" },
        ],
      },
      {
        label: "Edit",
        submenu: [
          { role: "undo" },
          { role: "redo" },
          { type: "separator" },
          { role: "cut" },
          { role: "copy" },
          { role: "paste" },
          { role: "selectAll" },
        ],
      },
      {
        label: "View",
        submenu: [
          { role: "reload" },
          { role: "toggleDevTools" },
          { type: "separator" },
          { role: "togglefullscreen" },
        ],
      },
      {
        label: "Help",
        submenu: [
          {
            label: "About Server Rock: AI",
            click: () =>
              BrowserWindow.getFocusedWindow()?.webContents.executeJavaScript(
                "alert('Server Rock: AI · Local-first AI workspace')",
              ),
          },
        ],
      },
    ]),
  );
  createWindow();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit();
});
