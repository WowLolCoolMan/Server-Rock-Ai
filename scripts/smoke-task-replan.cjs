const fs = require("node:fs");
const path = require("node:path");
const { spawn } = require("node:child_process");

const defaultName = process.platform === "win32" ? "Skai Desktop.exe" : "Skai Desktop";
const defaultRoot = path.join(__dirname, "..", "dist", "electron", `${process.platform === "win32" ? "win" : process.platform}-x64`);
const executable = path.resolve(process.argv[2] || path.join(defaultRoot, defaultName));
const port = 19322;
const timeoutMs = 20_000;
const prefillKey = "skai.chat.taskPrefill";

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
    const timer = setTimeout(() => reject(new Error("DevTools evaluation timed out.")), 5_000);
    socket.addEventListener("message", (event) => {
      const message = JSON.parse(typeof event.data === "string" ? event.data : String(event.data));
      if (message.id !== id) return;
      clearTimeout(timer);
      if (message.error) reject(new Error(message.error.message));
      else resolve(message.result?.result?.value);
      socket.close();
    });
    socket.send(JSON.stringify({ id, method: "Runtime.evaluate", params: { expression, returnByValue: true } }));
  });
  return result;
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
  if (!target) throw new Error(`Packaged Electron did not expose the main page before timeout. Output: ${output.slice(-2_000)}`);

  const staged = await evaluate(target, `localStorage.setItem(${JSON.stringify(prefillKey)}, JSON.stringify({ title: "Recover local task", instruction: "Review the failed result and produce a verified local plan.", environment: "local" })); location.hash = "#/chat"; "staged"`);
  if (staged !== "staged") throw new Error(`Could not stage the task handoff: ${JSON.stringify(staged)}`);

  let state;
  const renderDeadline = Date.now() + 8_000;
  while (Date.now() < renderDeadline) {
    await sleep(250);
    const raw = await evaluate(target, `JSON.stringify({ hash: location.hash, title: document.querySelector("#chat-task-composer-fields input")?.value || "", instruction: document.querySelector("#chat-task-composer-fields textarea")?.value || "", open: Boolean(document.querySelector("#chat-task-composer-fields")), consumed: localStorage.getItem(${JSON.stringify(prefillKey)}) === null })`);
    if (typeof raw === "string") state = JSON.parse(raw);
    if (state?.hash === "#/chat" && state.open && state.title && state.instruction && state.consumed) break;
  }
  if (!state?.open || state.title !== "Recover local task" || !state.instruction.includes("verified local plan") || !state.consumed) {
    throw new Error(`Task re-plan handoff did not render as expected: ${JSON.stringify(state)}`);
  }
  console.log(`Packaged task re-plan smoke passed: ${JSON.stringify({ route: state.hash, title: state.title, composerOpen: state.open, prefillConsumed: state.consumed })}`);
}

main().catch((error) => { console.error(error.message); process.exitCode = 1; }).finally(() => {
  if (!child.killed) child.kill();
});
