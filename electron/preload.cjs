const { contextBridge, ipcRenderer } = require("electron");
contextBridge.exposeInMainWorld("desktopModels", {
  download: (repoId) =>
    ipcRenderer.invoke("models:download", repoId),
  searchHuggingFace: (query, task) =>
    ipcRenderer.invoke("hf:search", query, task),
  runHuggingFaceTask: (task, model, prompt, mediaData) =>
    ipcRenderer.invoke("hf:inference", task, model, prompt, mediaData),
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
  terminalStart: (options) => ipcRenderer.invoke("terminal:start", options),
  terminalWrite: (id, input) => ipcRenderer.invoke("terminal:write", id, input),
  terminalStop: (id, reason) => ipcRenderer.invoke("terminal:stop", id, reason),
  terminalList: () => ipcRenderer.invoke("terminal:list"),
  terminalSnapshot: (id) => ipcRenderer.invoke("terminal:snapshot", id),
  terminalAttach: (id) => ipcRenderer.invoke("terminal:attach", id),
  terminalOnEvent: (onEvent) => {
    const handler = (_event, data) => onEvent(data);
    ipcRenderer.on("terminal:event", handler);
    return () => ipcRenderer.removeListener("terminal:event", handler);
  },
  scanHosts: () => ipcRenderer.invoke("models:scan-hosts"),
  getProviderSecretStatus: (provider) =>
    ipcRenderer.invoke("secrets:status", provider),
  setProviderSecret: (provider, secret) =>
    ipcRenderer.invoke("secrets:set", provider, secret),
  scanProvider: (provider, endpoint) =>
    ipcRenderer.invoke("models:scan-provider", provider, endpoint),
  streamProviderChat: (request, onChunk) => {
    const handler = (_event, data) => {
      if (data.requestId === request.requestId) onChunk(data.text);
    };
    ipcRenderer.on("provider:chunk", handler);
    return ipcRenderer
      .invoke("provider:stream", request)
      .finally(() => ipcRenderer.removeListener("provider:chunk", handler));
  },
  cancelProviderChat: (requestId) =>
    ipcRenderer.send("provider:cancel", requestId),
  githubBeginSignIn: (clientId) =>
    ipcRenderer.invoke("github:begin-signin", clientId),
  githubDefaultClientId: () => ipcRenderer.invoke("github:default-client-id"),
  githubPollSignIn: (id) => ipcRenderer.invoke("github:poll-signin", id),
  githubCancelSignIn: (id) => ipcRenderer.send("github:cancel-signin", id),
  githubStatus: () => ipcRenderer.invoke("github:status"),
  githubDisconnect: () => ipcRenderer.invoke("github:disconnect"),
  githubRepos: () => ipcRenderer.invoke("github:repos"),
  githubResolveRepo: (value) => ipcRenderer.invoke("github:resolve-repo", value),
  githubClone: (repo) => ipcRenderer.invoke("github:clone", repo),
  githubProjectStatus: (folders) =>
    ipcRenderer.invoke("github:status-project", folders),
  githubPushProject: (folders, message, expectedFullName, expectedChangeToken) =>
    ipcRenderer.invoke("github:push-project", folders, message, expectedFullName, expectedChangeToken),
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
  aiUpdateStatus: () => ipcRenderer.invoke("ai-updates:status"),
  aiUpdateCheck: (ring) => ipcRenderer.invoke("ai-updates:check", ring),
  aiUpdateRollback: (ring) => ipcRenderer.invoke("ai-updates:rollback", ring),
  aiUpdateDownload: (ring, version) => ipcRenderer.invoke("ai-updates:download", ring, version),
});
