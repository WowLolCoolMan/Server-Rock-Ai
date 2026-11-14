const fs = require("node:fs");
const path = require("node:path");
const { spawn } = require("node:child_process");

const defaultName = process.platform === "win32" ? "Skai Desktop.exe" : "Skai Desktop";
const defaultRoot = path.join(__dirname, "..", "dist", "electron", `${process.platform === "win32" ? "win" : process.platform}-x64`);
const executable = path.resolve(process.argv[2] || path.join(defaultRoot, defaultName));
const port = 19321;
const timeoutMs = 20_000;

if (!fs.existsSync(executable)) {
  console.error(`Electron artifact does not exist: ${executable}`);
  process.exit(1);
}

const child = spawn(executable, [`--remote-debugging-port=${port}`, "--disable-gpu"], { cwd: path.dirname(executable), windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
let output = "";
child.stdout.on("data", (chunk) => { output += chunk.toString(); });
child.stderr.on("data", (chunk) => { output += chunk.toString(); });

const sleep = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds));
async function fetchJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`HTTP ${response.status} from ${url}`);
  return response.json();
}

async function evaluate(target, expression) {
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => { socket.addEventListener("open", resolve, { once: true }); socket.addEventListener("error", reject, { once: true }); });
  const result = await new Promise((resolve, reject) => {
    const id = 1;
    const seen = [];
    const timer = setTimeout(() => reject(new Error(`DevTools evaluation timed out. Messages seen: ${seen.join(" | ")}`)), 5_000);
    socket.addEventListener("message", (event) => {
      const raw = typeof event.data === "string" ? event.data : String(event.data);
      const message = JSON.parse(raw);
      seen.push(`id=${String(message.id)} method=${String(message.method)} hasResult=${Boolean(message.result)}`);
      if (message.id !== id) return;
      clearTimeout(timer);
      if (message.error) reject(new Error(message.error.message));
      else if (!message.result?.result) reject(new Error(`DevTools evaluation returned no result: ${JSON.stringify(message)}`));
      else resolve(message.result.result.value);
      socket.close();
    });
    socket.send(JSON.stringify({ id, method: "Runtime.evaluate", params: { expression, returnByValue: true } }));
  });
  return result;
}

async function main() {
  const deadline = Date.now() + timeoutMs;
  let mainTarget;
  let petTarget;
  while (Date.now() < deadline) {
    try {
      const targets = await fetchJson(`http://127.0.0.1:${port}/json/list`);
      const pages = targets.filter((item) => item.type === "page");
      mainTarget = pages.find((item) => item.title?.includes("Skai Desktop") && !item.url?.includes("?pet=1"));
      petTarget = pages.find((item) => item.title?.includes("Percy") || item.url?.includes("?pet=1"));
      if (mainTarget && petTarget) break;
    } catch { /* The packaged window is still starting. */ }
    await sleep(250);
  }
  if (!mainTarget || !petTarget) throw new Error(`Packaged Electron did not expose both main and Percy pages before timeout. Output: ${output.slice(-2_000)}`);
  console.log(`Packaged main target: ${JSON.stringify({ title: mainTarget.title, url: mainTarget.url, type: mainTarget.type, websocket: Boolean(mainTarget.webSocketDebuggerUrl) })}`);
  console.log(`Packaged Percy target: ${JSON.stringify({ title: petTarget.title, url: petTarget.url, type: petTarget.type, websocket: Boolean(petTarget.webSocketDebuggerUrl) })}`);
  const mainResult = await evaluate(mainTarget, "JSON.stringify({ title: document.title, settings: document.body.innerText.includes('Settings'), navigation: Boolean(document.querySelector('[aria-label=\\\"Primary navigation\\\"]')) })");
  const petResult = await evaluate(petTarget, "JSON.stringify((() => { const root = document.querySelector('[aria-label=\\\"Percy desktop companion. Drag to move.\\\"]'); const sprite = document.querySelector('.pet-window-sprite'); const style = sprite ? getComputedStyle(sprite) : null; return { title: document.title, companion: Boolean(root), sprite: Boolean(sprite), transparent: getComputedStyle(document.body).backgroundColor === 'rgba(0, 0, 0, 0)', animated: Boolean(style?.animationName && style.animationName !== 'none') }; })())");
  if (typeof mainResult !== "string" || typeof petResult !== "string") throw new Error(`Packaged Electron returned an unexpected evaluation value: ${JSON.stringify({ mainResult, petResult })}`);
  const mainState = JSON.parse(mainResult);
  const petState = JSON.parse(petResult);
  if (mainTarget.id === petTarget.id || !petTarget.url?.includes("pet=1") || !mainState.title.includes("Skai Desktop") || !mainState.settings || !mainState.navigation || !petState.title.includes("Percy") || !petState.companion || !petState.sprite || !petState.transparent || !petState.animated) {
    throw new Error(`Packaged Electron rendered incomplete surfaces: ${JSON.stringify({ mainState, petState })}`);
  }
  console.log(`Packaged Electron smoke passed: ${mainState.title}; main and separate transparent Percy surfaces rendered with animated sprite.`);
}

main().catch((error) => { console.error(error.message); process.exitCode = 1; }).finally(() => {
  if (!child.killed) child.kill();
});
