// IPC: "ChatGPT plan" provider: sign in with a ChatGPT account and use its plan
// for chat requests through the OpenAI Responses API.
//
// Sign-in is OAuth with PKCE: a one-shot loopback HTTP server on 127.0.0.1 receives
// the redirect, the ID token is verified against OpenAI's published keys (jose), and
// the session is saved encrypted in userData/chatgpt-plan.enc. Tokens are refreshed
// automatically when they are about to expire.
//
// Channels: chatgpt:status, chatgpt:signin, chatgpt:models, chatgpt:stream
//           (+ "chatgpt:chunk" events back to the page), chatgpt:cancel.
// Depends on: electron (app, net, shell, safeStorage), jose (loaded lazily),
//   ../secure-store.cjs, ../response-limits.cjs, ../ipc-validation.cjs, ../bounded-file.cjs.
// Used by: main.cjs (registerChatGPTIpc), preload.cjs (chatgpt* methods).
const { app, net, shell, safeStorage } = require("electron");
const path = require("node:path");
const http = require("node:http");
const crypto = require("node:crypto");
const {
  readJsonBodyLimited,
  readTextBodyLimited,
  MAX_STREAM_TEXT_LENGTH,
  MAX_STREAM_BUFFER_LENGTH,
} = require("../response-limits.cjs");
const { validateChatGPTRequest } = require("../ipc-validation.cjs");
const { readTextFileLimited } = require("../bounded-file.cjs");
const {
  chatGPTEncryptionReady,
  writeProtectedFile,
  quarantineUnreadableFile,
} = require("../secure-store.cjs");
// Abort controllers for streams in flight, by request ID (used by chatgpt:cancel).
const activeChatGPTRequests = new Map();
// Cached JWKS for verifying ID tokens; created on first sign-in.
let chatGPTJwks = null;
const CHATGPT_AUTH = "https://auth.openai.com/api/accounts/oauth";
const CHATGPT_RESOURCE = "https://api.openai.com/v1";
const chatGPTStorePath = () =>
  path.join(app.getPath("userData"), "chatgpt-plan.enc");
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
function registerChatGPTIpc(ipcMain) {
  ipcMain.handle("chatgpt:status", () => chatGPTStatus());
  ipcMain.handle("chatgpt:signin", () => chatGPTSignIn());
  ipcMain.handle("chatgpt:models", () => chatGPTModels());
  ipcMain.handle("chatgpt:stream", (event, messages, model, requestId) =>
    chatGPTStream(event, messages, model, requestId),
  );
  ipcMain.on("chatgpt:cancel", (_event, requestId) =>
    activeChatGPTRequests.get(requestId)?.abort(),
  );
}
module.exports = { registerChatGPTIpc };
