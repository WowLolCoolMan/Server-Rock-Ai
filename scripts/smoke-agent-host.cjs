#!/usr/bin/env node

/* Isolated replacement-shell protocol smoke. It exercises only disposable
 * settings/history files and never reads or writes the user's profile. */

const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { spawn } = require("node:child_process");
const readline = require("node:readline");

const root = path.resolve(__dirname, "..");
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), "skai-agent-host-smoke-"));
const env = {
  ...process.env,
  SRAI_SETTINGS_PATH: path.join(tempRoot, "settings.json"),
  SRAI_CONVERSATIONS_PATH: path.join(tempRoot, "conversations.json"),
  SRAI_TASKS_PATH: path.join(tempRoot, "agent-tasks.json"),
};
const child = spawn("dotnet", ["run", "--project", path.join(root, "services", "agent-host", "ServerRock.AgentHost.csproj"), "--no-build", "--", "--stdio"], {
  cwd: root,
  env,
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});
const reader = readline.createInterface({ input: child.stdout });
let nextId = 1;
const pending = new Map();
let stderr = "";

child.stderr.on("data", chunk => { stderr += chunk.toString(); });
reader.on("line", line => {
  try {
    const response = JSON.parse(line);
    const resolve = pending.get(response.id);
    if (resolve) { pending.delete(response.id); resolve(response); }
  } catch { /* The host protocol response is validated by the request timeout. */ }
});

function request(method, params) {
  const id = `smoke-${nextId++}`;
  child.stdin.write(`${JSON.stringify({ v: 1, id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timed out waiting for ${method}.`)); }, 10_000);
    pending.set(id, response => { clearTimeout(timer); resolve(response); });
  });
}

async function run() {
  const health = await request("runtime.health");
  if (!health.ok || !health.result?.capabilities?.includes("conversation.save") || !health.result?.capabilities?.includes("settings.save") || !health.result?.capabilities?.includes("voice.speak"))
    throw new Error("runtime.health did not advertise the replacement persistence capabilities.");
  const voices = await request("voice.models");
  if (!voices.ok || !Array.isArray(voices.result?.models) || !voices.result.models.some(model => model.id === "os-native" && model.installed))
    throw new Error("voice.models did not advertise the installed OS-native voice engine.");
  const invalidSpeech = await request("voice.speak", { text: "   " });
  if (invalidSpeech.ok) throw new Error("voice.speak accepted empty text.");
  const stoppedVoice = await request("voice.stop");
  if (!stoppedVoice.ok || stoppedVoice.result?.stopped !== true) throw new Error("voice.stop did not acknowledge the idle interruption request.");
  const before = await request("conversations.list");
  if (!before.ok || !Array.isArray(before.result) || before.result.length !== 0)
    throw new Error("Disposable conversation history was not empty.");
  const saved = await request("conversation.save", { title: "Protocol smoke", messages: [{ role: "user", content: "hello" }, { role: "assistant", content: "world" }] });
  if (!saved.ok || typeof saved.result?.id !== "string") throw new Error("conversation.save did not return a local identity.");
  const after = await request("conversations.list");
  if (!after.ok || after.result.length !== 1 || after.result[0].title !== "Protocol smoke") throw new Error("conversation persistence round-trip failed.");
  const settings = await request("settings.get");
  if (!settings.ok || typeof settings.result?.endpoint !== "string") throw new Error("settings.get did not return bounded settings.");
  const savedSettings = await request("settings.save", { endpoint: settings.result.endpoint, model: settings.result.model });
  if (!savedSettings.ok) throw new Error("settings.save failed in the disposable profile.");
  const remoteSettings = await request("settings.save", { endpoint: "https://api.example.invalid/v1", model: "remote-model", provider: "Hosted compatible" });
  if (remoteSettings.ok || !remoteSettings.error?.message.includes("local loopback")) throw new Error("settings.save accepted a non-local model endpoint.");
  const restoredSettings = await request("settings.save", { endpoint: settings.result.endpoint, model: settings.result.model, provider: "Ollama" });
  if (!restoredSettings.ok) throw new Error("settings.save could not restore the disposable local profile.");
  const planned = await request("tasks.plan", { title: "Protocol smoke task", instruction: "Review the local task workflow.", environment: "local" });
  if (!planned.ok || planned.result?.status !== "proposed") throw new Error("tasks.plan did not create a proposed local task.");
  const tasks = await request("tasks.list");
  if (!tasks.ok || tasks.result.length !== 1 || tasks.result[0].title !== "Protocol smoke task" || tasks.result[0].instruction !== "Review the local task workflow.") throw new Error("tasks.plan persistence round-trip failed.");
  const rejectedEnvironment = await request("tasks.plan", { title: "Unsupported environment", instruction: "Must be rejected.", environment: "worktree" });
  if (rejectedEnvironment.ok || !rejectedEnvironment.error?.message.includes("Only the Local environment")) throw new Error("tasks.plan accepted an unsupported environment.");
  console.log("Agent host replacement smoke passed: health, voice protocol, disposable conversation/settings persistence, and local task planning.");
}

run().catch(error => {
  console.error(error.message);
  if (stderr.trim()) console.error(stderr.trim());
  process.exitCode = 1;
}).finally(async () => {
  if (!child.stdin.destroyed) child.stdin.end();
  if (child.exitCode !== null) {
    reader.close();
    fs.rmSync(tempRoot, { recursive: true, force: true });
    return;
  }
  await new Promise(resolve => {
    const timeout = setTimeout(() => { if (!child.killed) child.kill(); }, 1500);
    child.once("exit", () => { clearTimeout(timeout); resolve(); });
  });
  reader.close();
  fs.rmSync(tempRoot, { recursive: true, force: true });
});
