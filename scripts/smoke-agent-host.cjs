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
  SRAI_SKILLS_PATH: path.join(tempRoot, "installed-skills"),
};
const skillSource = path.join(tempRoot, "review-skill");
fs.mkdirSync(skillSource, { recursive: true });
fs.writeFileSync(path.join(skillSource, "skill.json"), JSON.stringify({ id: "review-skill", name: "Review skill", version: "1.0.0", publisher: "Smoke publisher", requiredPermissions: ["project.read"] }));
fs.writeFileSync(path.join(skillSource, "SKILL.md"), "# Review\n\nRead-only review instructions.");
const hostOutput = process.env.SRAI_AGENT_HOST_OUTPUT || path.join(root, "services", "agent-host", "bin", "Debug", "net10.0");
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
  if (!health.ok || !health.result?.capabilities?.includes("conversation.save") || !health.result?.capabilities?.includes("settings.save") || !health.result?.capabilities?.includes("credentials.status") || !health.result?.capabilities?.includes("voice.speak") || !health.result?.capabilities?.includes("voice.speak.cloud") || !health.result?.capabilities?.includes("marketplace.models.search") || !health.result?.capabilities?.includes("marketplace.models.download") || !health.result?.capabilities?.includes("marketplace.skills.search") || !health.result?.capabilities?.includes("marketplace.skills.inspect") || !health.result?.capabilities?.includes("skills.local.preview") || !health.result?.capabilities?.includes("skills.local.install") || !health.result?.capabilities?.includes("skills.enabled.set") || !health.result?.capabilities?.includes("skills.installed.inspect") || !health.result?.capabilities?.includes("skills.installed.remove") || !health.result?.capabilities?.includes("skills.rollback") || !health.result?.capabilities?.includes("tasks.execute"))
    throw new Error("runtime.health did not advertise the replacement persistence capabilities.");
  const tools = await request("marketplace.tools.list");
  if (!tools.ok || !Array.isArray(tools.result) || !tools.result.some(item => item.id === "github" && item.builtIn === true)) throw new Error("marketplace.tools.list did not return the built-in tool catalog.");
  const mcp = await request("marketplace.mcp.list");
  if (!mcp.ok || !Array.isArray(mcp.result) || !mcp.result.some(item => item.id === "filesystem" && item.transport === "stdio")) throw new Error("marketplace.mcp.list did not return the MCP server catalog.");
  const emptyModelScope = await request("marketplace.models.search", { query: "", kind: "llm", provider: "modelscope", limit: 20 });
  if (!emptyModelScope.ok || !Array.isArray(emptyModelScope.result)) throw new Error("marketplace.models.search did not accept the ModelScope provider lane.");
  const invalidModelProvider = await request("marketplace.models.search", { query: "", kind: "llm", provider: "unknown-provider", limit: 20 });
  if (invalidModelProvider.ok) throw new Error("marketplace.models.search accepted an unknown provider filter.");
  const unsupportedDownloadProvider = await request("marketplace.models.download", { provider: "unknown-provider", endpoint: "http://127.0.0.1:11434/v1", id: "example/model" });
  if (unsupportedDownloadProvider.ok) throw new Error("marketplace.models.download accepted an unknown provider.");
  const invalidSkillRepository = await request("marketplace.skills.inspect", { repository: "https://evil.invalid/skill" });
  if (invalidSkillRepository.ok || !invalidSkillRepository.error?.message.includes("owner/name")) throw new Error("marketplace.skills.inspect accepted an unsafe repository identifier.");
  const emptySkills = await request("skills.installed.list");
  if (!emptySkills.ok || !Array.isArray(emptySkills.result) || emptySkills.result.length !== 0) throw new Error("Disposable skill library was not empty.");
  const skillPreview = await request("skills.local.preview", { sourceDirectory: skillSource });
  if (!skillPreview.ok || skillPreview.result?.isUpdate || skillPreview.result?.requiredPermissions?.[0] !== "project.read" || skillPreview.result?.packageSha256?.length !== 64) throw new Error("skills.local.preview did not return a bounded package review.");
  const installedSkill = await request("skills.local.install", { sourceDirectory: skillSource, expectedPackageSha256: skillPreview.result.packageSha256 });
  if (!installedSkill.ok || installedSkill.result?.enabled !== false || installedSkill.result?.sourceRef !== "local-folder:review-skill") throw new Error("skills.local.install did not require explicit enablement or preserve local provenance.");
  const enabledSkill = await request("skills.enabled.set", { id: "review-skill", enabled: true });
  if (!enabledSkill.ok || enabledSkill.result?.enabled !== true) throw new Error("skills.enabled.set did not enable the installed skill.");
  fs.writeFileSync(path.join(skillSource, "skill.json"), JSON.stringify({ id: "review-skill", name: "Review skill", version: "2.0.0", publisher: "Smoke publisher", requiredPermissions: ["project.read"] }));
  fs.writeFileSync(path.join(skillSource, "SKILL.md"), "# Review v2\n\nUpdated read-only review instructions.");
  const updatePreview = await request("skills.local.preview", { sourceDirectory: skillSource });
  if (!updatePreview.ok || updatePreview.result?.isUpdate !== true || updatePreview.result?.existingVersion !== "1.0.0") throw new Error("skills.local.preview did not identify a skill update.");
  const updatedSkill = await request("skills.local.install", { sourceDirectory: skillSource, expectedPackageSha256: updatePreview.result.packageSha256 });
  if (!updatedSkill.ok || updatedSkill.result?.version !== "2.0.0" || updatedSkill.result?.canRollback !== true) throw new Error("skills.local.install did not activate the update with rollback metadata.");
  const rolledBackSkill = await request("skills.rollback", { id: "review-skill" });
  if (!rolledBackSkill.ok || rolledBackSkill.result?.version !== "1.0.0" || rolledBackSkill.result?.enabled !== true) throw new Error("skills.rollback did not restore the prior enabled package.");
  const inspectedSkill = await request("skills.installed.inspect", { id: "review-skill" });
  if (!inspectedSkill.ok || !inspectedSkill.result?.instructions?.includes("Read-only review instructions")) throw new Error("skills.installed.inspect did not return bounded instructions.");
  const removedSkill = await request("skills.installed.remove", { id: "review-skill" });
  if (!removedSkill.ok || removedSkill.result?.removed !== true) throw new Error("skills.installed.remove did not remove the owned package.");
  const finalSkills = await request("skills.installed.list");
  if (!finalSkills.ok || finalSkills.result.length !== 0) throw new Error("skills.installed.remove left an installed package behind.");
  const voices = await request("voice.models");
  if (!voices.ok || !Array.isArray(voices.result?.models) || !voices.result.models.some(model => model.id === "os-native" && model.installed))
    throw new Error("voice.models did not advertise the installed OS-native voice engine.");
  const invalidSpeech = await request("voice.speak", { text: "   " });
  if (invalidSpeech.ok) throw new Error("voice.speak accepted empty text.");
  const invalidCloudSpeech = await request("voice.speak.cloud", { text: "   ", cloudVoiceEnabled: true });
  if (invalidCloudSpeech.ok) throw new Error("voice.speak.cloud accepted empty text.");
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
  const remoteSettings = await request("settings.save", { endpoint: "https://api.example.invalid/v1", model: "remote-model", provider: "Hosted compatible", cloudRouteEnabled: true });
  if (!remoteSettings.ok || remoteSettings.result?.cloudRouteEnabled !== true) throw new Error("settings.save did not preserve an explicitly enabled HTTPS cloud route.");
  const restoredSettings = await request("settings.save", { endpoint: settings.result.endpoint, model: settings.result.model, provider: "Ollama" });
  if (!restoredSettings.ok) throw new Error("settings.save could not restore the disposable local profile.");
  const planned = await request("tasks.plan", { title: "Protocol smoke task", instruction: "Review the local task workflow.", environment: "local" });
  if (!planned.ok || planned.result?.status !== "proposed") throw new Error("tasks.plan did not create a proposed local task.");
  const tasks = await request("tasks.list");
  if (!tasks.ok || tasks.result.length !== 1 || tasks.result[0].title !== "Protocol smoke task" || tasks.result[0].instruction !== "Review the local task workflow.") throw new Error("tasks.plan persistence round-trip failed.");
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
