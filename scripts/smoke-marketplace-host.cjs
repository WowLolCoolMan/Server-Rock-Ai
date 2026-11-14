#!/usr/bin/env node

/* Disposable marketplace protocol smoke. It validates the local catalog and
 * provider-backed response shape without downloading a model. */

const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const profileRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-marketplace-smoke-"));
const env = {
  ...process.env,
  SRAI_SETTINGS_PATH: path.join(profileRoot, "settings.json"),
  SRAI_CONVERSATIONS_PATH: path.join(profileRoot, "conversations.json"),
  SRAI_TASKS_PATH: path.join(profileRoot, "agent-tasks.json"),
  SRAI_SCHEDULED_TASKS_PATH: path.join(profileRoot, "scheduled-tasks.json"),
  SRAI_PROJECTS_PATH: path.join(profileRoot, "projects.json"),
  SRAI_CORPUS_PATH: path.join(profileRoot, "document-corpus.json"),
};
const hostOutput = path.join(root, "services", "agent-host", "bin", "Debug", "net10.0");
const hostProgram = process.platform === "win32" ? path.join(hostOutput, "ServerRock.AgentHost.exe") : "dotnet";
const hostArgs = process.platform === "win32" ? ["--stdio"] : [path.join(hostOutput, "ServerRock.AgentHost.dll"), "--stdio"];
const child = spawn(hostProgram, hostArgs, { cwd: root, env, stdio: ["pipe", "pipe", "pipe"], windowsHide: true });
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
  } catch { /* protocol failures are reported by timeout */ }
});

function request(method, params) {
  const id = `marketplace-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const health = await request("runtime.health");
  const capabilities = health.result?.capabilities ?? [];
  for (const capability of ["marketplace.catalog", "marketplace.models.search", "marketplace.models.install.start", "marketplace.models.install.status", "marketplace.models.install.cancel"]) {
    if (!health.ok || !capabilities.includes(capability)) throw new Error(`runtime.health did not advertise ${capability}.`);
  }
  const catalog = await request("marketplace.catalog");
  const providers = catalog.result?.providers ?? [];
  if (!catalog.ok || !providers.some(provider => provider.id === "huggingface") || !providers.some(provider => provider.id === "modelscope") || !Array.isArray(catalog.result?.skills) || !Array.isArray(catalog.result?.tools)) throw new Error("marketplace.catalog did not return the provider and capability catalog.");
  const search = await request("marketplace.models.search", { query: "qwen", uncensored: false });
  if (!search.ok || !Array.isArray(search.result?.models) || !search.result?.providers || typeof search.result.providers !== "object") throw new Error(`marketplace.models.search returned an invalid provider response: ${JSON.stringify(search)}`);
  for (const model of search.result.models) {
    if (typeof model.id !== "string" || typeof model.repository !== "string" || typeof model.source !== "string" || typeof model.installable !== "boolean") throw new Error("marketplace.models.search returned an incomplete model record.");
  }
  console.log(`Marketplace host smoke passed (${providers.length} providers, ${search.result.models.length} model results).`);
}

run().catch(error => {
  console.error(error instanceof Error ? error.message : error);
  if (stderr.trim()) console.error(stderr.trim());
  process.exitCode = 1;
}).finally(() => {
  child.kill();
});
