const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("serverRockAgent", {
  request: (method, params) => ipcRenderer.invoke("agent:request", method, params),
});
