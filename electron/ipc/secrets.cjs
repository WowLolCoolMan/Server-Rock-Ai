// IPC: provider API keys (Settings → provider credentials).
//
// Channels: secrets:status (is a key saved? is secure storage available?),
//           secrets:set (save, or clear with an empty string).
// Depends on: ../secure-store.cjs, ../ipc-validation.cjs.
// Used by: main.cjs (registerSecretsIpc), preload.cjs (getProviderSecretStatus, setProviderSecret).
const {
  chatGPTEncryptionReady,
  writeProviderSecret,
  readSecureProviderSecret,
} = require("../secure-store.cjs");
const { validateProviderName } = require("../ipc-validation.cjs");
function registerSecretsIpc(ipcMain) {
  ipcMain.handle("secrets:status", async (_event, provider) => ({
    saved: Boolean(await readSecureProviderSecret(provider)),
    secureStorage: chatGPTEncryptionReady(),
  }));
  ipcMain.handle("secrets:set", (_event, provider, secret) =>
    (validateProviderName(provider), writeProviderSecret(provider, secret)),
  );
}
module.exports = { registerSecretsIpc };
