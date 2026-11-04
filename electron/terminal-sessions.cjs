const path = require("node:path");
const fs = require("node:fs/promises");
const crypto = require("node:crypto");
const childProcess = require("node:child_process");
const { createSafeChildEnvironment } = require("./child-environment.cjs");

const MAX_SESSIONS = 8;
const MAX_STORED_SESSIONS = 32;
const MAX_COMMAND_LENGTH = 16 * 1024;
const MAX_INPUT_BYTES = 64 * 1024;
const MAX_OUTPUT_BYTES = 2 * 1024 * 1024;
const MAX_REPLAY_BYTES = 256 * 1024;
const DEFAULT_TIMEOUT_MS = 15 * 60 * 1000;
const MAX_TIMEOUT_MS = 30 * 60 * 1000;

function isPathWithinOrSame(root, candidate) {
  const relative = path.relative(root, candidate);
  return relative === "" || (!relative.startsWith(`..${path.sep}`) && relative !== ".." && !path.isAbsolute(relative));
}

function validateText(value, label, maxLength) {
  if (typeof value !== "string" || value.length < 1 || value.length > maxLength || value.includes("\u0000"))
    throw Error(`${label} is invalid or too large.`);
  return value;
}

function validateFolders(folders) {
  if (!Array.isArray(folders) || !folders.length || folders.length > 20 || folders.some((folder) => typeof folder !== "string" || folder.length < 1 || folder.length > 4096))
    throw Error("Select an authorized project folder first.");
  return folders;
}

function validateTimeout(value) {
  if (value === undefined || value === null || value === "") return DEFAULT_TIMEOUT_MS;
  const timeout = Number(value);
  if (!Number.isFinite(timeout) || timeout < 1000 || timeout > MAX_TIMEOUT_MS) throw Error("Terminal timeout is invalid.");
  return Math.floor(timeout);
}

function createTerminalManager({ resolveRoots, spawn = childProcess.spawn, platform = process.platform, environment = process.env, now = () => Date.now() } = {}) {
  if (typeof resolveRoots !== "function") throw new TypeError("A project-root resolver is required.");
  const sessions = new Map();

  async function resolveCwd(folders, cwd) {
    validateFolders(folders);
    const roots = await resolveRoots(folders);
    if (!Array.isArray(roots) || !roots.length) throw Error("Use Add a folder to authorize a project first.");
    const requested = validateText(cwd, "Terminal folder", 4096);
    let realCwd;
    try {
      realCwd = await fs.realpath(requested);
      const stat = await fs.stat(realCwd);
      if (!stat.isDirectory()) throw new Error("The terminal folder must be a directory.");
    } catch (error) {
      throw Error(error?.message || "The terminal folder is unavailable.");
    }
    if (!roots.some((root) => isPathWithinOrSame(root, realCwd))) throw Error("The terminal folder must stay inside an authorized project folder.");
    return realCwd;
  }

  function emit(session, event) {
    for (const listener of session.listeners) {
      try { listener({ id: session.id, ...event }); } catch { /* A closed renderer must not break the process session. */ }
    }
  }

  function remember(session, text) {
    session.history += text;
    if (Buffer.byteLength(session.history, "utf8") > MAX_REPLAY_BYTES) {
      const bytes = Buffer.from(session.history, "utf8");
      session.history = bytes.subarray(bytes.length - MAX_REPLAY_BYTES).toString("utf8");
    }
  }

  function appendOutput(session, stream, chunk) {
    if (session.outputBytes >= MAX_OUTPUT_BYTES) return;
    const bytes = Buffer.isBuffer(chunk) ? chunk : Buffer.from(String(chunk));
    const remaining = MAX_OUTPUT_BYTES - session.outputBytes;
    const visible = bytes.subarray(0, remaining).toString("utf8");
    if (visible) {
      session.outputBytes = Math.min(MAX_OUTPUT_BYTES, session.outputBytes + Buffer.byteLength(visible, "utf8"));
      remember(session, visible);
      emit(session, { type: "data", stream, text: visible });
    }
    if (bytes.length > remaining && !session.truncated) {
      session.truncated = true;
      const notice = "\n[terminal output truncated at 2 MiB]\n";
      remember(session, notice);
      emit(session, { type: "data", stream: "system", text: notice, truncated: true });
      try { session.process.kill(); } catch { /* The exit event will finish cleanup. */ }
    }
  }

  function finish(session, code, signal, reason = "exit") {
    if (session.status !== "running") return;
    session.status = "exited";
    session.exitCode = Number.isInteger(code) ? code : null;
    session.signal = signal || null;
    session.endedAt = now();
    if (session.timer) clearTimeout(session.timer);
    emit(session, { type: "exit", status: session.status, code: session.exitCode, signal: session.signal, reason });
  }

  async function start({ folders, cwd, command, timeoutMs } = {}, listener) {
    if ([...sessions.values()].filter((session) => session.status === "running").length >= MAX_SESSIONS) throw Error("Too many active terminal sessions. Stop one before starting another.");
    if (typeof listener !== "function") throw new TypeError("A terminal event listener is required.");
    const realCwd = await resolveCwd(folders, cwd);
    const text = validateText(command, "Terminal command", MAX_COMMAND_LENGTH).trim();
    if (!text) throw Error("Enter a terminal command first.");
    const timeout = validateTimeout(timeoutMs);
    const id = crypto.randomUUID();
    const shell = platform === "win32" ? (environment.ComSpec || "cmd.exe") : (environment.SHELL || "/bin/sh");
    const args = platform === "win32" ? ["/d", "/s", "/c", text] : ["-lc", text];
    const session = {
      id, cwd: realCwd, command: text, startedAt: now(), endedAt: null,
      status: "running", exitCode: null, signal: null, outputBytes: 0,
      history: "", truncated: false, listeners: new Set([listener]), process: null, timer: null,
    };
    if (sessions.size >= MAX_STORED_SESSIONS) {
      const oldest = [...sessions.values()].find((candidate) => candidate.status !== "running");
      if (oldest) sessions.delete(oldest.id);
    }
    let child;
    try {
      child = spawn(shell, args, {
        cwd: realCwd,
        env: createSafeChildEnvironment(environment),
        windowsHide: true,
        stdio: ["pipe", "pipe", "pipe"],
      });
    } catch (error) {
      throw Error(`Could not start terminal: ${error?.message || "spawn failed"}`);
    }
    session.process = child;
    sessions.set(id, session);
    child.stdout?.on("data", (chunk) => appendOutput(session, "stdout", chunk));
    child.stderr?.on("data", (chunk) => appendOutput(session, "stderr", chunk));
    child.on("error", (error) => {
      const message = error?.message || "Terminal process failed.";
      remember(session, `\n[terminal error] ${message}\n`);
      emit(session, { type: "error", message });
      finish(session, null, null, "error");
    });
    child.on("exit", (code, signal) => finish(session, code, signal));
    session.timer = setTimeout(() => {
      if (session.status !== "running") return;
      emit(session, { type: "status", message: "Terminal timed out; stopping the process." });
      try { child.kill(); } catch { /* The exit event will finish cleanup. */ }
      finish(session, null, null, "timeout");
    }, timeout);
    session.timer.unref?.();
    emit(session, { type: "started", cwd: realCwd, command: text, startedAt: session.startedAt });
    return { id, cwd: realCwd, command: text, startedAt: session.startedAt, status: session.status };
  }

  function getSession(id) {
    if (typeof id !== "string" || !/^[0-9a-f-]{36}$/i.test(id)) throw Error("Invalid terminal session ID.");
    const session = sessions.get(id);
    if (!session) throw Error("Terminal session was not found.");
    return session;
  }

  function write(id, input) {
    const session = getSession(id);
    if (session.status !== "running") throw Error("Terminal session has already exited.");
    if (typeof input !== "string" || !input || Buffer.byteLength(input, "utf8") > MAX_INPUT_BYTES || input.includes("\u0000")) throw Error("Terminal input is invalid or too large.");
    session.process.stdin.write(input);
    return { id, accepted: true };
  }

  function stop(id, reason = "user") {
    const session = getSession(id);
    if (session.status === "running") {
      try { session.process.kill(); } catch { /* The process may have exited between the check and kill. */ }
      finish(session, null, null, reason);
    }
    return snapshot(id);
  }

  function attach(id, listener) {
    const session = getSession(id);
    if (typeof listener !== "function") throw new TypeError("A terminal event listener is required.");
    session.listeners.add(listener);
    listener({ id, type: "snapshot", status: session.status, cwd: session.cwd, command: session.command, startedAt: session.startedAt, endedAt: session.endedAt, code: session.exitCode, signal: session.signal, output: session.history, truncated: session.truncated });
    return () => session.listeners.delete(listener);
  }

  function snapshot(id) {
    const session = getSession(id);
    return { id, status: session.status, cwd: session.cwd, command: session.command, startedAt: session.startedAt, endedAt: session.endedAt, code: session.exitCode, signal: session.signal, output: session.history, truncated: session.truncated };
  }

  function list() {
    return [...sessions.values()].map((session) => ({ id: session.id, status: session.status, cwd: session.cwd, command: session.command, startedAt: session.startedAt, endedAt: session.endedAt, code: session.exitCode, signal: session.signal, truncated: session.truncated }));
  }

  function dispose() {
    for (const session of sessions.values()) {
      if (session.status === "running") {
        try { session.process.kill(); } catch { /* Best effort during application shutdown. */ }
        finish(session, null, null, "app-shutdown");
      }
      session.listeners.clear();
    }
    sessions.clear();
  }

  return { start, write, stop, attach, snapshot, list, dispose };
}

module.exports = {
  MAX_SESSIONS,
  MAX_STORED_SESSIONS,
  MAX_COMMAND_LENGTH,
  MAX_INPUT_BYTES,
  MAX_OUTPUT_BYTES,
  MAX_REPLAY_BYTES,
  DEFAULT_TIMEOUT_MS,
  MAX_TIMEOUT_MS,
  createTerminalManager,
};
