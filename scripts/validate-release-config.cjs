const fs = require("node:fs");
const path = require("node:path");

const requiredWhenSigning = [
  'WINDOWS_CODESIGN_CERT_BASE64',
  'WINDOWS_CODESIGN_CERT_PASSWORD',
  'WINDOWS_CODESIGN_TIMESTAMP_URL',
  'RELEASE_GPG_PRIVATE_KEY',
];

const root = path.resolve(__dirname, "..");
const manifestPath = path.join(root, "packaging", "native", "release-manifest.json");
const propsPath = path.join(root, "native", "Directory.Build.props");
const errors = [];

let manifest;
try {
  manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
} catch (error) {
  errors.push(`Unable to read packaging/native/release-manifest.json: ${error.message}`);
}

const requireText = (value, field, maximum = 256) => {
  if (typeof value !== "string" || value.trim().length === 0 || value.length > maximum || /[\u0000-\u001f\u007f]/.test(value)) {
    errors.push(`release manifest field ${field} must be a bounded non-empty text value`);
    return false;
  }
  return true;
};

if (manifest) {
  for (const field of ["version", "displayName", "installFolder", "launcherName", "artifact", "profilePath", "installBehavior"]) {
    requireText(manifest[field], field);
  }
  if (typeof manifest.artifact === "string" && (path.isAbsolute(manifest.artifact) || manifest.artifact.includes("/") || manifest.artifact.includes("\\"))) {
    errors.push("release manifest artifact must be a filename, not a path");
  }
  if (typeof manifest.installFolder === "string" && !/^[a-z0-9][a-z0-9.-]{0,63}$/.test(manifest.installFolder)) {
    errors.push("release manifest installFolder must be a stable lowercase package identifier");
  }
  if (typeof manifest.launcherName === "string" && !/^[a-z0-9][a-z0-9.-]{0,63}$/.test(manifest.launcherName)) {
    errors.push("release manifest launcherName must be a stable lowercase package identifier");
  }
  if (typeof manifest.version === "string" && manifest.version !== "0.0.0" && !/^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$/.test(manifest.version.replace(/^v/, ""))) {
    errors.push(`Invalid release manifest version: ${manifest.version}`);
  }
}

try {
  const props = fs.readFileSync(propsPath, "utf8");
  const match = props.match(/<ProductDisplayName>([^<]+)<\/ProductDisplayName>/);
  if (!match) errors.push("native/Directory.Build.props is missing ProductDisplayName");
  else if (manifest && manifest.displayName !== match[1].trim()) errors.push("release manifest displayName must match native ProductDisplayName");
} catch (error) {
  errors.push(`Unable to read native/Directory.Build.props: ${error.message}`);
}

if (process.env.RELEASE_SIGNING_READY === 'true') {
  const missing = requiredWhenSigning.filter((name) => !String(process.env[name] || '').trim());
  if (missing.length) {
    console.error(`Release signing is enabled but required material is missing: ${missing.join(', ')}`);
    process.exit(1);
  }
}

const version = String(process.env.RELEASE_VERSION || '').replace(/^v/, '');
if (version && !/^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$/.test(version)) {
  console.error(`Invalid release version: ${version}`);
  process.exit(1);
}

if (errors.length > 0) {
  console.error("Release configuration validation failed:");
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log("Release configuration is internally consistent, including package metadata.");
