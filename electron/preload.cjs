const { contextBridge, ipcRenderer } = require("electron");
contextBridge.exposeInMainWorld("desktopModels", {
  download: (repoId, token) =>
    ipcRenderer.invoke("models:download", repoId, token),
  generate: (repoId, task, prompt, requestId, onChunk, imageData) => {
    const handler = (_event, data) => {
      if (data.requestId === requestId) onChunk(data.text);
    };
    ipcRenderer.on("models:chunk", handler);
    return ipcRenderer
      .invoke("models:generate", repoId, task, prompt, requestId, imageData)
      .finally(() => ipcRenderer.removeListener("models:chunk", handler));
  },
  cancel: (requestId) => ipcRenderer.send("models:cancel", requestId),
  pickFolder: () => ipcRenderer.invoke("projects:pick-folder"),
  readProjectFiles: (folders) =>
    ipcRenderer.invoke("projects:read-files", folders),
  writeProjectFile: (folders, relativePath, content) =>
    ipcRenderer.invoke("projects:write-file", folders, relativePath, content),
  scanHosts: () => ipcRenderer.invoke("models:scan-hosts"),
  getProviderSecret: (provider) => ipcRenderer.invoke("secrets:get", provider),
  setProviderSecret: (provider, secret) =>
    ipcRenderer.invoke("secrets:set", provider, secret),
  scanProvider: (provider, endpoint, apiKey) =>
    ipcRenderer.invoke("models:scan-provider", provider, endpoint, apiKey),
  chatgptStatus: () => ipcRenderer.invoke("chatgpt:status"),
  chatgptSignIn: () => ipcRenderer.invoke("chatgpt:signin"),
  chatgptModels: () => ipcRenderer.invoke("chatgpt:models"),
  chatgptStream: (messages, model, requestId, onChunk) => {
    const h = (_e, d) => {
      if (d.requestId === requestId) onChunk(d.text);
    };
    ipcRenderer.on("chatgpt:chunk", h);
    return ipcRenderer
      .invoke("chatgpt:stream", messages, model, requestId)
      .finally(() => ipcRenderer.removeListener("chatgpt:chunk", h));
  },
  cancelChatGPT: (id) => ipcRenderer.send("chatgpt:cancel", id),
});
