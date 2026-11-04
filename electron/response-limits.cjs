const MAX_JSON_BODY_BYTES = 8 * 1024 * 1024;
const MAX_TEXT_BODY_BYTES = 16 * 1024;
const MAX_BINARY_BODY_BYTES = 50 * 1024 * 1024;
const MAX_STREAM_TEXT_LENGTH = 2 * 1024 * 1024;
const MAX_STREAM_BUFFER_LENGTH = 256 * 1024;

async function readJsonBodyLimited(response, label = "Remote") {
  const declaredLength = Number(response.headers?.get?.("content-length") || 0);
  if (declaredLength > MAX_JSON_BODY_BYTES)
    throw Error(`${label} response exceeds the 8 MB JSON limit.`);
  const reader = response.body?.getReader?.();
  if (!reader) throw Error(`${label} response body is not readable.`);
  const chunks = [];
  let total = 0;
  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      total += value.byteLength;
      if (total > MAX_JSON_BODY_BYTES)
        throw Error(`${label} response exceeds the 8 MB JSON limit.`);
      chunks.push(Buffer.from(value));
    }
  } finally {
    await reader.cancel().catch(() => {});
  }
  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
  } catch {
    throw Error(`${label} returned invalid JSON.`);
  }
}

async function readTextBodyLimited(response, label = "Remote") {
  const declaredLength = Number(response.headers?.get?.("content-length") || 0);
  if (declaredLength > MAX_TEXT_BODY_BYTES)
    throw Error(`${label} error response exceeds the 16 KB limit.`);
  const reader = response.body?.getReader?.();
  if (!reader) throw Error(`${label} response body is not readable.`);
  const chunks = [];
  let total = 0;
  const decoder = new TextDecoder();
  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      total += value.byteLength;
      if (total > MAX_TEXT_BODY_BYTES)
        throw Error(`${label} error response exceeds the 16 KB limit.`);
      chunks.push(decoder.decode(value, { stream: true }));
    }
    chunks.push(decoder.decode());
  } finally {
    await reader.cancel().catch(() => {});
  }
  return chunks.join("").slice(0, MAX_TEXT_BODY_BYTES);
}

async function readBinaryBodyLimited(response, maxBytes = MAX_BINARY_BODY_BYTES, label = "Remote") {
  const declaredLength = Number(response.headers?.get?.("content-length") || 0);
  if (declaredLength > maxBytes)
    throw Error(`${label} response exceeds the ${Math.floor(maxBytes / (1024 * 1024))} MB limit.`);
  const reader = response.body?.getReader?.();
  if (!reader) throw Error(`${label} response body is not readable.`);
  const chunks = [];
  let total = 0;
  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      total += value.byteLength;
      if (total > maxBytes)
        throw Error(`${label} response exceeds the ${Math.floor(maxBytes / (1024 * 1024))} MB limit.`);
      chunks.push(Buffer.from(value));
    }
  } finally {
    await reader.cancel().catch(() => {});
  }
  return Buffer.concat(chunks);
}

module.exports = {
  MAX_JSON_BODY_BYTES,
  MAX_TEXT_BODY_BYTES,
  MAX_BINARY_BODY_BYTES,
  MAX_STREAM_TEXT_LENGTH,
  MAX_STREAM_BUFFER_LENGTH,
  readJsonBodyLimited,
  readTextBodyLimited,
  readBinaryBodyLimited,
};
