"use strict";

const crypto = require("node:crypto");
const fs = require("node:fs/promises");
const path = require("node:path");

const MAX_PACKAGE_BYTES = 1024 * 1024 * 1024;
const PLATFORM_SUFFIX = {
  "windows-x64": ".exe",
  "macos-x64": ".dmg",
  "macos-arm64": ".dmg",
  "linux-x64": ".AppImage",
  "linux-arm64": ".AppImage",
};
const ED25519_SPKI_PREFIX = Buffer.from("302a300506032b6570032100", "hex");

function coreOrigin(value) {
  let parsed;
  try { parsed = new URL(value); } catch { throw new Error("Server Rock Core update origin is not configured."); }
  if (parsed.protocol !== "https:" || parsed.username || parsed.password || parsed.pathname !== "/" || parsed.search || parsed.hash)
    throw new Error("Server Rock Core must use a trusted HTTPS origin.");
  return parsed.origin;
}

function platformFor(platform = process.platform, arch = process.arch) {
  const name = platform === "win32" ? "windows" : platform === "darwin" ? "macos" : platform === "linux" ? "linux" : "";
  const cpu = arch === "x64" ? "x64" : arch === "arm64" ? "arm64" : "";
  if (!name || !cpu) throw new Error("Updates are not published for this operating system and CPU architecture.");
  const selected = `${name}-${cpu}`;
  if (!PLATFORM_SUFFIX[selected]) throw new Error("Updates are not published for this operating system and CPU architecture.");
  return selected;
}

function parseVersion(value) {
  if (typeof value !== "string" || value.length > 128) throw new Error("The update version is invalid.");
  const match = /^(0|[1-9]\d{0,17})\.(0|[1-9]\d{0,17})\.(0|[1-9]\d{0,17})(?:-([0-9A-Za-z.-]+))?(?:\+([0-9A-Za-z.-]+))?$/.exec(value);
  if (!match) throw new Error("The update version is invalid.");
  const prerelease = match[4] ? match[4].split(".") : [];
  if (prerelease.length > 32 || prerelease.some((part) => !part || /^0\d+$/.test(part) || /^\d+$/.test(part) && part.length > 18))
    throw new Error("The update version is invalid.");
  if (match[5] && (match[5].split(".").length > 32 || match[5].split(".").some((part) => !part || part.length > 64)))
    throw new Error("The update version is invalid.");
  return { core: match.slice(1, 4).map(Number), prerelease };
}

function compareVersions(left, right) {
  const a = parseVersion(left), b = parseVersion(right);
  for (let index = 0; index < 3; index++) if (a.core[index] !== b.core[index]) return a.core[index] < b.core[index] ? -1 : 1;
  if (!a.prerelease.length) return b.prerelease.length ? 1 : 0;
  if (!b.prerelease.length) return -1;
  for (let index = 0; index < Math.min(a.prerelease.length, b.prerelease.length); index++) {
    if (a.prerelease[index] === b.prerelease[index]) continue;
    const an = /^\d+$/.test(a.prerelease[index]), bn = /^\d+$/.test(b.prerelease[index]);
    if (an && bn) return BigInt(a.prerelease[index]) < BigInt(b.prerelease[index]) ? -1 : 1;
    if (an !== bn) return an ? -1 : 1;
    return a.prerelease[index] < b.prerelease[index] ? -1 : 1;
  }
  return Math.sign(a.prerelease.length - b.prerelease.length);
}

function signedFields(manifest) {
  return {
    artifact_sha256: manifest.artifact_sha256,
    artifact_url: manifest.artifact_url,
    channel: manifest.channel,
    minimum_version: manifest.minimum_version,
    published_at: manifest.published_at,
    rollback_version: manifest.rollback_version,
    version: manifest.version,
  };
}

function verifyManifest(manifest, { publicKeyB64, origin, channel }) {
  const expected = new Set([
    "version", "channel", "artifact_url", "artifact_sha256", "published_at",
    "minimum_version", "rollback_version", "signature",
  ]);
  if (!manifest || typeof manifest !== "object" || Object.keys(manifest).length !== expected.size
      || Object.keys(manifest).some((key) => !expected.has(key))) throw new Error("Core returned an unsupported update manifest.");
  if (manifest.channel !== channel || typeof manifest.version !== "string") throw new Error("Core returned an update for a different channel.");
  parseVersion(manifest.version);
  if (manifest.minimum_version) parseVersion(manifest.minimum_version);
  if (manifest.rollback_version) parseVersion(manifest.rollback_version);
  if (!/^[a-f0-9]{64}$/.test(manifest.artifact_sha256)) throw new Error("The signed package hash is invalid.");
  if (typeof manifest.published_at !== "string" || !Number.isFinite(Date.parse(manifest.published_at))) throw new Error("The signed release date is invalid.");
  const artifact = new URL(manifest.artifact_url);
  if (artifact.origin !== coreOrigin(origin) || artifact.search || artifact.hash
      || artifact.pathname !== `/updates/${channel}/${manifest.version}/artifact`)
    throw new Error("The signed package is not hosted by Server Rock Core.");
  if (typeof publicKeyB64 !== "string" || !/^[A-Za-z0-9+/]+={0,2}$/.test(publicKeyB64)) throw new Error("Core's update verification key is not configured.");
  const rawKey = Buffer.from(publicKeyB64, "base64");
  const signature = Buffer.from(String(manifest.signature).replace(/-/g, "+").replace(/_/g, "/") + "=".repeat((4 - String(manifest.signature).length % 4) % 4), "base64");
  if (rawKey.length !== 32 || signature.length !== 64) throw new Error("Core's update signature is malformed.");
  const publicKey = crypto.createPublicKey({ key: Buffer.concat([ED25519_SPKI_PREFIX, rawKey]), format: "der", type: "spki" });
  const canonical = Buffer.from(JSON.stringify(Object.fromEntries(Object.entries(signedFields(manifest)).sort(([a], [b]) => a < b ? -1 : a > b ? 1 : 0))), "ascii");
  if (!crypto.verify(null, canonical, publicKey, signature)) throw new Error("Core's release signature could not be verified.");
  return manifest;
}

async function fetchManifest({ origin, publicKeyB64, sessionToken, ring = "stable", platform, version = "", fetchImpl = fetch }) {
  const base = coreOrigin(origin);
  if (!sessionToken || typeof sessionToken !== "string" || sessionToken.length > 4096) throw new Error("Sign in to your Server Rock account to check for updates.");
  if (!new Set(["stable", "beta"]).has(ring)) throw new Error("Choose the stable or beta update channel.");
  const selectedPlatform = platform || platformFor();
  if (!PLATFORM_SUFFIX[selectedPlatform]) throw new Error("Updates are not published for this platform.");
  const channel = `ai-${ring}-${selectedPlatform}`;
  const url = new URL(`/updates/${channel}/manifest`, base);
  if (version) url.searchParams.set("version", version);
  const response = await fetchImpl(url, {
    headers: { Accept: "application/json", Authorization: `Bearer ${sessionToken}` },
    cache: "no-store", redirect: "error",
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok) throw new Error(payload?.detail || `Core update check failed (${response.status}).`);
  return verifyManifest(payload, { publicKeyB64, origin: base, channel });
}

async function downloadVerifiedPackage(manifest, { origin, publicKeyB64, sessionToken, directory, fetchImpl = fetch }) {
  const verified = verifyManifest(manifest, { publicKeyB64, origin, channel: manifest.channel });
  const selectedPlatform = verified.channel.slice(verified.channel.indexOf("-", 3) + 1);
  const suffix = PLATFORM_SUFFIX[selectedPlatform];
  if (!suffix) throw new Error("The signed release package type is not supported.");
  const response = await fetchImpl(verified.artifact_url, {
    headers: { Accept: "application/octet-stream", Authorization: `Bearer ${sessionToken}` },
    cache: "no-store", redirect: "error",
  });
  if (!response.ok) {
    const payload = await response.json().catch(() => null);
    throw new Error(payload?.detail || `Core package download failed (${response.status}).`);
  }
  const lengthHeader = response.headers.get("content-length");
  if (lengthHeader !== null && (!/^\d+$/.test(lengthHeader) || Number(lengthHeader) > MAX_PACKAGE_BYTES))
    throw new Error("The release package exceeds the 1 GiB download limit or has an invalid size header.");
  const targetDirectory = path.resolve(directory);
  await fs.mkdir(targetDirectory, { recursive: true, mode: 0o700 });
  await fs.chmod(targetDirectory, 0o700);
  const destination = path.join(targetDirectory, `server-rock-ai-${verified.version}-${crypto.randomUUID()}${suffix}`);
  const handle = await fs.open(destination, "wx", 0o600);
  const digest = crypto.createHash("sha256");
  let written = 0;
  try {
    if (!response.body) throw new Error("Core returned an empty package response.");
    for await (const chunk of response.body) {
      const bytes = Buffer.from(chunk);
      written += bytes.length;
      if (written > MAX_PACKAGE_BYTES) throw new Error("The release package exceeds the 1 GiB download limit.");
      digest.update(bytes);
      await handle.write(bytes);
    }
    await handle.sync();
    if (!written || digest.digest("hex") !== verified.artifact_sha256) throw new Error("The downloaded package does not match Core's signed SHA-256 digest.");
  } catch (error) {
    await handle.close().catch(() => {});
    await fs.rm(destination, { force: true }).catch(() => {});
    throw error;
  }
  await handle.close();
  if (process.platform === "linux" && suffix === ".AppImage") await fs.chmod(destination, 0o700);
  return destination;
}

module.exports = { compareVersions, coreOrigin, downloadVerifiedPackage, fetchManifest, parseVersion, platformFor, verifyManifest };
