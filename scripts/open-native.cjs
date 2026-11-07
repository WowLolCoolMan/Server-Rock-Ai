const { spawn } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const isWindows = process.platform === "win32";
const rid = isWindows ? "win-x64" : "linux-x64";
const executableName = isWindows ? "ServerRock.Native.exe" : "ServerRock.Native";
const candidates = [
  path.join(root, "dist", "native", rid, executableName),
  path.join(root, "native", "ServerRock.Native", "bin", "Release", "net10.0-desktop", rid, "publish", executableName),
  path.join(root, "native", "ServerRock.Native", "bin", "Release", isWindows ? "net10.0-windows10.0.26100" : "net10.0-desktop", "win-x64", "publish", executableName),
];
const packaged = candidates.find((candidate) => fs.existsSync(candidate));

const child = packaged
  ? spawn(packaged, process.argv.slice(2), { cwd: path.dirname(packaged), env: process.env, stdio: "inherit", windowsHide: false })
  : spawn("dotnet", ["run", "--project", path.join(root, "native", "ServerRock.Native", "ServerRock.Native.csproj"), "--framework", process.env.SERVER_ROCK_NATIVE_FRAMEWORK || "net10.0-desktop", "--", ...process.argv.slice(2)], { cwd: root, env: process.env, stdio: "inherit", windowsHide: false });

child.on("error", (error) => {
  console.error(`Unable to open Server Rock AI: ${error.message}`);
  console.error(packaged ? "Rebuild with npm run build:native, then try again." : "Install the .NET 10 SDK and confirm that dotnet is on PATH.");
  process.exitCode = 1;
});
child.on("exit", (code, signal) => {
  if (signal) process.kill(process.pid, signal);
  else process.exitCode = code ?? 1;
});
