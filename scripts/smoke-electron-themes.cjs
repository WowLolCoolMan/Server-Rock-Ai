const fs = require("node:fs");
const path = require("node:path");
const { spawn } = require("node:child_process");

const defaultName = process.platform === "win32" ? "Skai Desktop.exe" : "Skai Desktop";
const defaultRoot = path.join(__dirname, "..", "dist", "electron", `${process.platform === "win32" ? "win" : "linux"}-x64`);
const executable = path.resolve(process.argv[2] || path.join(defaultRoot, defaultName));
const port = 19323;
const timeoutMs = 25_000;
const outputDir = path.join(__dirname, "..", ".tmp", "theme-acceptance");

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

function connect(target) {
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  const pending = new Map();
  let nextId = 1;
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(typeof event.data === "string" ? event.data : String(event.data));
    const resolve = pending.get(message.id);
    if (typeof resolve !== "function") return;
    pending.delete(message.id);
    resolve(message);
  });
  const send = async (method, params = {}) => {
    const id = nextId++;
    const result = new Promise((resolve, reject) => {
      pending.set(id, (message) => message.error ? reject(new Error(message.error.message)) : resolve(message.result));
      setTimeout(() => { if (pending.delete(id)) reject(new Error(`DevTools command timed out: ${method}`)); }, 5_000);
    });
    socket.send(JSON.stringify({ id, method, params }));
    return result;
  };
  const evaluate = async (expression) => {
    const result = await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
    return result?.result?.value;
  };
  const callFunction = async (functionDeclaration, args = []) => {
    const windowResult = await send("Runtime.evaluate", { expression: "window", returnByValue: false });
    const objectId = windowResult?.result?.objectId;
    if (!objectId) throw new Error("DevTools did not return a page object for the probe.");
    try {
      const result = await send("Runtime.callFunctionOn", {
        objectId,
        functionDeclaration,
        arguments: args.map((value) => ({ value })),
        returnByValue: true,
        awaitPromise: true,
      });
      if (result?.exceptionDetails) throw new Error("The page probe failed.");
      return result?.result?.value;
    } finally {
      await send("Runtime.releaseObject", { objectId }).catch(() => {});
    }
  };
  const close = () => socket.close();
  return { open: new Promise((resolve, reject) => { socket.addEventListener("open", resolve, { once: true }); socket.addEventListener("error", reject, { once: true }); }), send, evaluate, callFunction, close };
}

async function waitFor(evaluate, expression, predicate, label) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const value = await evaluate(expression);
    if (predicate(value)) return value;
    await sleep(150);
  }
  throw new Error(`Timed out waiting for ${label}`);
}

async function main() {
  const deadline = Date.now() + timeoutMs;
  let target;
  while (Date.now() < deadline) {
    try {
      const targets = await fetchJson(`http://127.0.0.1:${port}/json/list`);
      target = targets.find((item) => item.type === "page" && item.title?.includes("Skai Desktop") && !item.url?.includes("?pet=1"));
      if (target) break;
    } catch { /* The packaged window is still starting. */ }
    await sleep(250);
  }
  if (!target) throw new Error(`Packaged Electron did not expose the main page. Output: ${output.slice(-2_000)}`);

  const devtools = connect(target);
  await devtools.open;
  await devtools.evaluate("document.querySelector('.sidebar-settings')?.click(); 'settings'");
  await waitFor(devtools.evaluate, "document.querySelectorAll('.settings-content select').length", (value) => value >= 2, "Settings controls");
  await devtools.evaluate("[...document.querySelectorAll('.settings-nav button')].find((button) => button.textContent?.trim() === 'Appearance')?.click(); 'appearance'");
  await waitFor(devtools.evaluate, "document.querySelector('.settings-content select')?.value", (value) => typeof value === "string", "Appearance controls");

  const setAppearance = async (theme, backdrop) => {
    const selected = await devtools.callFunction("function (theme, backdrop) { const labels = [...document.querySelectorAll('.settings-content label')]; const set = (labelText, value) => { const select = labels.find((label) => label.textContent?.includes(labelText))?.querySelector('select'); if (!select) return false; select.value = value; select.dispatchEvent(new Event('change', { bubbles: true })); return true; }; return { theme: set('Color theme', theme), backdrop: set('Backdrop', backdrop) } }", [theme, backdrop]);
    if (!selected?.theme || !selected?.backdrop) throw new Error(`Appearance controls were not found for ${theme}/${backdrop}`);
    const state = await waitFor(devtools.evaluate, "JSON.stringify((() => { const root = document.documentElement; const styles = getComputedStyle(root); return { theme: root.dataset.theme, backdrop: root.dataset.backdrop, canvas: styles.getPropertyValue('--skai-color-canvas').trim(), accent: styles.getPropertyValue('--skai-color-accent').trim(), overflow: Math.max(root.scrollWidth, document.body.scrollWidth) <= window.innerWidth + 1, width: window.innerWidth }; })())", (value) => { try { const parsed = JSON.parse(value); return parsed.theme === theme && parsed.backdrop === backdrop; } catch { return false; } }, `${theme}/${backdrop} state`);
    const parsed = JSON.parse(state);
    if (!parsed.overflow || !parsed.canvas || !parsed.accent) throw new Error(`Appearance state overflowed or lacked tokens: ${state}`);
    fs.mkdirSync(outputDir, { recursive: true });
    const screenshot = await devtools.send("Page.captureScreenshot", { format: "png" });
    fs.writeFileSync(path.join(outputDir, `${theme}-${backdrop}.png`), Buffer.from(screenshot.data, "base64"));
    return parsed;
  };

  const states = [];
  for (const [theme, backdrop] of [["dark", "none"], ["light", "deep-sea"], ["system", "deep-space"]]) states.push(await setAppearance(theme, backdrop));
  await devtools.send("Emulation.setDeviceMetricsOverride", { width: 960, height: 640, deviceScaleFactor: 1, mobile: false });
  const constrained = await setAppearance("light", "deep-space");
  await devtools.send("Emulation.clearDeviceMetricsOverride");
  devtools.close();
  console.log(`Packaged theme acceptance passed: ${JSON.stringify({ states, constrained })}`);
}

main().catch((error) => { console.error(error.message); process.exitCode = 1; }).finally(() => {
  if (!child.killed) child.kill();
});
