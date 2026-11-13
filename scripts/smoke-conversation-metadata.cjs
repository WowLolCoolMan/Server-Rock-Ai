#!/usr/bin/env node

/* Exercises bounded local conversation organization in disposable history. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-conversation-metadata-smoke-"));
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
  const id = `conversation-metadata-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const saved = await request("conversation.save", { title: "Organized chat", messages: [{ role: "user", content: "metadata fixture" }] });
  if (!saved.ok || typeof saved.result?.id !== "string") throw new Error("conversation.save did not create the disposable metadata fixture.");
  const updated = await request("conversation.metadata.save", { id: saved.result.id, pinned: true, folderName: "Research", tags: ["local", "review"] });
  if (!updated.ok || updated.result?.pinned !== true || updated.result?.folderName !== "Research" || updated.result?.tags?.length !== 2) throw new Error("conversation.metadata.save did not persist bounded organization metadata.");
  const listed = await request("conversations.list");
  if (!listed.ok || listed.result.length !== 1 || listed.result[0].pinned !== true || listed.result[0].tags[0] !== "local") throw new Error("conversation metadata did not round-trip through conversations.list.");
  const archived = await request("conversation.metadata.save", { id: saved.result.id, archived: true });
  if (!archived.ok || archived.result?.archived !== true) throw new Error("conversation archive state did not persist.");
  const hidden = await request("conversations.list");
  if (!hidden.ok || hidden.result.length !== 0) throw new Error("archived conversations were not hidden from the active list.");
  const restoredView = await request("conversations.list", { includeArchived: true });
  if (!restoredView.ok || restoredView.result.length !== 1 || restoredView.result[0].archived !== true) throw new Error("archived conversation view did not return the archived item.");
  const exportResult = await request("conversations.export", { includeArchived: true });
  if (!exportResult.ok || exportResult.result?.conversationCount !== 1 || !String(exportResult.result?.markdown).includes("Organized chat")) throw new Error("conversation Markdown export did not include the archived fixture.");
  const forked = await request("conversation.fork", { id: saved.result.id, throughIndex: 0 });
  if (!forked.ok || forked.result?.forkedFrom !== saved.result.id || forked.result?.messageCount !== 1) throw new Error("conversation fork did not create the expected message prefix.");
  const forkList = await request("conversations.list", { includeArchived: true });
  if (!forkList.ok || forkList.result.length !== 2 || !forkList.result.some(item => item.id === forked.result.id)) throw new Error("forked conversation did not persist in history.");
  console.log("Conversation metadata smoke passed: labels, archive, active filtering, archived view, Markdown export, and fork.");
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
