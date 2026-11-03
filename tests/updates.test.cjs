const { test } = require("node:test");
const assert = require("node:assert/strict");
const crypto = require("node:crypto");
const fs = require("node:fs/promises");
const os = require("node:os");
const path = require("node:path");
const { Readable } = require("node:stream");
const {
  compareVersions,
  downloadVerifiedPackage,
  fetchManifest,
  platformFor,
  verifyManifest,
} = require("../electron/updates.cjs");

function signedManifest(version = "1.2.3", channel = "ai-stable-linux-x64", artifact = Buffer.from("verified app package")) {
  const { privateKey, publicKey } = crypto.generateKeyPairSync("ed25519");
  const rawPublic = publicKey.export({ format: "der", type: "spki" }).subarray(-32);
  const unsigned = {
    artifact_sha256: crypto.createHash("sha256").update(artifact).digest("hex"),
    artifact_url: `https://www.server-rock.com/updates/${channel}/${version}/artifact`,
    channel,
    minimum_version: "",
    published_at: "2026-11-03T12:00:00Z",
    rollback_version: "",
    version,
  };
  const canonical = Buffer.from(JSON.stringify(Object.fromEntries(Object.entries(unsigned).sort(([a], [b]) => a < b ? -1 : a > b ? 1 : 0))), "ascii");
  return {
    artifact,
    publicKeyB64: rawPublic.toString("base64"),
    manifest: { ...unsigned, signature: crypto.sign(null, canonical, privateKey).toString("base64url") },
  };
}

test("AI update signatures bind Core origin, product channel and exact manifest", () => {
  const { manifest, publicKeyB64 } = signedManifest();
  assert.equal(verifyManifest(manifest, { publicKeyB64, origin: "https://www.server-rock.com", channel: manifest.channel }).version, "1.2.3");
  assert.throws(() => verifyManifest({ ...manifest, artifact_sha256: "0".repeat(64) }, { publicKeyB64, origin: "https://www.server-rock.com", channel: manifest.channel }), /signature/);
  assert.throws(() => verifyManifest(manifest, { publicKeyB64, origin: "https://attacker.example", channel: manifest.channel }), /hosted by Server Rock Core/);
  assert.throws(() => verifyManifest({ ...manifest, artifact_url: `${manifest.artifact_url}?redirect=1` }, { publicKeyB64, origin: "https://www.server-rock.com", channel: manifest.channel }), /hosted by Server Rock Core/);
  assert.throws(() => verifyManifest(manifest, { publicKeyB64, origin: "https://www.server-rock.com", channel: "ai-beta-linux-x64" }), /different channel/);
});

test("the client verifies the canonical Ed25519 signature produced by Core's Python signer", () => {
  const manifest = {
    artifact_sha256: "8cf3a5dd0310a07a1e2afc7437b185b0728cf1a789d648d0a0c9266d7509397f",
    artifact_url: "https://www.server-rock.com/updates/ai-stable-linux-x64/1.2.3/artifact",
    channel: "ai-stable-linux-x64",
    minimum_version: "",
    published_at: "2026-11-03T12:00:00Z",
    rollback_version: "",
    signature: "elUi-24gb-Ftl70yRBKzdGoFbv0OnyMOOP5zdBv14vPU_woIHOXXM-quiDHuW-JYGushbVbuRwGq_sZnkkbxAQ",
    version: "1.2.3",
  };
  assert.equal(verifyManifest(manifest, {
    publicKeyB64: "A6EHv/POEL4dcN0Y50vAmWfk1jCbpQ1fHdyGZBJVMbg=",
    origin: "https://www.server-rock.com", channel: manifest.channel,
  }).version, "1.2.3");
});

test("channel version comparison and platform selection support the declared release matrix", () => {
  assert.equal(compareVersions("1.2.0-beta.9", "1.2.0-beta.10"), -1);
  assert.equal(compareVersions("1.2.0", "1.2.0-beta.10"), 1);
  assert.equal(platformFor("win32", "x64"), "windows-x64");
  assert.equal(platformFor("darwin", "arm64"), "macos-arm64");
  assert.equal(platformFor("linux", "arm64"), "linux-arm64");
  assert.throws(() => platformFor("linux", "ia32"), /not published/);
  assert.throws(() => compareVersions("1.2.3-beta.01", "1.2.3"), /version is invalid/);
  assert.throws(() => compareVersions("1.2.3+build..7", "1.2.3"), /version is invalid/);
});

test("package download writes only a SHA-256 verified file and removes tampered packages", async () => {
  const { manifest, publicKeyB64, artifact } = signedManifest();
  const directory = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-ai-updates-"));
  const responseFor = (bytes) => new Response(Readable.toWeb(Readable.from([bytes])), {
    status: 200, headers: { "content-length": String(bytes.length) },
  });
  try {
    const destination = await downloadVerifiedPackage(manifest, {
      origin: "https://www.server-rock.com", publicKeyB64, sessionToken: "signed-core-session",
      directory, fetchImpl: async (url, options) => {
        assert.equal(String(url), manifest.artifact_url);
        assert.equal(options.redirect, "error");
        assert.equal(options.headers.Authorization, "Bearer signed-core-session");
        return responseFor(artifact);
      },
    });
    assert.deepEqual(await fs.readFile(destination), artifact);
    await fs.rm(destination);
    await assert.rejects(downloadVerifiedPackage(manifest, {
      origin: "https://www.server-rock.com", publicKeyB64, sessionToken: "signed-core-session",
      directory, fetchImpl: async () => responseFor(Buffer.from("tampered")),
    }), /does not match/);
    assert.deepEqual(await fs.readdir(directory), []);
  } finally { await fs.rm(directory, { recursive: true, force: true }); }
});

test("manifest fetch uses only the signed-in Core bearer session and refuses redirects", async () => {
  const { manifest, publicKeyB64 } = signedManifest();
  let seen;
  const loaded = await fetchManifest({
    origin: "https://www.server-rock.com", publicKeyB64, sessionToken: "verified-session",
    platform: "linux-x64", fetchImpl: async (url, options) => {
      seen = { url: String(url), options };
      return Response.json(manifest);
    },
  });
  assert.equal(loaded.version, manifest.version);
  assert.equal(seen.url, "https://www.server-rock.com/updates/ai-stable-linux-x64/manifest");
  assert.equal(seen.options.headers.Authorization, "Bearer verified-session");
  assert.equal(seen.options.redirect, "error");
  await assert.rejects(fetchManifest({ origin: "https://www.server-rock.com", publicKeyB64, sessionToken: "", platform: "linux-x64" }), /Sign in/);
});
