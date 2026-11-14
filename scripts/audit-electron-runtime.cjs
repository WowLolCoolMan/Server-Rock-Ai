const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const packageJson = JSON.parse(fs.readFileSync(path.join(root, "package.json"), "utf8"));
const desktopPackage = JSON.parse(fs.readFileSync(path.join(root, "apps", "desktop", "package.json"), "utf8"));
const main = fs.readFileSync(path.join(root, "apps", "desktop", "electron", "main.cjs"), "utf8");
const preload = fs.readFileSync(path.join(root, "apps", "desktop", "electron", "preload.cjs"), "utf8");
const petWindow = fs.readFileSync(path.join(root, "apps", "desktop", "src", "components", "overlays", "PetWindow.tsx"), "utf8");
const modelPicker = fs.readFileSync(path.join(root, "apps", "desktop", "src", "components", "ui", "ModelPicker.tsx"), "utf8");
const overflowMenu = fs.readFileSync(path.join(root, "apps", "desktop", "src", "components", "ui", "OverflowMenu.tsx"), "utf8");
const sidebar = fs.readFileSync(path.join(root, "apps", "desktop", "src", "components", "layout", "Sidebar.tsx"), "utf8");
const chatComposer = fs.readFileSync(path.join(root, "apps", "desktop", "src", "features", "chat", "ChatComposer.tsx"), "utf8");
const chatWorkspace = fs.readFileSync(path.join(root, "apps", "desktop", "src", "features", "chat", "useChatWorkspace.ts"), "utf8");
const navigation = fs.readFileSync(path.join(root, "apps", "desktop", "src", "app", "navigation.ts"), "utf8");
const packageScript = fs.readFileSync(path.join(root, "scripts", "package-electron.cjs"), "utf8");
const smokeScript = fs.readFileSync(path.join(root, "scripts", "smoke-electron-artifact.cjs"), "utf8");
const themeSmokeScript = fs.readFileSync(path.join(root, "scripts", "smoke-electron-themes.cjs"), "utf8");

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
expect(main.includes('"workspace.importDroppedFiles"') && main.includes("decodeDroppedAttachment"), "Electron bridge must validate dropped chat files at the local boundary");
expect(chatComposer.includes("onDrop={handleDrop}") && chatComposer.includes("Drop files to attach"), "Chat composer must expose a visible drag-and-drop attachment state");
expect(chatComposer.includes("aria-activedescendant={commandQuery !== null ? activeCommandId : undefined}") && chatComposer.includes("(index + 1) % commandMatches.length"), "Chat composer commands must expose the active option and wrap keyboard navigation");
expect(modelPicker.includes("const onPopoverKeyDown") && modelPicker.includes("aria-activedescendant={filtered[activeIndex]"), "Model picker must keep keyboard focus inside the popover and expose the active option");
expect(overflowMenu.includes('event.key === "Tab"') && sidebar.includes('event.key === "Tab"'), "Overflow and recent-chat menus must close when keyboard focus leaves with Tab");
expect(chatWorkspace.includes("importDroppedFiles") && chatWorkspace.includes("workspace.importDroppedFiles"), "Chat workspace must route dropped files through the guarded Electron method");
expect(navigation.includes("NavigationState") && navigation.includes("HISTORY_KEY"), "Renderer navigation must keep typed app history state");
expect(navigation.includes('window.location.protocol === "file:"') && navigation.includes("#/${route}"), "Electron navigation must use reload-safe hash routes for file URLs");
expect(navigation.includes("window.history.replaceState") && navigation.includes('window.history[replace ? "replaceState" : "pushState"]'), "Renderer navigation must preserve browser-like back/forward history");
for (const method of ["scheduled.create", "scheduled.pause", "scheduled.resume", "scheduled.cancel", "scheduled.runNow"]) {
  expect(main.includes(`"${method}"`), `Electron bridge must expose the local scheduled method ${method}`);
}
expect(main.includes("HOST_TIMEOUT"), "Electron bridge must bound pending host requests");
expect(main.includes("taskkill") && main.includes("/T"), "Electron shutdown must terminate the full Windows host process tree");
expect(main.includes("restartAgentHost"), "Electron must restart a timed-out host after resolving pending requests");
expect(main.includes("timeoutSeconds: chatHostDeadlineSeconds"), "Electron must give chat requests a host-side deadline before the renderer deadline");
expect(main.includes("function clampPetPosition") && main.includes("petWindow.setPosition(position.x, position.y)"), "Percy drag movement must remain inside the active monitor work area");
expect(main.includes("function movePetWithCursor") && main.includes("setInterval(movePetWithCursor, 16)"), "Percy desktop dragging must continue while the pointer leaves the companion window");
expect(main.includes("setVisibleOnAllWorkspaces(true") && main.includes("transparent: true") && main.includes("alwaysOnTop: true"), "Percy must remain a transparent always-on-top desktop companion across workspaces");
expect(main.includes('"tasks.retry": "Retried the local task."'), "Percy must summarize successful task retries");
expect(main.includes('kind = "success"') && main.includes('kind: "error"'), "Percy completion events must distinguish successful and failed local work");
expect(main.includes("if (!task) return null"), "Percy completion bubbles must stay scoped to tracked agent work");
expect(petWindow.includes("percy-error-frames.png") && petWindow.includes("event.kind"), "Percy must render a matching error sprite and completion state");
expect(petWindow.includes("window.addEventListener(\"blur\", cancelDrag)") && petWindow.includes("event.target instanceof Element"), "Percy renderer must cancel lost drags and protect its hide control from drag initiation");
expect(main.includes("packagedRuntime") && main.includes("process.resourcesPath"), "Electron must resolve packaged runtime paths");
expect(main.includes("packagedHost") && main.includes("ServerRock.AgentHost"), "Electron must launch a bundled Agent Host when packaged");
expect(packageScript.includes("dotnet\", [\"publish\"") && packageScript.includes("PublishSingleFile=true"), "Electron packaging must publish a self-contained Agent Host");
expect(packageScript.includes("electronDist") && packageScript.includes("const appRoot = path.join(outputRoot, \"resources\", \"app\")"), "Electron packaging must stage the renderer and runtime files");
expect(smokeScript.includes("remote-debugging-port") && smokeScript.includes("mainTarget") && smokeScript.includes("petTarget") && smokeScript.includes("Primary navigation") && smokeScript.includes("Percy desktop companion") && smokeScript.includes("pet-window-sprite"), "Electron packaged smoke must inspect the main navigation, Settings, and separate Percy surfaces");
expect(packageJson.scripts["smoke:electron:themes"] === "node scripts/smoke-electron-themes.cjs" && themeSmokeScript.includes("Emulation.setDeviceMetricsOverride") && themeSmokeScript.includes("Page.captureScreenshot") && themeSmokeScript.includes("deep-space"), "Packaged Electron theme acceptance must exercise constrained rendering and capture appearance states");

if (failures.length) {
  console.error("Electron replacement audit failed:");
  for (const failure of failures) console.error(`- ${failure}`);
  process.exitCode = 1;
} else {
  console.log("Electron replacement audit passed: default commands, secure boundary, and rollback aliases are present.");
}
