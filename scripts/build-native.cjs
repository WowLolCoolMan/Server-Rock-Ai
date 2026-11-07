const { spawnSync } = require("node:child_process");
const path = require("node:path");
const fs = require("node:fs");

const root = path.resolve(__dirname, "..");
const isWindows = process.platform === "win32";
const rid = isWindows ? "win-x64" : "linux-x64";
const output = path.join(root, "dist", "native", rid);
const project = path.join(root, "native", "ServerRock.Native", "ServerRock.Native.csproj");
fs.mkdirSync(output, { recursive: true });

const args = ["publish", project, "-c", "Release", "-f", "net10.0-desktop", "-r", rid, "-p:SelfContained=true", "-o", output];
const result = spawnSync("dotnet", args, { cwd: root, env: process.env, stdio: "inherit", shell: false });
if (result.error) {
  console.error(`Unable to build the native app: ${result.error.message}`);
  process.exit(1);
}
process.exit(result.status ?? 1);
