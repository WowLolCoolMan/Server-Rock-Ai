#!/usr/bin/env node

/* Exercises bounded local conversation search against disposable history. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-conversation-search-smoke-"));
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env: { ...process.env, SRAI_CONVERSATIONS_PATH: path.join(tempRoot, "conversations.json") },
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
  const id = `conversation-search-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const first = await request("conversation.save", { title: "Needle discussion", messages: [{ role: "user", content: "Keep this needle in local history." }] });
  const second = await request("conversation.save", { title: "Other discussion", messages: [{ role: "user", content: "No matching term here." }] });
  if (!first.ok || !second.ok) throw new Error("conversation.save did not create the disposable search fixtures.");
  const search = await request("conversations.search", { query: "needle" });
  if (!search.ok || search.result.length !== 1 || search.result[0].title !== "Needle discussion" || !String(search.result[0].display).includes("Needle"))
    throw new Error("conversations.search did not return the bounded matching conversation.");
  console.log("Conversation search smoke passed: persisted message match and bounded display result.");
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
