#!/usr/bin/env node

/* Exercises durable bounded chat queue operations in a disposable store. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-chat-queue-smoke-"));
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env: { ...process.env, SRAI_CHAT_QUEUE_PATH: path.join(tempRoot, "queue.json") },
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});
const reader = readline.createInterface({ input: child.stdout });
const pending = new Map();
let nextId = 1;
let stderr = "";
child.stderr.on("data", chunk => { stderr += chunk.toString(); });
reader.on("line", line => {
  try {
    const response = JSON.parse(line);
    const resolve = pending.get(response.id);
    if (resolve) { pending.delete(response.id); resolve(response); }
  } catch { /* timeout reports malformed host output */ }
});

function request(method, params) {
  const id = `chat-queue-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const before = await request("chat.queue.list");
  if (!before.ok || before.result.length !== 0) throw new Error("chat.queue.list was not empty in the disposable store.");
  const first = await request("chat.queue.enqueue", { text: "first queued message" });
  const second = await request("chat.queue.enqueue", { text: "second queued message" });
  if (!first.ok || !second.ok || second.result.length !== 2) throw new Error("chat.queue.enqueue did not preserve FIFO messages.");
  const removed = await request("chat.queue.remove", { index: 0 });
  if (!removed.ok || removed.result.length !== 1 || removed.result[0].text !== "second queued message") throw new Error("chat.queue.remove did not remove the selected queue item.");
  console.log("Chat queue smoke passed: disposable persistence, FIFO enqueue, and indexed removal.");
}

run().catch(error => {
  console.error(error.message);
  if (stderr.trim()) console.error(stderr.trim());
  process.exitCode = 1;
}).finally(() => {
  child.kill();
  reader.close();
  fs.rmSync(tempRoot, { recursive: true, force: true });
});
