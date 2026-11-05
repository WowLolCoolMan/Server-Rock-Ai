const fs = require('fs');
const path = require('path');

const platform = process.env.RELEASE_PLATFORM;
const arch = process.env.RELEASE_ARCH;
const version = process.env.RELEASE_VERSION;

const expected = {
  win: ['.exe'],
  mac: ['.dmg', '.zip'],
  linux: ['.AppImage', '.deb', '.tar.gz'],
};

if (!expected[platform] || !/^(x64|arm64)$/.test(arch || '') || !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(version || '')) {
  console.error('Set RELEASE_PLATFORM (win|mac|linux), RELEASE_ARCH (x64|arm64), and RELEASE_VERSION before auditing artifacts.');
  process.exit(1);
}

const dist = path.resolve('dist');
if (!fs.existsSync(dist)) {
  console.error('dist/ does not exist; packaging did not produce artifacts.');
  process.exit(1);
}

const files = fs.readdirSync(dist).filter((entry) => {
  const fullPath = path.join(dist, entry);
  return fs.statSync(fullPath).isFile();
});

// NSIS setup names intentionally omit the architecture; portable/macOS/Linux
// artifacts may include it. Match the immutable release version and validate
// the platform's complete expected extension set instead of assuming one name
// shape across every electron-builder target.
const candidates = files.filter((entry) => entry.includes(version));
const missing = expected[platform].filter((extension) => !candidates.some((entry) => entry.endsWith(extension)));
if (missing.length > 0) {
  console.error(`Missing expected ${platform}/${arch} artifact(s): ${missing.join(', ')}`);
  process.exit(1);
}

const invalid = candidates.filter((entry) => {
  const fullPath = path.join(dist, entry);
  return fs.statSync(fullPath).size < 1024;
});
if (invalid.length > 0) {
  console.error(`Artifact(s) are unexpectedly small: ${invalid.join(', ')}`);
  process.exit(1);
}

console.log(`Release artifact audit passed for ${platform}/${arch}: ${candidates.join(', ')}`);
