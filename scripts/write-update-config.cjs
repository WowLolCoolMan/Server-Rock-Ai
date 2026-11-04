"use strict";

const fs = require("node:fs");
const path = require("node:path");

const originValue = process.env.SERVER_ROCK_AI_CORE_ORIGIN || (process.env.GITHUB_ACTIONS === "true" ? "" : "https://www.server-rock.com");
const origin = originValue.trim().replace(/\/$/, "");
const publicKeyB64 = (process.env.SERVER_ROCK_UPDATE_PUBLIC_KEY_B64 || "").trim();
let parsed;
if (!origin) throw new Error("Set SERVER_ROCK_AI_CORE_ORIGIN before packaging update-enabled releases.");
try { parsed = new URL(origin); } catch { throw new Error("SERVER_ROCK_AI_CORE_ORIGIN must be a valid HTTPS origin."); }
if (parsed.protocol !== "https:" || parsed.username || parsed.password || parsed.pathname !== "/" || parsed.search || parsed.hash)
  throw new Error("SERVER_ROCK_AI_CORE_ORIGIN must be a trusted HTTPS origin.");
if (publicKeyB64 && (!/^[A-Za-z0-9+/]+={0,2}$/.test(publicKeyB64) || Buffer.from(publicKeyB64, "base64").length !== 32))
  throw new Error("SERVER_ROCK_UPDATE_PUBLIC_KEY_B64 must be a base64-encoded 32-byte Ed25519 public key.");
if (process.env.GITHUB_ACTIONS === "true" && !publicKeyB64)
  throw new Error("Set the SERVER_ROCK_UPDATE_PUBLIC_KEY_B64 GitHub Actions variable before packaging update-enabled releases.");

const destination = path.join(__dirname, "..", "electron", "update-config.generated.cjs");
const source = `"use strict";\nmodule.exports = Object.freeze(${JSON.stringify({ origin: parsed.origin, publicKeyB64 })});\n`;
fs.writeFileSync(destination, source, { mode: 0o600 });
console.log(`Configured signed Core updates for ${parsed.origin}; public key ${publicKeyB64 ? "pinned" : "unset"}.`);
