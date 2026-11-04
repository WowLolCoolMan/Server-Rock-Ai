"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const {
  API_URL,
  chooseInstaller,
  fetchReleaseStatus,
  parseReleaseVersion,
  releasePageUrl,
  selectRelease,
} = require("../electron/github-updates.cjs");

function release(tag, options = {}) {
  const version = tag.replace(/^v/, "");
  const assets = (options.assets || ["Server Rock AI Setup.exe", "Server-Rock-AI-x64.AppImage", "Server-Rock-AI-arm64.AppImage", "Server Rock AI.dmg"])
    .map((name, index) => ({
      id: index + 1,
      name,
      state: "uploaded",
      size: 1024,
      digest: `sha256:${String(index + 1).padStart(64, "0")}`,
      url: `https://api.github.com/repos/WowLolCoolMan/Server-Rock-Ai/releases/assets/${index + 1}`,
    }));
  return {
    tag_name: tag,
    name: tag,
    body: "Release notes",
    draft: false,
    prerelease: Boolean(options.prerelease),
    published_at: "2026-10-01T00:00:00Z",
    html_url: `https://github.com/WowLolCoolMan/Server-Rock-Ai/releases/tag/${tag}`,
    assets,
    version,
  };
}

test("release tags only accept versioned stable and beta channels", () => {
  assert.deepEqual(parseReleaseVersion("v1.2.3"), { version: "1.2.3", beta: false });
  assert.deepEqual(parseReleaseVersion("1.2.3-beta.4"), { version: "1.2.3-beta.4", beta: true });
  assert.equal(parseReleaseVersion("latest"), null);
  assert.equal(parseReleaseVersion("v01.2.3"), null);
  assert.equal(releasePageUrl("v1.2.3"), "https://github.com/WowLolCoolMan/Server-Rock-Ai/releases/tag/v1.2.3");
  assert.throws(() => releasePageUrl("https://evil.example"));
});
test("release selection chooses the newest matching channel and verifies platform asset metadata", () => {
  const list = [release("v1.0.0"), release("v1.2.0"), release("v1.3.0-beta.1", { prerelease: true })];
  assert.equal(selectRelease(list, "stable").version, "1.2.0");
  const beta = selectRelease(list, "beta");
  assert.equal(beta.version, "1.3.0-beta.1");
  assert.equal(chooseInstaller(beta, "linux", "arm64").name, "Server-Rock-AI-arm64.AppImage");
  assert.equal(chooseInstaller(beta, "linux", "x64").name, "Server-Rock-AI-x64.AppImage");
  assert.equal(chooseInstaller(beta, "win32", "x64").name, "Server Rock AI Setup.exe");
  assert.equal(chooseInstaller(beta, "win32", "arm64"), null);
});

test("GitHub update check reports empty releases, rejects redirects, and returns the verified update summary", async () => {
  let request;
  const empty = await fetchReleaseStatus({
    channel: "stable", currentVersion: "1.0.0", platform: "linux", arch: "x64",
    fetchImpl: async (url, options) => { request = { url, options }; return new Response("[]", { status: 200 }); },
  });
  assert.equal(request.url, API_URL);
  assert.equal(request.options.redirect, "error");
  assert.equal(empty.available, false);
  assert.equal(empty.reason, "No stable release is available yet.");

  const update = await fetchReleaseStatus({
    channel: "stable", currentVersion: "1.0.0", platform: "linux", arch: "x64",
    fetchImpl: async () => new Response(JSON.stringify([release("v1.2.0")]), { status: 200 }),
  });
  assert.equal(update.available, true);
  assert.equal(update.version, "1.2.0");
  assert.equal(update.installer.name, "Server-Rock-AI-x64.AppImage");
  assert.match(update.installer.sha256, /^[a-f0-9]{64}$/);

  await assert.rejects(() => fetchReleaseStatus({
    channel: "stable", currentVersion: "1.0.0", platform: "linux", arch: "x64",
    fetchImpl: async () => new Response("redirect", { status: 302 }),
  }), /release check failed \(302\)/);
});
