// Encrypted on-disk storage for secrets (provider API keys, the GitHub token).
//
// Secrets are encrypted with Electron's safeStorage (the OS keychain) and written
// to userData/provider-secrets.enc. On Linux the plain-text "basic_text" backend
// is refused, so keys are never stored unencrypted.
//
// Depends on: electron (app, safeStorage), bounded-file.cjs, ipc-validation.cjs.
// Used by: main.cjs, ipc/secrets.cjs, ipc/chatgpt.cjs (the ChatGPT session uses the
//   same encryption check and atomic writer), and the GitHub/Hugging Face/provider
//   handlers that read saved keys.
const { app, safeStorage } = require("electron");
const path = require("node:path");
const fs = require("node:fs/promises");
const crypto = require("node:crypto");
const { readTextFileLimited } = require("./bounded-file.cjs");
const { validateProviderName } = require("./ipc-validation.cjs");
const providerSecretsPath = () =>
  path.join(app.getPath("userData"), "provider-secrets.enc");
// True when the OS can encrypt secrets for us. Despite the name it guards every
// encrypted store, not just the ChatGPT one.
function chatGPTEncryptionReady() {
  try {
    if (!safeStorage.isEncryptionAvailable()) return false;
    return (
      process.platform !== "linux" ||
      safeStorage.getSelectedStorageBackend() !== "basic_text"
    );
  } catch {
    return false;
  }
}
// Writes a file atomically (temp file + rename) with owner-only permissions.
async function writeProtectedFile(destination, content) {
  await fs.mkdir(path.dirname(destination), { recursive: true });
  const temp = path.join(
    path.dirname(destination),
    `.${path.basename(destination)}-${crypto.randomUUID()}.tmp`,
  );
  try {
    await fs.writeFile(temp, content, { encoding: "utf8", mode: 0o600, flag: "wx" });
    await fs.rename(temp, destination);
  } finally {
    await fs.rm(temp, { force: true }).catch(() => {});
  }
}
// Moves an unreadable credential file aside so the app can start clean without
// destroying data the user might want to recover.
async function quarantineUnreadableFile(filePath) {
  try {
    await fs.access(filePath);
    const recoveryPath = `${filePath}.corrupt-${crypto.randomUUID()}`;
    await fs.rename(filePath, recoveryPath);
    console.warn(`Preserved unreadable credential data at ${recoveryPath}`);
  } catch {
    // Missing or inaccessible files cannot be quarantined; callers still get a clean state.
  }
}
async function readProviderSecrets() {
  if (!chatGPTEncryptionReady()) return {};
  try {
    const encoded = await readTextFileLimited(providerSecretsPath(), undefined, "Provider credential store");
    return JSON.parse(
      safeStorage.decryptString(Buffer.from(encoded, "base64")),
    );
  } catch (error) {
    if (error?.code !== "ENOENT") await quarantineUnreadableFile(providerSecretsPath());
    return {};
  }
}
async function writeProviderSecret(provider, secret) {
  if (!chatGPTEncryptionReady())
    throw Error("Secure OS credential storage is unavailable.");
  if (typeof provider !== "string" || !/^[\w -]{1,64}$/.test(provider))
    throw Error("Choose a valid provider.");
  if (typeof secret !== "string" || secret.length > 4096)
    throw Error("Provider keys must be 4096 characters or fewer.");
  const secrets = await readProviderSecrets();
  if (secret.trim()) secrets[provider] = secret.trim();
  else delete secrets[provider];
  const dest = providerSecretsPath(),
    encoded = safeStorage
      .encryptString(JSON.stringify(secrets))
      .toString("base64");
  await writeProtectedFile(dest, encoded);
  return { saved: Boolean(secret.trim()) };
}
// Returns the saved key for one provider, or "" when none is saved.
const readSecureProviderSecret = async (provider) => {
  validateProviderName(provider);
  const secrets = await readProviderSecrets();
  return secrets[provider] || "";
};
module.exports = {
  chatGPTEncryptionReady,
  writeProtectedFile,
  quarantineUnreadableFile,
  readProviderSecrets,
  writeProviderSecret,
  readSecureProviderSecret,
};
