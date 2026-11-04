#!/usr/bin/env node
/* UI screenshot harness for design review.
   Loads index.html the same way tests/ui.spec.cjs does (file:// URL, seeded
   localStorage, stubbed desktop bridge) and saves PNGs per state and viewport.
   Usage: node scripts/ui-shots.cjs [label]   ->  .ui-shots/<label>/<state>-<w>x<h>.png */
const { chromium } = require("@playwright/test");
const path = require("node:path");
const fs = require("node:fs");

const label = process.argv[2] || "current";
const outDir = path.resolve(".ui-shots", label);
const appUrl = "file://" + path.resolve("index.html").replaceAll("\\", "/");
const viewports = [
  { width: 1440, height: 900 },
  { width: 1100, height: 750 },
  { width: 800, height: 700 },
];

const messages = [
  { role: "user", content: "Can you sketch a small Express server that serves a JSON health check?" },
  {
    role: "assistant",
    content:
      "Here's a minimal version. It listens on port 3000 and answers `/health` with uptime.\n\n```js\nconst express = require(\"express\");\nconst app = express();\n\napp.get(\"/health\", (req, res) => {\n  res.json({ ok: true, uptime: process.uptime() });\n});\n\napp.listen(3000);\n```\n\n- Run it with `node server.js`\n- Then open http://localhost:3000/health",
  },
  { role: "user", content: "Add a request counter too." },
  {
    role: "assistant",
    content:
      "Add a counter in module scope and bump it in a middleware:\n\n```js\nlet requests = 0;\napp.use((req, res, next) => { requests++; next(); });\n```\n\nThen include `requests` in the health response.",
  },
];

function seed(theme, agentMode, withChat) {
  return {
    chats: withChat ? [{ id: "c1", title: "Express health check", messages }, { id: "c2", title: "Rust lifetimes question", messages: messages.slice(0, 2) }] : [],
    projects: [{ id: "p1", name: "server-rock", folders: [], instructions: "" }],
    agentMode,
    settings: { provider: "Ollama", endpoint: "http://localhost:11434/v1/chat/completions", model: "llama3.2", theme, temperature: 0.7 },
    settingsByProvider: {},
  };
}

// Offline desktop bridge, same shape the tests stub. Model scan returns a few models
// so the picker has realistic content.
function installBridge() {
  const refused = () => Promise.reject(new Error("net::ERR_CONNECTION_REFUSED"));
  window.desktopModels = {
    getProviderSecretStatus: async () => ({ saved: false, secureStorage: true }),
    setProviderSecret: async () => ({ saved: true }),
    scanHosts: async () => ({
      models: [
        { model: "llama3.2", provider: "Ollama", local: true, size: 2019393189, endpoint: "http://localhost:11434/v1/chat/completions" },
        { model: "qwen2.5-coder:7b", provider: "Ollama", local: true, size: 4683087332, endpoint: "http://localhost:11434/v1/chat/completions" },
        { model: "mistral-nemo", provider: "LM Studio", local: true, endpoint: "http://localhost:1234/v1/chat/completions" },
      ],
      hosts: ["localhost:11434", "localhost:1234"],
    }),
    scanProvider: refused,
    streamProviderChat: refused,
    cancelProviderChat: () => {},
    chatgptStatus: async () => ({ connected: false, sharing: false }),
    githubStatus: async () => ({ connected: false, secureStorage: true }),
    githubDefaultClientId: async () => "",
    githubRepos: async () => [],
    updateState: async () => ({ status: "idle", version: "0.1.2" }),
    onUpdateState: () => () => {},
    readProjectFiles: async () => ({ roots: [], files: [] }),
    searchHuggingFace: async () => [],
  };
}

// Each state: which seed to use and what to do after load.
const states = {
  "empty-dark": { theme: "dark" },
  "empty-light": { theme: "light" },
  "chat-dark": { theme: "dark", chat: true, run: (p) => p.locator("#history .chat-item").first().click() },
  "chat-light": { theme: "light", chat: true, run: (p) => p.locator("#history .chat-item").first().click() },
  "model-picker": { theme: "dark", run: async (p) => { await p.locator("#model").click(); await p.waitForTimeout(400); } },
  "settings": { theme: "dark", run: (p) => p.locator("#settings").click() },
  "settings-light": { theme: "light", run: (p) => p.locator("#settings").click() },
  "coding-agent": { theme: "dark", mode: "code", chat: true },
  "mode-menu": { theme: "dark", run: (p) => p.locator("#agentModeToggle").click() },
  "slash-command": { theme: "dark", run: async (p) => { await p.locator("#prompt").fill("/fast summarise this"); await p.locator("#prompt").dispatchEvent("input"); } },
  "advanced": { theme: "dark", run: (p) => p.locator(".advanced-menu summary").click() },
};

(async () => {
  fs.mkdirSync(outDir, { recursive: true });
  const browser = await chromium.launch();
  const only = process.env.STATES ? process.env.STATES.split(",") : null;
  for (const vp of viewports) {
    for (const [name, st] of Object.entries(states)) {
      if (only && !only.includes(name)) continue;
      const context = await browser.newContext({ viewport: vp, deviceScaleFactor: 1, reducedMotion: "reduce" });
      const page = await context.newPage();
      await page.addInitScript(
        ({ data }) => { localStorage.setItem("local-studio-v1", JSON.stringify(data)); },
        { data: seed(st.theme, st.mode || "chat", st.chat) },
      );
      await page.addInitScript(installBridge);
      await page.goto(appUrl);
      await page.waitForTimeout(300);
      if (st.run) await st.run(page).catch((e) => console.warn(name, "step failed:", e.message.split("\n")[0]));
      await page.waitForTimeout(200);
      const file = path.join(outDir, `${name}-${vp.width}x${vp.height}.png`);
      await page.screenshot({ path: file });
      await context.close();
    }
  }
  await browser.close();
  console.log("Saved screenshots to", outDir);
})();
