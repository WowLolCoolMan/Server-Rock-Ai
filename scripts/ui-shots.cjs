#!/usr/bin/env node
/* UI screenshot harness for design review.
   Loads index.html the same way tests/ui.spec.cjs does (file:// URL, seeded
   localStorage, stubbed desktop bridge) and saves PNGs per state and viewport.
   Usage: node scripts/ui-shots.cjs [label]   ->  .ui-shots/<label>/<state>-<w>x<h>.png */
const { chromium } = require("@playwright/test");
const path = require("node:path");
const fs = require("node:fs");

const label = process.argv[2] || "current";
/* Labels may be nested one or two levels (e.g. "round-1/before") so the polish loop can
   keep before/after shots per round. Each segment is still validated, so ".." can't escape. */
const segment = /^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/;
const parts = label.split("/");
if (parts.length > 2 || parts.some((part) => !segment.test(part) || part === "." || part === "..")) {
  throw new Error("Label must be one or two '/'-separated parts of 1–64 letters, numbers, dots, dashes, or underscores, each starting with a letter or number.");
}
const outDir = path.resolve(".ui-shots", label);
const appUrl = "file://" + path.resolve("index.html").replaceAll("\\", "/");
const viewports = [
  { width: 1440, height: 900 },
  { width: 1100, height: 750 },
  { width: 800, height: 700 },
  { width: 390, height: 844 },
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
    // The Activity page lists terminal sessions; GitHub dialogs ask for project status.
    terminalList: async () => [],
    terminalOnEvent: () => () => {},
    githubProjectStatus: async () => ({ repository: false }),
  };
}

// Pages and dialogs. On phones the rail is hidden, so pages open from the drawer's nav.
async function openPage(p, page) {
  if (await p.locator("#mobileMenu").isVisible()) {
    await p.locator("#mobileMenu").click();
    await p.locator(`.mobile-nav [data-page="${page}"]`).click();
  } else {
    await p.locator(`.rail [data-page="${page}"]`).click();
  }
}
async function fromDrawer(p, selector) {
  if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click();
  await p.locator(selector).click();
}

async function openChatMenu(p) {
  if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click();
  await p.locator("#history .chat-item").first().click();
  if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click();
  await p.locator(".chat-row .chat-item-actions").first().click();
}

async function openAccountMenu(p) {
  if (await p.locator("#accountMenuToggle").isVisible()) await p.locator("#accountMenuToggle").click();
  else await p.locator("#mobileMenu").click();
}

// Each state: which seed to use and what to do after load.
const states = {
  "empty-dark": { theme: "dark" },
  "empty-light": { theme: "light" },
  "chat-dark": { theme: "dark", chat: true, run: async (p) => { if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click(); await p.locator("#history .chat-item").first().click(); } },
  "chat-light": { theme: "light", chat: true, run: async (p) => { if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click(); await p.locator("#history .chat-item").first().click(); } },
  // The chat row's ⋯ menu, so sidebar polish can be checked in both themes.
  "chat-menu": { theme: "dark", chat: true, run: openChatMenu },
  "chat-menu-light": { theme: "light", chat: true, run: openChatMenu },
  "model-picker": { theme: "dark", run: async (p) => { await p.locator("#model").click(); await p.waitForTimeout(400); } },
  "settings": { theme: "dark", run: (p) => p.locator("#settings").click() },
  "settings-light": { theme: "light", run: (p) => p.locator("#settings").click() },
  "coding-agent": { theme: "dark", mode: "code", chat: true },
  "mode-menu": { theme: "dark", run: async (p) => { if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click(); await p.locator("#agentModeToggle").click(); } },
  // Light versions of the popovers, plus the rail's account menu (on phones the drawer's
  // account buttons stand in for it, since the rail is hidden there).
  "mode-menu-light": { theme: "light", run: async (p) => { if (await p.locator("#mobileMenu").isVisible()) await p.locator("#mobileMenu").click(); await p.locator("#agentModeToggle").click(); } },
  "advanced-light": { theme: "light", run: (p) => p.locator(".advanced-menu summary").click() },
  "account-menu": { theme: "dark", run: openAccountMenu },
  "account-menu-light": { theme: "light", run: openAccountMenu },
  // Composer states: typing in light, and the voice button while recording. Recording is
  // faked with the same class and glyph app.js sets, since headless Chromium has no microphone.
  "slash-command-light": { theme: "light", run: async (p) => { await p.locator("#prompt").fill("/fast summarise this"); await p.locator("#prompt").dispatchEvent("input"); } },
  "voice-recording": { theme: "dark", run: (p) => p.evaluate(() => { const v = document.querySelector("#voice"); v.textContent = "■"; v.classList.add("recording"); }) },
  "slash-command": { theme: "dark", run: async (p) => { await p.locator("#prompt").fill("/fast summarise this"); await p.locator("#prompt").dispatchEvent("input"); } },
  "advanced": { theme: "dark", run: (p) => p.locator(".advanced-menu summary").click() },
  // Screens added for the 10-chunk UI plan: every page and dialog, in both themes.
  "model-picker-light": { theme: "light", run: async (p) => { await p.locator("#model").click(); await p.waitForTimeout(400); } },
  "coding-agent-light": { theme: "light", mode: "code", chat: true },
  "slash-menu": { theme: "dark", run: (p) => p.locator("#slashHelp").click() },
  "slash-menu-light": { theme: "light", run: (p) => p.locator("#slashHelp").click() },
  "images": { theme: "dark", run: (p) => openPage(p, "images") },
  "images-light": { theme: "light", run: (p) => openPage(p, "images") },
  "activity": { theme: "dark", chat: true, run: (p) => openPage(p, "activity") },
  "activity-light": { theme: "light", chat: true, run: (p) => openPage(p, "activity") },
  "models-page": { theme: "dark", run: (p) => p.locator("#welcomeMarketplace").click() },
  "models-page-light": { theme: "light", run: (p) => p.locator("#welcomeMarketplace").click() },
  "github-dialog": { theme: "dark", run: (p) => fromDrawer(p, "#openGitHub") },
  "github-dialog-light": { theme: "light", run: (p) => fromDrawer(p, "#openGitHub") },
  "project-dialog": { theme: "dark", run: (p) => fromDrawer(p, "#newProject") },
  "project-dialog-light": { theme: "light", run: (p) => fromDrawer(p, "#newProject") },
  // The push review normally needs a connected GitHub project; it's opened directly to check its layout.
  "push-review": { theme: "dark", run: (p) => p.evaluate(() => document.querySelector("#githubPushDialog").showModal()) },
  "push-review-light": { theme: "light", run: (p) => p.evaluate(() => document.querySelector("#githubPushDialog").showModal()) },
};

const only = process.env.STATES !== undefined
  ? process.env.STATES.split(",").map((name) => name.trim()).filter(Boolean)
  : Object.keys(states);
const unknown = only.filter((name) => !Object.hasOwn(states, name));
if (unknown.length) throw new Error(`Unknown state(s): ${unknown.join(", ")}`);
if (!only.length) throw new Error("Select at least one state to capture.");

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const browser = await chromium.launch();
  try {
    for (const vp of viewports) {
      for (const name of only) {
        const st = states[name];
        const context = await browser.newContext({ viewport: vp, deviceScaleFactor: 1, reducedMotion: "reduce" });
        try {
          const page = await context.newPage();
          await page.addInitScript(
            ({ data }) => { localStorage.setItem("local-studio-v1", JSON.stringify(data)); },
            { data: seed(st.theme, st.mode || "chat", st.chat) },
          );
          await page.addInitScript(installBridge);
          await page.goto(appUrl);
          await page.locator("#prompt").waitFor({ state: "visible" });
          if (st.run) await st.run(page);
          // Let layout, fonts and the post-click scroll settle so shots don't catch a half-laid-out frame.
          await page.evaluate(() => document.fonts.ready);
          await page.waitForTimeout(250);
          const file = path.join(outDir, `${name}-${vp.width}x${vp.height}.png`);
          await page.screenshot({ path: file });
        } catch (error) {
          throw new Error(`Failed to capture ${name} at ${vp.width}x${vp.height}: ${error.message}`, { cause: error });
        } finally {
          await context.close();
        }
      }
    }
  } finally {
    await browser.close();
  }
  console.log(`Saved ${only.length * viewports.length} screenshots to`, outDir);
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
