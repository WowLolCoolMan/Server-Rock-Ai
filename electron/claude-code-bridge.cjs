const { spawn, execFile } = require("node:child_process");
const { promisify } = require("node:util");
const { createSafeChildEnvironment } = require("./child-environment.cjs");

const execFileAsync = promisify(execFile);
const MAX_PROMPT_LENGTH = 1_000_000;
const MAX_OUTPUT_LENGTH = 2 * 1024 * 1024;
const MAX_ERROR_LENGTH = 16 * 1024;

function commandCandidates(platform = process.platform) {
  return platform === "win32" ? ["claude.cmd", "claude.exe", "claude"] : ["claude"];
}

function textFromContent(content) {
  if (typeof content === "string") return content;
  if (!Array.isArray(content)) return "";
  return content
    .map((part) => (part && typeof part.text === "string" ? part.text : ""))
    .join("");
}

function formatMessages(messages) {
  if (!Array.isArray(messages) || messages.length === 0) throw new Error("Claude Code requires at least one message.");
  const prompt = messages
    .map((message) => {
      const role = typeof message?.role === "string" ? message.role.toUpperCase() : "MESSAGE";
      return `${role}:\n${textFromContent(message?.content)}`;
    })
    .join("\n\n");
  if (!prompt.trim()) throw new Error("Claude Code requires non-empty message content.");
  if (prompt.length > MAX_PROMPT_LENGTH) throw new Error("Claude Code prompt exceeds the 1 MB bridge limit.");
  return prompt;
}

function extractText(event) {
  if (!event || typeof event !== "object") return "";
  if (event.delta && typeof event.delta.text === "string") return event.delta.text;
  if (event.message?.content) return textFromContent(event.message.content);
  if (typeof event.result === "string") return event.result;
  if (typeof event.text === "string") return event.text;
  return "";
}

async function resolveClaudeExecutable({ platform = process.platform, env = process.env, execFileImpl = execFileAsync } = {}) {
  const safeEnv = createSafeChildEnvironment(env);
  for (const candidate of commandCandidates(platform)) {
    try {
      await execFileImpl(candidate, ["--version"], {
        env: safeEnv,
        timeout: 10_000,
        maxBuffer: 64 * 1024,
        windowsHide: true,
        shell: platform === "win32",
      });
      return candidate;
    } catch {
      // Try the next platform-appropriate executable name.
    }
  }
  return null;
}

function launchClaudeLogin({ executable, platform = process.platform, spawnImpl = spawn } = {}) {
  if (typeof executable !== "string" || !executable) throw new Error("Claude Code is not installed.");
  const options = { detached: true, stdio: "ignore", windowsHide: false };
  if (platform === "win32") {
    const child = spawnImpl(process.env.ComSpec || "cmd.exe", ["/d", "/k", executable], options);
    child.unref?.();
    return { launched: true, instructions: "Complete sign-in in the Claude Code terminal, then return to Server Rock: AI." };
  }
  if (platform === "darwin") {
    const child = spawnImpl("osascript", ["-e", `tell application \"Terminal\" to do script \"${executable.replaceAll('\\"', '\\\\"')}\"`], options);
    child.unref?.();
    return { launched: true, instructions: "Complete sign-in in the Claude Code terminal, then return to Server Rock: AI." };
  }
  const child = spawnImpl("x-terminal-emulator", ["-e", executable], options);
  child.unref?.();
  return { launched: true, instructions: "Complete sign-in in the Claude Code terminal, then return to Server Rock: AI." };
}

function streamClaudeCode({ executable, messages, model, signal, onChunk, spawnImpl = spawn, env = process.env, platform = process.platform } = {}) {
  if (typeof executable !== "string" || !executable) return Promise.reject(new Error("Claude Code is not installed. Install the official Claude Code CLI, then sign in with your Claude account."));
  const prompt = formatMessages(messages);
  if (typeof model !== "string" || !/^[\w.-]{1,256}$/.test(model)) throw new Error("Choose a valid Claude Code model.");
  // Keep user content on stdin. Passing it through a Windows shell would make
  // shell metacharacters executable, and large prompts exceed command-line limits.
  const args = ["-p", "Respond to the input provided on stdin.", "--output-format", "stream-json", "--model", model];
  const windowsShim = platform === "win32" && /\.cmd$/i.test(executable);
  const command = windowsShim ? (env.ComSpec || process.env.ComSpec || "cmd.exe") : executable;
  const commandArgs = windowsShim
    ? ["/d", "/s", "/c", `"${executable}" -p "Respond to the input provided on stdin." --output-format stream-json --model ${model}`]
    : args;
  return new Promise((resolve, reject) => {
    const child = spawnImpl(command, commandArgs, {
      env: createSafeChildEnvironment(env),
      shell: false,
      windowsHide: true,
      stdio: ["pipe", "pipe", "pipe"],
    });
    let stdout = "", stderr = "", output = "", settled = false;
    const finish = (error, result) => {
      if (settled) return;
      settled = true;
      signal?.removeEventListener("abort", abort);
      error ? reject(error) : resolve(result);
    };
    const emit = (text) => {
      if (!text) return;
      const next = output + text;
      if (next.length > MAX_OUTPUT_LENGTH) {
        child.kill("SIGTERM");
        finish(new Error("Claude Code response exceeded the 2 MB bridge limit."));
        return;
      }
      output = next;
      onChunk?.(text);
    };
    const consume = (chunk) => {
      stdout += chunk.toString("utf8");
      const lines = stdout.split(/\r?\n/);
      stdout = lines.pop() || "";
      for (const line of lines) {
        if (!line.trim()) continue;
        try {
          const text = extractText(JSON.parse(line));
          if (text) {
            const remainder = text.startsWith(output) ? text.slice(output.length) : text;
            emit(remainder);
          }
        } catch {
          // Stream-json may include diagnostic lines; never surface them as model text.
        }
      }
    };
    const abort = () => {
      child.kill("SIGTERM");
      finish(new DOMException("The Claude Code request was aborted.", "AbortError"));
    };
    child.stdout?.on("data", consume);
    child.stderr?.on("data", (chunk) => {
      stderr = (stderr + chunk.toString("utf8")).slice(-MAX_ERROR_LENGTH);
    });
    child.once("error", (error) => finish(new Error("Claude Code could not start: " + error.message)));
    child.once("close", (code) => {
      if (settled) return;
      if (stdout.trim()) {
        try {
          const text = extractText(JSON.parse(stdout.trim()));
          if (text) emit(text.startsWith(output) ? text.slice(output.length) : text);
        } catch {}
      }
      if (code !== 0) {
        const detail = stderr.trim();
        const hint = /login|auth|credential|not signed/i.test(detail)
          ? " Run Claude Code once in the sign-in terminal and choose your Claude account."
          : "";
        finish(new Error((detail || `Claude Code exited with status ${code}.`).slice(0, 800) + hint));
        return;
      }
      finish(null, { content: output, model });
    });
    child.stdin?.end(prompt);
    if (signal?.aborted) abort();
    else signal?.addEventListener("abort", abort, { once: true });
  });
}

module.exports = {
  MAX_PROMPT_LENGTH,
  MAX_OUTPUT_LENGTH,
  commandCandidates,
  textFromContent,
  formatMessages,
  extractText,
  resolveClaudeExecutable,
  launchClaudeLogin,
  streamClaudeCode,
};
