const { spawn } = require("node:child_process");
const path = require("node:path");

const rendererUrl = process.env.SERVER_ROCK_RENDERER_URL || "http://127.0.0.1:5173";
const desktopRoot = path.join(__dirname, "..");
const electronBinary = require("electron");

async function waitForRenderer() {
  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(rendererUrl);
      if (response.ok) return;
    } catch {
      // Vite is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 200));
  }
  throw new Error(`Renderer did not become available at ${rendererUrl}.`);
}

async function start() {
  await waitForRenderer();
  const child = spawn(electronBinary, ["."], {
    cwd: desktopRoot,
    env: { ...process.env, SERVER_ROCK_RENDERER_URL: rendererUrl },
    stdio: "inherit",
    windowsHide: true,
  });

  const forwardSignal = (signal) => {
    if (!child.killed) child.kill(signal);
  };
  process.on("SIGINT", () => forwardSignal("SIGINT"));
  process.on("SIGTERM", () => forwardSignal("SIGTERM"));
  child.on("error", (error) => {
    console.error(`Unable to start Electron: ${error.message}`);
    process.exitCode = 1;
  });
  child.on("exit", (code, signal) => {
    if (signal) {
      process.kill(process.pid, signal);
      return;
    }
    process.exitCode = code ?? 1;
  });
}

start().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
