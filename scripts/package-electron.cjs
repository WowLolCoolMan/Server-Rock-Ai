const fs = require("node:fs");
const path = require("node:path");
const { spawnSync } = require("node:child_process");

const root = path.resolve(__dirname, "..");
const desktopRoot = path.join(root, "apps", "desktop");
const electronDist = path.join(desktopRoot, "node_modules", "electron", "dist");
const outputRoot = path.join(root, "dist", "electron", `${process.platform === "win32" ? "win" : process.platform}-x64`);
const appRoot = path.join(outputRoot, "resources", "app");
const hostRoot = path.join(outputRoot, "resources", "agent-host");
const runtime = process.platform === "win32" ? "win-x64" : process.platform === "linux" ? "linux-x64" : "osx-x64";
const electronBinary = process.platform === "win32" ? "electron.exe" : "electron";
const packagedBinary = process.platform === "win32" ? "Skai Desktop.exe" : "Skai Desktop";
const hostBinary = process.platform === "win32" ? "ServerRock.AgentHost.exe" : "ServerRock.AgentHost";

function run(command, args) {
  const result = spawnSync(command, args, { cwd: root, stdio: "inherit", shell: false });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${command} exited with status ${result.status}.`);
}

if (!fs.existsSync(electronDist)) throw new Error("Electron dependencies are not installed. Run npm run desktop:install first.");
if (!fs.existsSync(path.join(desktopRoot, "dist", "index.html"))) throw new Error("Electron renderer is not built. Run npm run desktop:build first.");

fs.rmSync(outputRoot, { recursive: true, force: true });
fs.mkdirSync(appRoot, { recursive: true });
fs.mkdirSync(hostRoot, { recursive: true });

run("dotnet", ["publish", path.join("services", "agent-host", "ServerRock.AgentHost.csproj"), "-c", "Release", "-r", runtime, "--self-contained", "true", "-p:PublishSingleFile=true", "-p:PublishTrimmed=false", "-o", hostRoot]);

fs.cpSync(electronDist, outputRoot, { recursive: true, force: true });
fs.cpSync(path.join(desktopRoot, "dist"), path.join(appRoot, "dist"), { recursive: true, force: true });
fs.cpSync(path.join(desktopRoot, "electron"), path.join(appRoot, "electron"), { recursive: true, force: true });
fs.cpSync(path.join(desktopRoot, "package.json"), path.join(appRoot, "package.json"), { force: true });
fs.renameSync(path.join(outputRoot, electronBinary), path.join(outputRoot, packagedBinary));

const publishedHost = path.join(hostRoot, hostBinary);
if (!fs.existsSync(publishedHost)) throw new Error(`The self-contained Agent Host was not published at ${publishedHost}.`);
fs.writeFileSync(path.join(outputRoot, "README-release.txt"), [
  "Skai Desktop portable Electron artifact",
  "",
  `Run ${packagedBinary} from this folder. The bundled Agent Host is in resources/agent-host.`,
  "This portable artifact is for release validation; signed installers require the release signing gate.",
  "",
].join("\n"), "utf8");

console.log(`Electron artifact staged: ${outputRoot}`);
