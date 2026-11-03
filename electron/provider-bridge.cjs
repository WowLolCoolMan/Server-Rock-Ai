const MAX_ENDPOINT_LENGTH = 2048;
const MAX_SECRET_LENGTH = 4096;
const MAX_ERROR_BODY_LENGTH = 4096;

function validateEndpoint(endpoint) {
  if (typeof endpoint !== "string" || endpoint.length > MAX_ENDPOINT_LENGTH)
    throw new Error("Provider endpoint must be a valid URL under 2048 characters.");

  let url;
  try {
    url = new URL(endpoint);
  } catch {
    throw new Error("Provider endpoint must be a valid absolute URL.");
  }

  if (!(["https:", "http:"].includes(url.protocol)))
    throw new Error("Provider endpoint must use HTTP or HTTPS.");
  if (
    url.protocol === "http:" &&
    !/^(localhost|127\.0\.0\.1|\[::1\])$/i.test(url.hostname)
  )
    throw new Error("Remote provider endpoints must use HTTPS.");
  if (url.username || url.password)
    throw new Error("Provider endpoint must not contain embedded credentials.");
  if (url.hash)
    throw new Error("Provider endpoint must not contain a URL fragment.");
  for (const key of url.searchParams.keys()) {
    if (/^(api[-_]?key|access[-_]?token|auth(orization)?|password|secret|token)$/i.test(key))
      throw new Error("Put provider credentials in the API key setting, not the endpoint URL.");
  }

  return url;
}

function validateMessages(messages, protocol) {
  if (!Array.isArray(messages) || messages.length < 1 || messages.length > 500)
    throw new Error("A chat request must contain between 1 and 500 messages.");

  for (const message of messages) {
    if (!message || typeof message !== "object" || Array.isArray(message))
      throw new Error("Every chat message must be an object.");
    if (typeof message.role !== "string" || !message.role.trim())
      throw new Error("Every chat message must have a role.");
    if (!(typeof message.content === "string" || Array.isArray(message.content)))
      throw new Error("Every chat message must contain text or content parts.");
  }

  if (protocol === "anthropic") {
    for (const message of messages) {
      if (!(["system", "user", "assistant"].includes(message.role)))
        throw new Error("Anthropic Messages accepts system, user, and assistant messages only.");
    }
    if (!messages.some((message) => message.role !== "system"))
      throw new Error("Anthropic Messages requires at least one user or assistant message.");
  }
}

function asSafeNumber(value) {
  return value !== null && value !== undefined && value !== "" && Number.isFinite(Number(value)) && Number(value) >= 0
    ? Number(value)
    : undefined;
}

function normalizeUsage(providerUsage, protocol) {
  if (!providerUsage || typeof providerUsage !== "object") return undefined;
  const usage = protocol === "anthropic"
    ? {
        inputTokens: asSafeNumber(providerUsage.input_tokens),
        outputTokens: asSafeNumber(providerUsage.output_tokens),
      }
    : {
        inputTokens: asSafeNumber(providerUsage.prompt_tokens),
        outputTokens: asSafeNumber(providerUsage.completion_tokens),
        totalTokens: asSafeNumber(providerUsage.total_tokens),
      };
  for (const [key, value] of Object.entries(usage))
    if (value === undefined) delete usage[key];
  if (usage.totalTokens === undefined && usage.inputTokens !== undefined && usage.outputTokens !== undefined)
    usage.totalTokens = usage.inputTokens + usage.outputTokens;
  return Object.values(usage).some((value) => value !== undefined) ? usage : undefined;
}

function mergeUsage(current, next, protocol) {
  const merged = { ...(current || {}) };
  for (const [key, value] of Object.entries(next || {}))
    if (value !== undefined) merged[key] = value;
  if (protocol === "anthropic" && merged.inputTokens !== undefined && merged.outputTokens !== undefined)
    merged.totalTokens = merged.inputTokens + merged.outputTokens;
  return merged;
}

function textFromOpenAIContent(content) {
  if (typeof content === "string") return content;
  if (!Array.isArray(content)) return "";
  return content
    .map((part) => (part && typeof part.text === "string" ? part.text : ""))
    .join("");
}

function createSseReader(response, { protocol, signal, onChunk, secret }) {
  const reader = response.body?.getReader?.();
  if (!reader) throw new Error("Provider response has no readable stream.");

  let content = "";
  let model;
  let usage;
  let ended = false;
  let buffer = "";
  let eventName = "";
  const decoder = new TextDecoder();

  const emit = (text) => {
    if (!text) return;
    content += text;
    onChunk?.(text);
  };

  const processFrame = (frame) => {
    const dataLines = [];
    for (const line of frame.replaceAll("\r\n", "\n").split("\n")) {
      if (!line || line.startsWith(":")) continue;
      const colon = line.indexOf(":");
      const field = colon < 0 ? line : line.slice(0, colon);
      let value = colon < 0 ? "" : line.slice(colon + 1);
      if (value.startsWith(" ")) value = value.slice(1);
      if (field === "event") eventName = value;
      else if (field === "data") dataLines.push(value);
    }

    const event = eventName;
    eventName = "";
    if (!dataLines.length) return;
    const data = dataLines.join("\n");
    if (data === "[DONE]") {
      ended = true;
      return;
    }

    let payload;
    try {
      payload = JSON.parse(data);
    } catch {
      // Providers may send non-JSON keepalive events. They are not user text.
      return;
    }

    if (payload.error || event === "error" || payload.type === "error") {
      const message = payload.error?.message || payload.message || "Provider stream failed.";
      throw new Error(secret ? String(message).replaceAll(secret, "[redacted]") : String(message));
    }

    if (typeof payload.model === "string") model = payload.model;
    if (protocol === "anthropic") {
      if (payload.type === "message_start") {
        if (typeof payload.message?.model === "string") model = payload.message.model;
        usage = mergeUsage(usage, normalizeUsage(payload.message?.usage, protocol), protocol);
      } else if (payload.type === "message_delta") {
        usage = mergeUsage(usage, normalizeUsage(payload.usage, protocol), protocol);
      } else if (payload.type === "content_block_delta") {
        if (payload.delta?.type === "text_delta") emit(payload.delta.text);
      } else if (payload.delta?.type === "text_delta") {
        emit(payload.delta.text);
      }
      if (payload.type === "message_stop") ended = true;
      return;
    }

    usage = mergeUsage(usage, normalizeUsage(payload.usage, protocol), protocol);
    const choice = payload.choices?.[0];
    emit(textFromOpenAIContent(choice?.delta?.content ?? choice?.message?.content));
    if (choice?.finish_reason && payload.usage)
      usage = mergeUsage(usage, normalizeUsage(payload.usage, protocol), protocol);
  };

  const drainFrames = (flush = false) => {
    for (;;) {
      const match = /\r?\n\r?\n/.exec(buffer);
      if (!match) break;
      const frame = buffer.slice(0, match.index);
      buffer = buffer.slice(match.index + match[0].length);
      processFrame(frame);
      if (ended) return;
    }
    if (flush && buffer.trim()) {
      processFrame(buffer);
      buffer = "";
    }
  };

  return (async () => {
    try {
      while (!ended) {
        if (signal?.aborted) throw signal.reason || new DOMException("The request was aborted.", "AbortError");
        const { value, done } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        drainFrames();
      }
      if (!ended) {
        buffer += decoder.decode();
        drainFrames(true);
      }
      return { content, model, usage: Object.keys(usage || {}).length ? usage : undefined };
    } catch (error) {
      await reader.cancel(error).catch(() => {});
      throw error;
    } finally {
      await reader.cancel().catch(() => {});
      reader.releaseLock?.();
    }
  })();
}

async function readErrorBody(response, secret) {
  if (!response.body?.getReader) return "";
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let body = "";
  try {
    while (body.length < MAX_ERROR_BODY_LENGTH) {
      const { value, done } = await reader.read();
      if (done) break;
      body += decoder.decode(value, { stream: true });
    }
  } finally {
    await reader.cancel().catch(() => {});
  }
  body = body.slice(0, MAX_ERROR_BODY_LENGTH);
  return secret ? body.replaceAll(secret, "[redacted]") : body;
}

function createProviderBridge({ net, getProviderSecret }) {
  if (!net || typeof net.fetch !== "function")
    throw new TypeError("An Electron net module with fetch() is required.");
  if (typeof getProviderSecret !== "function")
    throw new TypeError("A secure provider-secret lookup function is required.");

  return async function streamProviderChat({
    provider,
    protocol = "openai",
    endpoint,
    model,
    messages,
    temperature,
    maxTokens,
    signal,
    onChunk,
  }) {
    if (typeof provider !== "string" || !/^[\w -]{1,64}$/.test(provider))
      throw new Error("Choose a valid provider name.");
    if (!(protocol === "openai" || protocol === "anthropic"))
      throw new Error("Provider protocol must be openai or anthropic.");
    if (typeof model !== "string" || !model.trim() || model.length > 256)
      throw new Error("Choose a model name under 256 characters.");
    if (onChunk !== undefined && typeof onChunk !== "function")
      throw new TypeError("onChunk must be a function when provided.");
    validateEndpoint(endpoint);
    validateMessages(messages, protocol);
    if (signal?.aborted) throw signal.reason || new DOMException("The request was aborted.", "AbortError");

    const secretValue = await getProviderSecret(provider);
    const secret = typeof secretValue === "string" ? secretValue : "";
    if (secret.length > MAX_SECRET_LENGTH)
      throw new Error("Stored provider credential is too long.");
    if (protocol === "anthropic" && !secret)
      throw new Error("Anthropic API requires a provider API key.");

    const headers = {
      Accept: "text/event-stream, application/json",
      "Content-Type": "application/json",
    };
    if (protocol === "anthropic") {
      headers["x-api-key"] = secret;
      headers["anthropic-version"] = "2023-06-01";
    } else if (secret) {
      headers.Authorization = "Bearer " + secret;
    }

    const body = protocol === "anthropic"
      ? makeAnthropicBody({ messages, model, temperature, maxTokens })
      : makeOpenAIBody({ messages, model, temperature, maxTokens });

    const response = await net.fetch(endpoint, {
      method: "POST",
      headers,
      body: JSON.stringify(body),
      signal,
      redirect: "manual",
      credentials: "omit",
      cache: "no-store",
      referrerPolicy: "no-referrer",
    });
    if (response.status >= 300 && response.status < 400 || response.status === 0 || response.type === "opaqueredirect")
      throw new Error("Provider redirected the request; redirects are blocked to protect credentials.");
    if (!response.ok) {
      const details = await readErrorBody(response, secret);
      throw new Error(
        "Provider request failed (" + response.status + "): " + details,
      );
    }

    const contentType = response.headers?.get?.("content-type") || "";
    if (/text\/event-stream/i.test(contentType))
      return createSseReader(response, { protocol, signal, onChunk, secret });
    return readJsonResponse(response, { protocol, onChunk, secret });
  };
}

function makeOpenAIBody({ messages, model, temperature, maxTokens }) {
  const body = { model, messages, stream: true };
  if (Number.isFinite(temperature) && temperature >= 0 && temperature <= 2)
    body.temperature = temperature;
  if (Number.isInteger(maxTokens) && maxTokens >= 1 && maxTokens <= 1_000_000)
    body.max_tokens = maxTokens;
  return body;
}

function makeAnthropicBody({ messages, model, temperature, maxTokens }) {
  const system = messages.filter((message) => message.role === "system");
  const body = {
    model,
    max_tokens: Number.isInteger(maxTokens) && maxTokens >= 1 && maxTokens <= 1_000_000 ? maxTokens : 4096,
    messages: messages.filter((message) => message.role !== "system"),
    stream: true,
  };
  if (system.length) body.system = system.map((message) => message.content).join("\n\n");
  if (Number.isFinite(temperature) && temperature >= 0 && temperature <= 1)
    body.temperature = temperature;
  return body;
}

async function readJsonResponse(response, { protocol, onChunk, secret }) {
  let payload;
  try {
    payload = await response.json();
  } catch {
    throw new Error("Provider returned a successful response that was not valid JSON or an SSE stream.");
  }
  if (payload.error) {
    const message = payload.error.message || "Provider request failed.";
    throw new Error(secret ? String(message).replaceAll(secret, "[redacted]") : String(message));
  }
  const content = protocol === "anthropic"
    ? (payload.content || []).filter((part) => part.type === "text").map((part) => part.text || "").join("")
    : textFromOpenAIContent(payload.choices?.[0]?.message?.content);
  if (content) onChunk?.(content);
  const usage = normalizeUsage(payload.usage, protocol);
  return {
    content,
    model: typeof payload.model === "string" ? payload.model : undefined,
    usage,
  };
}

module.exports = { createProviderBridge, validateEndpoint };
