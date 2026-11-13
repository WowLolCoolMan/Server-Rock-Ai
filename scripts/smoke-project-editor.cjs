#!/usr/bin/env node

/* Exercises reviewed file editing and undo only in a disposable project. */
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-project-editor-smoke-"));
const workspace = path.join(tempRoot, "workspace");
fs.mkdirSync(workspace, { recursive: true });
fs.writeFileSync(path.join(workspace, "README.md"), "before edit\n");
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env: { ...process.env, SRAI_PROJECTS_PATH: path.join(tempRoot, "projects.json"), SRAI_UNDO_PATH: path.join(tempRoot, "undo.json") },
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
  const id = `editor-smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const saved = await request("project.save", { name: "Editor smoke", folderPaths: [workspace] });
  if (!saved.ok || typeof saved.result?.id !== "string") throw new Error("project.save did not create the disposable editor project.");
  const id = saved.result.id;
  const before = await request("project.file.read", { id, relativePath: "README.md" });
  if (!before.ok || before.result?.content !== "before edit\n" || typeof before.result?.hash !== "string") throw new Error("project.file.read did not return the bounded original and hash.");
  const written = await request("project.file.write", { id, relativePath: "README.md", content: "after edit\n", expectedHash: before.result.hash });
  if (!written.ok || typeof written.result?.undoId !== "string") throw new Error("project.file.write did not return an undo receipt.");
  const after = await request("project.file.read", { id, relativePath: "README.md" });
  if (!after.ok || after.result?.content !== "after edit\n") throw new Error("project.file.write did not persist the reviewed text.");
  const undone = await request("project.file.undo", { undoId: written.result.undoId });
  if (!undone.ok || undone.result?.undone !== true) throw new Error("project.file.undo did not complete.");
  const restored = await request("project.file.read", { id, relativePath: "README.md" });
  if (!restored.ok || restored.result?.content !== "before edit\n") throw new Error("project.file.undo did not restore the original text.");
  console.log("Project editor smoke passed: hash-checked write, readback, and reviewed undo.");
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
