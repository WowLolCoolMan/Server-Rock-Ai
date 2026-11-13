#!/usr/bin/env node

/* Exercises the bounded PDF extraction route against a disposable host. */
const os = require("node:os");
const fs = require("node:fs");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-pdf-smoke-"));
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env: { ...process.env, SRAI_SETTINGS_PATH: path.join(tempRoot, "settings.json") },
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
  const id = `pdf-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => { const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000); pending.set(id, response => { clearTimeout(timer); resolve(response); }); });
}

async function run() {
  const rejected = await request("workspace.attachment.extract", { fileName: "notes.pdf", content: Buffer.from("not a pdf").toString("base64") });
  if (rejected.ok || !String(rejected.error?.message).toLowerCase().includes("pdf")) throw new Error("Invalid PDF content was not rejected.");
  const blocked = await request("workspace.attachment.extract", { fileName: ".env.pdf", content: Buffer.from("not a pdf").toString("base64") });
  if (blocked.ok || !String(blocked.error?.message).toLowerCase().includes("approved")) throw new Error("Blocked PDF name was not rejected.");
  console.log("PDF attachment smoke passed: invalid content and protected names are rejected locally.");
}
run().catch(error => { console.error(error.message); if (stderr.trim()) console.error(stderr.trim()); process.exitCode = 1; }).finally(() => { child.kill(); reader.close(); fs.rmSync(tempRoot, { recursive: true, force: true }); });
