const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const packageJson = JSON.parse(fs.readFileSync(path.join(root, "package.json"), "utf8"));
const desktopPackage = JSON.parse(fs.readFileSync(path.join(root, "apps", "desktop", "package.json"), "utf8"));
const main = fs.readFileSync(path.join(root, "apps", "desktop", "electron", "main.cjs"), "utf8");
const preload = fs.readFileSync(path.join(root, "apps", "desktop", "electron", "preload.cjs"), "utf8");
const packageScript = fs.readFileSync(path.join(root, "scripts", "package-electron.cjs"), "utf8");
const smokeScript = fs.readFileSync(path.join(root, "scripts", "smoke-electron-artifact.cjs"), "utf8");

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };
expect(packageJson.scripts.start === "node scripts/start-desktop.cjs", "root start must launch Electron");
expect(packageJson.scripts.build === "npm run desktop:build", "root build must build Electron");
expect(packageJson.scripts.open === "node scripts/open-desktop.cjs", "root open must launch Electron");
expect(packageJson.scripts["start:legacy-native"] === "node scripts/start-native.cjs", "legacy start alias must remain explicit");
expect(packageJson.scripts["open:legacy-native"] === "node scripts/open-native.cjs", "legacy open alias must remain explicit");
expect(desktopPackage.main === "electron/main.cjs", "desktop package must use Electron main");
expect(main.includes("contextIsolation: true"), "context isolation must remain enabled");
expect(main.includes("nodeIntegration: false"), "Node integration must remain disabled");
expect(main.includes("sandbox: true"), "renderer sandbox must remain enabled");
expect(preload.includes("contextBridge.exposeInMainWorld"), "renderer communication must use preload");
expect(!preload.includes("require(\"node:fs\")"), "preload must not expose filesystem access");
expect(main.includes('"tasks.plan"'), "Electron bridge must expose the review-first local task planning method");
expect(main.includes("HOST_TIMEOUT"), "Electron bridge must bound pending host requests");
expect(main.includes("taskkill") && main.includes("/T"), "Electron shutdown must terminate the full Windows host process tree");
expect(main.includes("restartAgentHost"), "Electron must restart a timed-out host after resolving pending requests");
expect(main.includes("timeoutSeconds: chatHostDeadlineSeconds"), "Electron must give chat requests a host-side deadline before the renderer deadline");
expect(main.includes("packagedRuntime") && main.includes("process.resourcesPath"), "Electron must resolve packaged runtime paths");
expect(main.includes("packagedHost") && main.includes("ServerRock.AgentHost"), "Electron must launch a bundled Agent Host when packaged");
expect(packageScript.includes("dotnet\", [\"publish\"") && packageScript.includes("PublishSingleFile=true"), "Electron packaging must publish a self-contained Agent Host");
expect(packageScript.includes("electronDist") && packageScript.includes("const appRoot = path.join(outputRoot, \"resources\", \"app\")"), "Electron packaging must stage the renderer and runtime files");
expect(smokeScript.includes("remote-debugging-port") && smokeScript.includes("Settings") && smokeScript.includes("Skai pet"), "Electron packaged smoke must inspect the rendered settings and pet surfaces");

if (failures.length) {
  console.error("Electron replacement audit failed:");
  for (const failure of failures) console.error(`- ${failure}`);
  process.exitCode = 1;
} else {
  console.log("Electron replacement audit passed: default commands, secure boundary, and rollback aliases are present.");
}
