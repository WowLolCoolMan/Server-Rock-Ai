const { spawn } = require("node:child_process");
const path = require("node:path");

const desktopRoot = path.join(process.cwd(), "apps", "desktop");
const electronBinary = process.platform === "win32"
  ? path.join(desktopRoot, "node_modules", "electron", "dist", "electron.exe")
  : path.join(desktopRoot, "node_modules", ".bin", "electron");
const child = spawn(electronBinary, ["."], {
  cwd: desktopRoot,
  env: process.env,
  stdio: "ignore",
  windowsHide: true,
  detached: true,
});
child.unref();

child.on("error", (error) => {
  console.error(`Unable to open the Electron desktop: ${error.message}`);
  console.error("Run npm run desktop:build, then try again.");
  process.exitCode = 1;
});

