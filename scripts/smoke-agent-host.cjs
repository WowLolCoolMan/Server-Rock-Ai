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
  SRAI_SCHEDULED_TASKS_PATH: path.join(tempRoot, "scheduled-tasks.json"),
};
const interruptedTaskId = "interrupted-smoke-task";
fs.writeFileSync(env.SRAI_TASKS_PATH, JSON.stringify([{
  id: interruptedTaskId,
  title: "Interrupted smoke task",
  instruction: "Verify restart recovery.",
  role: "specialist",
  status: "running",
  dependsOn: [],
  resultSummary: null,
  artifactLabels: [],
  worktreePath: null,
  worktreeBranch: null,
  worktreeRepositoryRoot: null,
  createdAtUtc: new Date().toISOString(),
  updatedAtUtc: new Date().toISOString(),
}], null, 2));
const hostOutput = path.join(root, "services", "agent-host", "bin", "Debug", "net10.0");
const hostProgram = process.platform === "win32" ? path.join(hostOutput, "ServerRock.AgentHost.exe") : "dotnet";
const hostArgs = process.platform === "win32" ? ["--stdio"] : [path.join(hostOutput, "ServerRock.AgentHost.dll"), "--stdio"];
const child = spawn(hostProgram, hostArgs, {
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
  if (!health.ok || !health.result?.capabilities?.includes("conversation.save") || !health.result?.capabilities?.includes("settings.save") || !health.result?.capabilities?.includes("voice.speak") || !health.result?.capabilities?.includes("tasks.execute"))
    throw new Error("runtime.health did not advertise the replacement persistence capabilities.");
  const recovered = await request("tasks.list");
  const interrupted = recovered.result?.find(task => task.id === interruptedTaskId);
  if (!recovered.ok || interrupted?.status !== "blocked" || !interrupted.resultSummary?.includes("interrupted"))
    throw new Error("Agent Host did not reconcile an interrupted task after restart.");
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
  const plannedTask = tasks.result?.find(task => task.id === planned.result.id);
  if (!tasks.ok || plannedTask?.title !== "Protocol smoke task" || plannedTask.instruction !== "Review the local task workflow." || !Array.isArray(plannedTask.artifactLabels)) throw new Error("tasks.plan persistence round-trip failed.");
  const queued = await request("tasks.queue", { id: planned.result.id });
  if (!queued.ok || queued.result?.status !== "queued") throw new Error("tasks.queue did not queue the local task.");
  const isolatedModelSettings = await request("settings.save", { endpoint: "http://127.0.0.1:1/v1", model: "protocol-smoke-model", provider: "Ollama" });
  if (!isolatedModelSettings.ok) throw new Error("settings.save could not prepare the isolated task execution endpoint.");
  const execution = await request("tasks.execute", { id: planned.result.id });
  if (!execution.ok || execution.result?.status !== "running") throw new Error("tasks.execute did not start a background local execution.");
  let terminalTask = null;
  for (let attempt = 0; attempt < 80; attempt += 1) {
    await new Promise(resolve => setTimeout(resolve, 100));
    const snapshot = await request("tasks.list");
    if (!snapshot.ok) throw new Error("tasks.list failed while observing local execution.");
    terminalTask = snapshot.result.find(item => item.id === planned.result.id) ?? null;
    if (terminalTask && ["completed", "failed", "cancelled", "blocked"].includes(terminalTask.status)) break;
  }
  if (!terminalTask || terminalTask.status !== "failed" || !terminalTask.resultSummary) throw new Error("tasks.execute did not persist a terminal local-model failure.");
  const rejectedEnvironment = await request("tasks.plan", { title: "Unsupported environment", instruction: "Must be rejected.", environment: "worktree" });
  if (rejectedEnvironment.ok || !rejectedEnvironment.error?.message.includes("Only the Local environment")) throw new Error("tasks.plan accepted an unsupported environment.");
  const scheduled = await request("scheduled.create", { prompt: "Run the local review", dueAt: new Date(Date.now() + 60_000).toISOString(), repeatEveryMinutes: 60 });
  if (!scheduled.ok || scheduled.result?.status !== "scheduled") throw new Error("scheduled.create did not persist a scheduled run.");
  const runNow = await request("scheduled.runNow", { id: scheduled.result.id });
  if (!runNow.ok || runNow.result?.id !== scheduled.result.id) throw new Error("scheduled.runNow did not update the scheduled run.");
  const paused = await request("scheduled.pause", { id: scheduled.result.id });
  if (!paused.ok || paused.result?.status !== "paused") throw new Error("scheduled.pause did not pause the scheduled run.");
  const resumed = await request("scheduled.resume", { id: scheduled.result.id });
  if (!resumed.ok || resumed.result?.status !== "scheduled") throw new Error("scheduled.resume did not resume the scheduled run.");
  const cancelled = await request("scheduled.cancel", { id: scheduled.result.id });
  if (!cancelled.ok || cancelled.result?.status !== "cancelled") throw new Error("scheduled.cancel did not cancel the scheduled run.");
  console.log("Agent host replacement smoke passed: health, voice protocol, disposable conversation/settings persistence, local task planning, scheduled automation lifecycle, and direct host lifecycle.");
}

async function stopHost() {
  if (!child.stdin.destroyed) child.stdin.end();
  if (child.exitCode === null) {
    await new Promise(resolve => {
      const timeout = setTimeout(() => {
        if (child.exitCode !== null) return;
        if (process.platform === "win32" && child.pid) {
          const killer = spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore", windowsHide: true });
          killer.once("error", () => { if (!child.killed) child.kill(); });
        } else if (!child.killed) child.kill("SIGTERM");
      }, 1500);
      child.once("exit", () => { clearTimeout(timeout); resolve(); });
    });
  }
  reader.close();
  fs.rmSync(tempRoot, { recursive: true, force: true });
  if (child.exitCode !== 0) throw new Error("The direct Agent Host did not exit cleanly after the smoke test.");
}

async function main() {
  try {
    await run();
  } catch (error) {
    console.error(error.message);
    if (stderr.trim()) console.error(stderr.trim());
    process.exitCode = 1;
  }
  try {
    await stopHost();
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

void main();
