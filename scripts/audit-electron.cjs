const path = require("node:path");
const fs = require("node:fs/promises");
const os = require("node:os");
const { _electron: electron } = require("playwright");

const appEntry = path.resolve(__dirname, "..", "electron", "main.cjs");
const appRoot = path.resolve(__dirname, "..");
const packagedExecutable = process.env.SERVER_ROCK_ELECTRON_EXECUTABLE
  ? path.resolve(process.env.SERVER_ROCK_ELECTRON_EXECUTABLE)
  : "";

(async () => {
  const profile = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-electron-audit-"));
  const project = await fs.mkdtemp(path.join(os.tmpdir(), "server-rock-terminal-audit-"));
  let app;
  try {
    await fs.writeFile(path.join(project, "audit-marker.txt"), "terminal audit\n", "utf8");
    await fs.writeFile(
      path.join(profile, "authorized-project-folders.json"),
      JSON.stringify([project]),
      "utf8",
    );
    const launchOptions = {
      args: [appEntry, `--user-data-dir=${profile}`],
      env: { ...process.env, ELECTRON_DISABLE_SECURITY_WARNINGS: "true" },
    };
    if (packagedExecutable) {
      launchOptions.executablePath = packagedExecutable;
      launchOptions.args = [`--user-data-dir=${profile}`];
    }
    app = await electron.launch(launchOptions);
    const page = await app.firstWindow({ timeout: 20_000 });
    await page.waitForLoadState("domcontentloaded");
    const menuVisible = await app.evaluate(({ BrowserWindow }) =>
      BrowserWindow.getAllWindows()[0]?.isMenuBarVisible?.() ?? false,
    );
    const result = await page.evaluate(async ({ unauthorizedFolder, authorizedFolder }) => {
      const bridge = window.desktopModels || {};
      const list = await bridge.terminalList?.();
      let unauthorizedError = "";
      try {
        await bridge.terminalStart?.({
          folders: [unauthorizedFolder],
          cwd: unauthorizedFolder,
          command: "echo audit",
        });
      } catch (error) {
        unauthorizedError = error?.message || String(error);
      }
      let authorizedTerminal = false;
      let terminalOutput = "";
      let terminalCode = null;
      try {
        const session = await bridge.terminalStart?.({
          folders: [authorizedFolder],
          cwd: authorizedFolder,
          command: "echo audit",
          timeoutMs: 5_000,
        });
        for (let attempt = 0; attempt < 100; attempt += 1) {
          await new Promise((resolve) => setTimeout(resolve, 50));
          const snapshot = await bridge.terminalSnapshot?.(session.id);
          if (snapshot?.status !== "running") {
            terminalOutput = snapshot?.output || "";
            terminalCode = snapshot?.code;
            authorizedTerminal = terminalCode === 0 && /audit/i.test(terminalOutput);
            break;
          }
        }
        if (!authorizedTerminal) await bridge.terminalStop?.(session.id, "audit-timeout");
      } catch (error) {
        terminalOutput = error?.message || String(error);
      }
      return {
        title: document.title,
        terminalBridge: typeof bridge.terminalStart === "function",
        terminalList: Array.isArray(list),
        unauthorizedError,
        authorizedTerminal,
        terminalOutput,
        terminalCode,
      };
    }, { unauthorizedFolder: appRoot, authorizedFolder: project });
    if (result.title !== "Server Rock: AI") throw new Error(`Unexpected desktop title: ${result.title}`);
    if (menuVisible) throw new Error("The native application menu is still visible.");
    if (!result.terminalBridge || !result.terminalList) throw new Error("Sandboxed terminal IPC bridge is unavailable.");
    if (!/authoriz|project folder/i.test(result.unauthorizedError)) throw new Error("Unauthorized terminal folder was not rejected.");
    if (!result.authorizedTerminal) throw new Error(`Authorized terminal execution failed: ${result.terminalCode} ${result.terminalOutput}`);
    console.log("Electron audit passed: menu hidden, sandboxed preload, terminal IPC, folder authorization, and a real authorized command.");
  } finally {
    await app?.close();
    await fs.rm(profile, { recursive: true, force: true });
    await fs.rm(project, { recursive: true, force: true });
  }
})().catch((error) => {
  console.error(error.stack || error.message || error);
  process.exitCode = 1;
});
