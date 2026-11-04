const fs = require("node:fs/promises");

const MAX_ENCRYPTED_STATE_BYTES = 1024 * 1024;

async function readTextFileLimited(filePath, maxBytes = MAX_ENCRYPTED_STATE_BYTES, label = "Saved file") {
  const handle = await fs.open(filePath, "r");
  try {
    const stat = await handle.stat();
    if (stat.size > maxBytes) throw Error(`${label} exceeds the ${maxBytes}-byte limit.`);
    const buffer = Buffer.alloc(Number(stat.size));
    let offset = 0;
    while (offset < buffer.length) {
      const { bytesRead } = await handle.read(buffer, offset, buffer.length - offset, offset);
      if (!bytesRead) break;
      offset += bytesRead;
    }
    return buffer.subarray(0, offset).toString("utf8");
  } finally {
    await handle.close();
  }
}

module.exports = { MAX_ENCRYPTED_STATE_BYTES, readTextFileLimited };
