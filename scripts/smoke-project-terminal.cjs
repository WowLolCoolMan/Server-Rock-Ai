#!/usr/bin/env node

/* Exercises one explicitly approved command in a disposable project root. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-project-terminal-smoke-"));
const workspace = path.join(tempRoot, "workspace");
fs.mkdirSync(workspace, { recursive: true });
const command = process.platform === "win32" ? "Write-Output terminal-smoke" : "printf terminal-smoke";
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env: { ...process.env, SRAI_PROJECTS_PATH: path.join(tempRoot, "projects.json") },
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
  const id = `terminal-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const saved = await request("project.save", { name: "Terminal smoke", folderPaths: [workspace] });
  if (!saved.ok || typeof saved.result?.id !== "string") throw new Error("project.save did not create the disposable terminal project.");
  const result = await request("project.command.run", { id: saved.result.id, command, relativeWorkingDirectory: ".", timeoutSeconds: 5 });
  if (!result.ok || result.result?.exitCode !== 0 || result.result?.timedOut || !String(result.result?.output).includes("terminal-smoke"))
    throw new Error("project.command.run did not return the bounded command result.");
  console.log("Project terminal smoke passed: disposable root, explicit command, bounded result, and output round trip.");
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
