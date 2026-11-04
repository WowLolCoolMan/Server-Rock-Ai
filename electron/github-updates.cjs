"use strict";

const OWNER = "WowLolCoolMan";
const REPOSITORY = "Server-Rock-Ai";
const API_URL = `https://api.github.com/repos/${OWNER}/${REPOSITORY}/releases?per_page=100`;
const MAX_RELEASE_LIST_BYTES = 2 * 1024 * 1024;
const MAX_PACKAGE_BYTES = 1024 * 1024 * 1024;

function parseReleaseVersion(tag) {
  if (typeof tag !== "string" || tag.length > 80) return null;
  const match = tag.match(/^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(beta)\.(0|[1-9]\d*))?$/);
  if (!match) return null;
  return { version: tag.replace(/^v/, ""), beta: Boolean(match[4]) };
}

function compareVersions(left, right) {
  const parse = (value) => {
    const match = /^(\d+)\.(\d+)\.(\d+)(?:-beta\.(\d+))?$/.exec(value);
    return match ? { numbers: match.slice(1, 4).map(Number), beta: match[4] === undefined ? null : Number(match[4]) } : null;
  };
  const a = parse(left), b = parse(right);
  if (!a || !b) throw Error("GitHub returned an invalid release version.");
  for (let index = 0; index < 3; index++) if (a.numbers[index] !== b.numbers[index]) return a.numbers[index] < b.numbers[index] ? -1 : 1;
  if (a.beta === null || b.beta === null) return a.beta === b.beta ? 0 : a.beta === null ? 1 : -1;
  return Math.sign(a.beta - b.beta);
}

function selectRelease(releases, channel) {
  if (channel !== "stable" && channel !== "beta") throw Error("Choose the stable or beta update channel.");
  if (!Array.isArray(releases) || releases.length > 100) throw Error("GitHub returned an unsupported release list.");
  const candidates = releases.flatMap((release) => {
    if (!release || release.draft === true) return [];
    const parsed = parseReleaseVersion(release.tag_name);
    if (!parsed || parsed.beta !== (channel === "beta") || Boolean(release.prerelease) !== parsed.beta) return [];
    let page;
    try { page = new URL(release.html_url || ""); } catch { return []; }
    if (page.origin !== "https://github.com" || page.pathname !== `/${OWNER}/${REPOSITORY}/releases/tag/${release.tag_name}`) return [];
    const assets = Array.isArray(release.assets) ? release.assets.filter((asset) =>
      asset && asset.state === "uploaded" && typeof asset.name === "string" && asset.name.length <= 255
      && Number.isSafeInteger(asset.size) && asset.size > 0 && asset.size <= MAX_PACKAGE_BYTES
      && typeof asset.digest === "string" && /^sha256:[a-f0-9]{64}$/i.test(asset.digest)
      && typeof asset.url === "string" && asset.url.startsWith(`https://api.github.com/repos/${OWNER}/${REPOSITORY}/releases/assets/`)
      && Number.isSafeInteger(asset.id)
    ) : [];
    if (!release.published_at || !Number.isFinite(Date.parse(release.published_at))) return [];
    return [{ version: parsed.version, tag: release.tag_name, channel, publishedAt: release.published_at,
      url: page.href, notes: String(release.body || "").slice(0, 12000), assets }];
  });
  candidates.sort((a, b) => compareVersions(b.version, a.version));
  return candidates[0] || null;
}

function chooseInstaller(release, platform, arch) {
  if (!release) return null;
  if (platform === "win32" && arch !== "x64") return null;
  const expected = platform === "win32" ? /setup.*\.exe$/i
    : platform === "darwin" ? /\.dmg$/i
      : platform === "linux" ? /\.appimage$/i : null;
  if (!expected) return null;
  const candidates = release.assets.filter((asset) => expected.test(asset.name));
  const archName = arch === "arm64" ? /(?:arm64|aarch64)/i : /(?:x64|x86_64|amd64)/i;
  const archMatches = candidates.filter((asset) => archName.test(asset.name));
  const selected = archMatches.length === 1 ? archMatches[0] : candidates.length === 1 ? candidates[0] : null;
  if (!selected) return null;
  return { name: selected.name, size: selected.size, sha256: selected.digest.slice("sha256:".length).toLowerCase(), assetId: selected.id };
}

async function readBounded(response, maximum) {
  const length = Number(response.headers.get("content-length"));
  if (Number.isFinite(length) && length > maximum) throw Error("GitHub returned a release list that is too large.");
  const reader = response.body?.getReader();
  if (!reader) throw Error("GitHub returned an empty response.");
  const chunks = [];
  let size = 0;
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    size += value.byteLength;
    if (size > maximum) {
      await reader.cancel();
      throw Error("GitHub returned a release list that is too large.");
    }
    chunks.push(Buffer.from(value));
  }
  return Buffer.concat(chunks, size).toString("utf8");
}

async function fetchReleaseStatus({ channel, currentVersion, platform, arch, fetchImpl = fetch }) {
  const response = await fetchImpl(API_URL, {
    headers: { Accept: "application/vnd.github+json", "X-GitHub-Api-Version": "2022-11-28", "User-Agent": "Server-Rock-AI" },
    redirect: "error",
  });
  if (!response.ok) throw Error(`GitHub release check failed (${response.status}).`);
  const payload = JSON.parse(await readBounded(response, MAX_RELEASE_LIST_BYTES));
  const release = selectRelease(payload, channel);
  if (!release) return { channel, available: false, currentVersion, release: null, installer: null, reason: `No ${channel} release is available yet.` };
  const installer = chooseInstaller(release, platform, arch);
  return { ...release, currentVersion, available: compareVersions(currentVersion, release.version) < 0, release, installer,
    reason: installer ? "" : `No verified ${platform}/${arch} installer is attached to this release.` };
}

function releasePageUrl(tag) {
  const parsed = parseReleaseVersion(tag);
  if (!parsed) throw Error("The GitHub release tag is invalid.");
  return `https://github.com/${OWNER}/${REPOSITORY}/releases/tag/${tag}`;
}

module.exports = { API_URL, MAX_PACKAGE_BYTES, chooseInstaller, fetchReleaseStatus, parseReleaseVersion, releasePageUrl, selectRelease };
