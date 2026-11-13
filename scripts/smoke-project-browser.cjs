#!/usr/bin/env node

/* Exercises only disposable project and workspace files. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-project-browser-smoke-"));
const workspace = path.join(tempRoot, "workspace");
fs.mkdirSync(path.join(workspace, "node_modules"), { recursive: true });
fs.writeFileSync(path.join(workspace, "README.md"), "safe metadata fixture\n");
fs.writeFileSync(path.join(workspace, ".env"), "SECRET=must-not-appear\n");
fs.writeFileSync(path.join(workspace, "node_modules", "ignored.js"), "ignored\n");

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
  const id = `browser-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const saved = await request("project.save", { name: "Browser smoke", folderPaths: [workspace] });
  if (!saved.ok || typeof saved.result?.id !== "string") throw new Error("project.save did not create the disposable project.");
  const browser = await request("project.files", { id: saved.result.id });
  const files = browser.result?.files ?? [];
  if (!browser.ok || files.length !== 1 || files[0].relativePath !== "README.md") throw new Error("project.files did not preserve safe filtering.");
  if (browser.result.protectedFilesSkipped < 1 || browser.result.excludedDirectoriesSkipped < 1)
    throw new Error("project.files did not report protected/dependency exclusions.");
  if (Object.values(files[0]).some(value => String(value).includes("SECRET"))) throw new Error("project browser leaked protected content.");
  console.log("Project browser smoke passed: disposable project round trip, safe filtering, and exclusion summary.");
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
