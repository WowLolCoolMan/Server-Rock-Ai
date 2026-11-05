const test = require("node:test");
const assert = require("node:assert/strict");
const { EventEmitter } = require("node:events");
const {
  commandCandidates,
  formatMessages,
  extractText,
  resolveClaudeExecutable,
  streamClaudeCode,
} = require("../electron/claude-code-bridge.cjs");

test("Claude Code bridge uses platform-safe executable candidates", () => {
  assert.deepEqual(commandCandidates("win32"), ["claude.cmd", "claude.exe", "claude"]);
  assert.deepEqual(commandCandidates("linux"), ["claude"]);
});

test("Claude Code bridge formats text and multimodal message parts without secrets", () => {
  const prompt = formatMessages([
    { role: "system", content: "Be concise." },
    { role: "user", content: [{ type: "text", text: "Hello" }] },
  ]);
  assert.match(prompt, /SYSTEM:\nBe concise\./);
  assert.match(prompt, /USER:\nHello/);
  assert.equal(prompt.includes("api_key"), false);
});

test("Claude Code stream extracts assistant text and strips API-key-shaped environment variables", async () => {
  let spawnOptions;
  const child = new EventEmitter();
  child.stdout = new EventEmitter();
  child.stderr = new EventEmitter();
  child.kill = () => {};
  const chunks = [];
  const resultPromise = streamClaudeCode({
    executable: "claude",
    model: "sonnet",
    messages: [{ role: "user", content: "Say hello" }],
    env: { PATH: "test", ANTHROPIC_API_KEY: "must-not-pass" },
    spawnImpl: (_command, _args, options) => {
      spawnOptions = options;
      setImmediate(() => {
        child.stdout.emit("data", Buffer.from(JSON.stringify({ type: "assistant", message: { content: [{ type: "text", text: "Hello" }] } }) + "\n"));
        child.emit("close", 0);
      });
      return child;
    },
    onChunk: (text) => chunks.push(text),
  });
  const result = await resultPromise;
  assert.equal(result.content, "Hello");
  assert.deepEqual(chunks, ["Hello"]);
  assert.equal(spawnOptions.env.ANTHROPIC_API_KEY, undefined);
  assert.equal(extractText({ delta: { text: "delta" } }), "delta");
});

test("Claude Code executable detection fails closed when the CLI is absent", async () => {
  const executable = await resolveClaudeExecutable({
    platform: "linux",
    execFileImpl: async () => { throw new Error("missing"); },
  });
  assert.equal(executable, null);
});
