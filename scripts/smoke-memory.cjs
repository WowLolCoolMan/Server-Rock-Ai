#!/usr/bin/env node

/* Exercises explicit local memory against a disposable store. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-memory-smoke-"));
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env: { ...process.env, SRAI_MEMORY_PATH: path.join(tempRoot, "memory.json") },
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});
const reader = readline.createInterface({ input: child.stdout });
const pending = new Map();
let nextId = 1;
let stderr = "";
child.stderr.on("data", chunk => { stderr += chunk.toString(); });
reader.on("line", line => { try { const response = JSON.parse(line); const resolve = pending.get(response.id); if (resolve) { pending.delete(response.id); resolve(response); } } catch { /* timeout reports malformed output */ } });
function request(method, params) {
  const id = `memory-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => { const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000); pending.set(id, response => { clearTimeout(timer); resolve(response); }); });
}

async function run() {
  const saved = await request("memory.save", { content: "User prefers local-first workflows.", scope: "global", source: "user", approvalId: "memory-smoke", tags: ["preference"] });
  if (!saved.ok || !saved.result?.id) throw new Error("memory.save did not create an entry.");
  const searched = await request("memory.search", { query: "local-first" });
  if (!searched.ok || searched.result.length !== 1) throw new Error("memory.search did not find the saved entry.");
  const disabled = await request("memory.enabled", { id: saved.result.id, enabled: false });
  if (!disabled.ok || disabled.result?.enabled !== false) throw new Error("memory.enabled did not disable the entry.");
  const hidden = await request("memory.search", { query: "local-first" });
  if (!hidden.ok || hidden.result.length !== 0) throw new Error("disabled memory remained in enabled-only search.");
  const exported = await request("memory.export");
  if (!exported.ok || !String(exported.result?.json).includes("local-first")) throw new Error("memory.export omitted the saved entry.");
  const removed = await request("memory.remove", { id: saved.result.id });
  if (!removed.ok || removed.result?.removed !== true) throw new Error("memory.remove did not remove the entry.");
  console.log("Memory smoke passed: explicit save, search, disable, export, and removal.");
}
run().catch(error => { console.error(error.message); if (stderr.trim()) console.error(stderr.trim()); process.exitCode = 1; }).finally(() => { child.kill(); reader.close(); fs.rmSync(tempRoot, { recursive: true, force: true }); });
