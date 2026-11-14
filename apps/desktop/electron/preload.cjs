const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("serverRockAgent", {
  request: (method, params) => ipcRenderer.invoke("agent:request", method, params),
  pet: {
    configure: (settings) => ipcRenderer.send("pet:configure", settings),
    setActivity: (activity) => ipcRenderer.send("pet:activity", activity),
    getConfig: () => ipcRenderer.invoke("pet:get-config"),
    onConfig: (listener) => {
      const handler = (_event, config) => listener(config);
      ipcRenderer.on("pet:config", handler);
      return () => ipcRenderer.removeListener("pet:config", handler);
    },
    onPosition: (listener) => {
      const handler = (_event, position) => listener(position);
      ipcRenderer.on("pet:position", handler);
      return () => ipcRenderer.removeListener("pet:position", handler);
    },
    onVisibilityChange: (listener) => {
      const handler = (_event, visible) => listener(visible);
      ipcRenderer.on("pet:visibility", handler);
      return () => ipcRenderer.removeListener("pet:visibility", handler);
    },
    pickAsset: (kind) => ipcRenderer.invoke("pet:pick-asset", kind),
    dragStart: (position) => ipcRenderer.send("pet:drag-start", position),
    dragMove: (position) => ipcRenderer.send("pet:drag-move", position),
    dragEnd: () => ipcRenderer.send("pet:drag-end"),
    hide: () => ipcRenderer.send("pet:hide"),
  },
});
