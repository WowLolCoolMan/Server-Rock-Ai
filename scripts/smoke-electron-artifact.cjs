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
      socket.send(JSON.stringify({ id, method: "Runtime.evaluate", params: { expression, returnByValue: true, awaitPromise: true } }));
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
      mainTarget = targets.find((item) => item.type === "page" && !item.url.includes("#pet"));
      petTarget = targets.find((item) => item.type === "page" && item.url.includes("#pet"));
      if (mainTarget?.title?.includes("Skai Desktop") && petTarget?.title?.includes("Skai Desktop")) break;
    } catch { /* The packaged window is still starting. */ }
    await sleep(250);
  }
  if (!mainTarget || !petTarget) throw new Error(`Packaged Electron did not expose both main and pet windows before timeout. Output: ${output.slice(-2_000)}`);
  console.log(`Packaged targets: ${JSON.stringify([mainTarget, petTarget].map((target) => ({ title: target.title, url: target.url, type: target.type, websocket: Boolean(target.webSocketDebuggerUrl) })))} `);
  const mainResult = await evaluate(mainTarget, "(async () => { const health = await window.serverRockAgent.request('runtime.health'); const settingsButton = [...document.querySelectorAll('button')].find((button) => button.textContent?.trim() === 'Settings'); settingsButton?.click(); await new Promise((resolve) => setTimeout(resolve, 100)); const petButton = [...document.querySelectorAll('button')].find((button) => button.textContent?.trim() === 'Pet'); petButton?.click(); await new Promise((resolve) => setTimeout(resolve, 100)); return JSON.stringify({ title: document.title, settings: document.body.innerText.includes('Settings'), petSettings: document.body.innerText.includes('Separate always-on-top window') && document.body.innerText.includes('Choose animation manifest'), credentialsBridge: health.ok && health.result.capabilities.includes('credentials.status') }); })()");
  const petResult = await evaluate(petTarget, "(async () => { const config = await window.serverRockAgent.pet.getConfig(); return JSON.stringify({ title: document.title, pet: Boolean(document.querySelector('[aria-label=\\\"Skai pet. Drag to move.\\\"]')), transparent: getComputedStyle(document.body).backgroundColor === 'rgba(0, 0, 0, 0)' || document.body.classList.contains('pet-window-document'), alwaysOnTop: config.alwaysOnTop === true, activity: config.activity }); })()");
  if (typeof mainResult !== "string" || typeof petResult !== "string") throw new Error(`Packaged Electron returned an unexpected evaluation value: ${JSON.stringify({ mainResult, petResult })}`);
  const mainState = JSON.parse(mainResult);
  const petState = JSON.parse(petResult);
  if (!mainState.title.includes("Skai Desktop") || !mainState.settings || !mainState.petSettings || !mainState.credentialsBridge || !petState.title.includes("Skai Desktop") || !petState.pet || !petState.transparent || !petState.alwaysOnTop) throw new Error(`Packaged Electron rendered incomplete main or pet windows: ${JSON.stringify({ mainState, petState })}`);
  console.log(`Packaged Electron smoke passed: main settings and credential bridge; separate transparent pet window rendered.`);
}

main().catch((error) => { console.error(error.message); process.exitCode = 1; }).finally(() => {
  if (!child.killed) child.kill();
});
