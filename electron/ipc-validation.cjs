const MAX_REQUEST_ID_LENGTH = 100;
const MAX_MODEL_NAME_LENGTH = 256;
const MAX_CHAT_MESSAGES = 500;
const MAX_MESSAGE_CONTENT_LENGTH = 1_000_000;
const MAX_CHAT_PAYLOAD_LENGTH = 4_000_000;
const MAX_PROVIDER_CHAT_PAYLOAD_LENGTH = 8_000_000;
const MAX_PROVIDER_NAME_LENGTH = 64;

function validateRequestId(value, label = "request ID") {
  if (typeof value !== "string" || !/^[A-Za-z0-9._:-]{1,100}$/.test(value))
    throw Error(`Invalid ${label}.`);
  return value;
}

function validateProviderName(value) {
  if (typeof value !== "string" || !/^[\w -]{1,64}$/.test(value))
    throw Error("Choose a valid provider name.");
  return value;
}

function validateChatGPTRequest(messages, model, requestId) {
  validateRequestId(requestId, "ChatGPT request ID");
  if (typeof model !== "string" || model.length < 1 || model.length > MAX_MODEL_NAME_LENGTH || /[\u0000-\u001f\u007f]/.test(model))
    throw Error("Invalid ChatGPT model.");
  validateMessagePayload(messages, "ChatGPT", MAX_CHAT_PAYLOAD_LENGTH);
  return { messages, model, requestId };
}

function validateClaudeCodeRequest(messages, model, requestId) {
  validateRequestId(requestId, "Claude Code request ID");
  if (typeof model !== "string" || model.length < 1 || model.length > MAX_MODEL_NAME_LENGTH || !/^[\w.-]+$/.test(model))
    throw Error("Invalid Claude Code model.");
  validateMessagePayload(messages, "Claude Code", MAX_CHAT_PAYLOAD_LENGTH);
  return { messages, model, requestId };
}

function validateMessagePayload(messages, label = "chat", maxTotalLength = MAX_PROVIDER_CHAT_PAYLOAD_LENGTH) {
  if (!Array.isArray(messages) || messages.length < 1 || messages.length > MAX_CHAT_MESSAGES)
    throw Error(`A ${label} request must contain between 1 and ${MAX_CHAT_MESSAGES} messages.`);

  let totalLength = 0;
  for (const message of messages) {
    if (!message || typeof message !== "object" || Array.isArray(message))
      throw Error(`Every ${label} message must be an object.`);
    if (typeof message.role !== "string" || message.role.length < 1 || message.role.length > 64 || /[\u0000-\u001f\u007f]/.test(message.role))
      throw Error(`Every ${label} message must have a valid role.`);
    if (!(typeof message.content === "string" || Array.isArray(message.content)))
      throw Error(`Every ${label} message must contain text or content parts.`);
    let contentLength;
    try {
      const serialized = JSON.stringify(message.content);
      if (typeof serialized !== "string") throw new Error("not serializable");
      contentLength = serialized.length;
    } catch {
      throw Error(`${label} message content is not serializable.`);
    }
    if (contentLength > MAX_MESSAGE_CONTENT_LENGTH)
      throw Error(`A ${label} message is too large.`);
    totalLength += message.role.length + contentLength;
    if (totalLength > maxTotalLength)
      throw Error(`The ${label} request is too large.`);
  }
  return messages;
}

module.exports = {
  MAX_REQUEST_ID_LENGTH,
  MAX_MODEL_NAME_LENGTH,
  MAX_CHAT_MESSAGES,
  MAX_MESSAGE_CONTENT_LENGTH,
  MAX_CHAT_PAYLOAD_LENGTH,
  MAX_PROVIDER_CHAT_PAYLOAD_LENGTH,
  MAX_PROVIDER_NAME_LENGTH,
  validateRequestId,
  validateProviderName,
  validateChatGPTRequest,
  validateClaudeCodeRequest,
  validateMessagePayload,
};
