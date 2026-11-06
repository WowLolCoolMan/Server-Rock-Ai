# Server Rock: AI – architecture (DRAFT)

Status: **draft from the refactor session's Step 0 (2026-11-06).** Sections 1–5 describe the code
as it is today (commit 80ffd99). Section 6 is the *proposed* split plan, waiting for approval.
Sections 7–8 are suggestions and bugs found while reading; nothing in them has been changed.
When the refactor finishes, this file will be rewritten to match the final layout and will get a
"Where do I find…?" index.

---

## 1. What is in this repository

Two apps share one repo:

| App | Tech | Where | Status |
|---|---|---|---|
| **Desktop app (Electron)** | HTML/CSS/vanilla JS renderer + Node main process | repo root, `electron/` | The shipping app |
| **Native app** | C# / .NET 10 / Uno Platform (WinUI XAML) | `native/` | Migration target (see `docs/NATIVE_MIGRATION.md`) |

They don't share code. The Electron app can export its chat history to a JSON file that the native
app imports (`history:export-native-migration` → `ConversationArchive.cs`).

### Top-level map

| Path | What it is |
|---|---|
| `index.html` | The whole renderer UI: rail, sidebar, chat view, composer, pages (Images, Activity, AI SuperMarket), and every `<dialog>`. Loads 4 scripts at the bottom (see §2). |
| `style.css` | All renderer styles. Owned by the UI session; rules live in `DESIGN.md`. |
| `commands.js` | Slash-command parser: `COMMANDS` list + `parseSlashCommands(text)`. Loaded by the page *and* `require()`d by tests (it exports via `module.exports` when available). |
| `app.js` | All renderer behavior, 3,318 lines, one classic script (see §3). |
| `electron/` | Main process (`main.cjs`), the preload bridge (`preload.cjs`), and small helper modules (see §4). |
| `native/` | .NET solution: `ServerRock.Core` (logic), `ServerRock.Core.Tests` (console test runner), `ServerRock.Native` (Uno UI). See §5. |
| `assets/pets/` | SVGs for the pet companion. |
| `tests/` | `ui.spec.cjs` + `regressions.spec.cjs` (Playwright against `file://…/index.html` with a fake `window.desktopModels`), `github-updates.test.cjs` (`node --test`). |
| `scripts/` | `audit-ui.cjs` (a11y/overflow sweep at 5 viewports), `audit-electron.cjs` (launches real Electron, checks the sandboxed bridge + terminal), `check-repo-hygiene.cjs`, `ui-shots.cjs` (screenshots to `.ui-shots/`). |
| `docs/` | Feature docs, research, UI references. Packaged into the app (`build.files` includes `docs/**/*.md`). |
| `.github/workflows/` | `electron-ci.yml`, `native-ci.yml`, `native-artifacts.yml`, `release.yml`. |
| `DESIGN.md`, `CLAUDE.md` | Styling rules; Claude Code project notes (imports `DESIGN.md`). |

### How to run and check

| Command | Does |
|---|---|
| `npm start` | Runs Electron (`electron/main.cjs`). |
| `npm run check:syntax` | `node --check` on the JS files listed in `package.json` (not all of them, see §7). |
| `npm test` | `node --test tests/github-updates.test.cjs` then `playwright test` (101 tests today). |
| `npm run audit:ui` / `audit:electron` | The two audit scripts above (`audit:electron` needs a display; CI uses `xvfb-run`). |
| `dotnet run --project native/ServerRock.Core.Tests/…csproj` | Native core tests (custom console runner, ~193 checks). |
| `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --framework net10.0-desktop` | Native desktop build. |

---

## 2. How the renderer talks to Electron

```
 index.html
   ├─ <script> node_modules/marked/lib/marked.umd.js     → window.marked   (Markdown)
   ├─ <script> node_modules/dompurify/dist/purify.min.js → window.DOMPurify (sanitizing)
   ├─ <script> commands.js                               → COMMANDS, parseSlashCommands
   └─ <script> app.js                                    → everything else
                 │
                 │  window.desktopModels.<method>(…)        (only bridge to the OS)
                 ▼
 electron/preload.cjs   contextBridge.exposeInMainWorld("desktopModels", {...})
                 │  ipcRenderer.invoke("<channel>", …)   request/response
                 │  ipcRenderer.send("<channel>", …)     fire-and-forget (cancel)
                 │  ipcRenderer.on("<channel>:chunk")    streaming back to the page
                 ▼
 electron/main.cjs      ipcMain.handle / ipcMain.on, all registered inside app.whenReady()
```

* The window is `contextIsolation: true`, `nodeIntegration: false`, `sandbox: true`. The page can
  only reach Node through `window.desktopModels`.
* The scripts are **classic scripts, not ES modules**: tests load the page from `file://`, and
  Chromium blocks module scripts there.
* Every bridge call is guarded with `window.desktopModels?.x?.()` in `app.js`, so the page also
  opens in a plain browser (and in tests, which install a fake `window.desktopModels`).
* Streaming: the preload adds a listener on `models:chunk`, `provider:chunk` or `chatgpt:chunk`,
  filters by `requestId`, calls the page's `onChunk`, and removes the listener when the `invoke`
  promise settles. The terminal uses one `terminal:event` stream (`terminalOnEvent` returns an
  unsubscribe function).

### IPC channels by feature

| Feature | Preload method → channel | Handler lives in `main.cjs` (today) |
|---|---|---|
| Projects / files | `pickFolder` → `projects:pick-folder`, `readProjectFiles` → `projects:read-files`, `writeProjectFile` → `projects:write-file` | `workspace.cjs` does the work (folder approval, size limits, protected files) |
| Chat export | `exportNativeMigration` → `history:export-native-migration` | save dialog + `writeProtectedFile` |
| Terminal | `terminalStart/Write/Stop/List/Snapshot/Attach` → `terminal:*`, events on `terminal:event` | `terminal-sessions.cjs` (`createTerminalManager`) |
| Provider keys | `getProviderSecretStatus` → `secrets:status`, `setProviderSecret` → `secrets:set` | `safeStorage`-encrypted `provider-secrets.enc` |
| Cloud/local chat providers | `streamProviderChat` → `provider:stream` (+ `provider:chunk`), `cancelProviderChat` → `provider:cancel`, `scanProvider` → `models:scan-provider`, `scanHosts` → `models:scan-hosts` | `provider-bridge.cjs` (OpenAI-style and Anthropic SSE) |
| ChatGPT plan | `chatgptStatus/SignIn/Models/Stream` → `chatgpt:*` (+ `chatgpt:chunk`), `cancelChatGPT` → `chatgpt:cancel` | OAuth PKCE + loopback server, `chatgpt-plan.enc` |
| Local models (Transformers.js) | `download` → `models:download`, `generate` → `models:generate` (+ `models:chunk`), `cancel` → `models:cancel` | ONNX files under `userData/models/<org>--<name>` |
| Hugging Face Hub / hosted | `searchHuggingFace` → `hf:search`, `runHuggingFaceTask` → `hf:inference`, `openHuggingFaceModel` → `models:open-hub` | `net.fetch` to huggingface.co / router |
| GitHub | `githubDefaultClientId/BeginSignIn/PollSignIn/CancelSignIn/Status/Disconnect/Repos/ResolveRepo/Clone/ProjectStatus/PushProject` → `github:*` | device-flow OAuth, `git` via `execFile` with hooks disabled |
| App updates | `githubUpdateCheck` → `github-updates:check`, `githubUpdateOpenRelease` → `github-updates:open-release` | `github-updates.cjs` |

### Where data lives (Electron)

| Data | Where |
|---|---|
| Chats, projects, settings, pet, page, model timings | renderer `localStorage["local-studio-v1"]` (API keys are stripped before saving) |
| Provider API keys, GitHub token | `userData/provider-secrets.enc` (OS `safeStorage`) |
| ChatGPT plan session | `userData/chatgpt-plan.enc` |
| Folders the user approved | `userData/authorized-project-folders.json` (`workspace.cjs`) |
| Downloaded ONNX models | `userData/models/` |

---

## 3. The renderer today: `app.js`

One 3,318-line classic script. All top-level `let`/`const`/`function` names are globals shared by
the whole page. Roughly in file order:

| Lines | Area | Main names |
|---|---|---|
| 1–47 | DOM helper + size-limited `fetch` readers | `$`, `readResponseBytesLimited`, `readJsonResponseLimited`, `readTextResponseLimited` |
| 48–91 | Saved state + shared session variables | `KEY`, `loadState`, `state`, `active`, `editingProjectId`, `terminalState`, … |
| 92–177 | Provider tables | `localHfTasks`, `mediaHfTasks`, `defaults`, `catalogProviders` |
| 178–275 | Saving + secrets migration + chat export | `persist`, `exportNativeMigration`, `getProviderSecretStatus`, `migrateAndLoadProviderSecrets` |
| 276–319 | Composer state + Send/Queue button | `activeRequest`, `messageQueue`, `updateQueueHint`, `updateSendControl` |
| 320–391 | Projects list + project dialog | `renderProjects`, `openProjectEditor`, `showProjectDialog`, `paintProjectFolders` |
| 392–598 | Sidebar chat list, chat ⋯ menu, rename dialog | `renderHistory`, `showChatContextMenu`, `closeChatContextMenu` |
| 599–775 | Pet companion + account menu | `renderPet`, `setPetPosition`, `setPetVisible`, `openPetSettings` |
| 776–959 | Chat view: open, render, branches, regenerate, fork | `openChat`, `renderMessages`, `renderBranchBar`, `regenerateLastTurn`, `newChat`, `showLocalReply` |
| 960–1045 | Settings dialog load + provider fields | `settingLoad`, `providerUsesLocalInference`, `syncProviderFields`, `refreshProviderSecretStatus` |
| 1046–1185 | GitHub status + push review dialog | `refreshGitHubStatus`, `refreshGitHubProjectAction`, `pushProjectChanges` |
| 1186–1220 | ChatGPT account (Settings) | `refreshChatGPTStatus` |
| 1221–1485 | Composer model picker + host scan + speed stats | `renderModelChoices`, `toggleModelPicker`, `scanModels`, `recordModelPerformance` |
| 1486–1546 | Settings save | `saveSettings` |
| 1547–1614 | Project dialog handlers | `saveProjectEdits`, `#projectForm` submit |
| 1615–1644 | ChatBot / Coding Agent mode menu | `updateAgentModeLabel` |
| 1645–1695 | Settings open + mobile drawer | `openSettingsDialog`, `setMobileSidebar` |
| 1696–1741 | App update check (Settings) | `checkedGitHubUpdate` |
| 1742–1930 | GitHub sign-in, repo browser, clone, disconnect | `loadGitHubRepos`, `openGitHubDialog` |
| 1931–2004 | More Settings handlers, New chat, Clear, outside-click closing | `clearChatHistory` |
| 2005–2045 | Attachments + welcome suggestions | `attachmentReachesModel`, `attachmentIgnoredNote` |
| 2046–2217 | Generation backends | `generateLocalHf`, `generateHostedHfTask`, `streamChat`, `generateImage` (ComfyUI) |
| 2218–2438 | Hugging Face search (Settings) + AI SuperMarket page | `searchHub`, `renderMarketplace`, `searchMarketplace`, `installMarketplaceModel` |
| 2439–2775 | Project knowledge retrieval + **`send()`** (the whole request pipeline) | `retrieveProjectKnowledge`, `send` |
| 2776–2887 | Slash menu, global Escape, prompt input, Send/Stop | `renderSlashMenu`, `openSlashMenu` |
| 2888–2949 | Page switching + Activity page | `setPage`, `renderActivity` |
| 2950–3117 | Project terminal (Activity page) | `renderTerminalSessions`, `handleTerminalEvent`, `startTerminalSession` |
| 3118–3138 | Rail/mobile nav, model library entry points | `openModelLibrary`, `openModelPicker` |
| 3139–3220 | Temperature slider + Images page | `renderImageGallery`, `createImageFromPage` |
| 3221–3232 | **Startup** | `settingLoad()`, `migrateAndLoadProviderSecrets()`, `renderProjects()`, `renderHistory()`, `setPage()` |
| 3234–3318 | Voice input/output | `speakReply`, `startRecognition` |

Things that run **at load time** (not just on events), which matters for any split: the state
setup (48–84), `petMoveButton = $("#petMiniMove")`, `renderPet()` (775), `updateAgentModeLabel()`
(1644), `if ($("#githubUpdateCheck"))` (1697), the `searchMarketplace = …` wrapper (2400), and the
startup block (3221–3232). There are also document-level listeners whose **order** matters:
`pointerdown` (chat menu 565, then account menu 768), `keydown` (chat menu 568, then Escape 2833),
one `click` (1988) and one `window` `resize` (726).

---

## 4. The main process today: `electron/`

| File | Lines | What it does |
|---|---|---|
| `main.cjs` | 1,669 | Window setup + **every** IPC handler. Top half: module-level helpers for secure storage, ChatGPT OAuth/streaming, Transformers.js loading, local host scanning, `createWindow`. Bottom half: one ~1,070-line `app.whenReady()` callback that builds a workspace + terminal manager, defines GitHub/git helpers as closures, and registers all channels. |
| `preload.cjs` | 85 | The `window.desktopModels` bridge (see §2). |
| `provider-bridge.cjs` | 367 | Streams chat from OpenAI-compatible and Anthropic APIs; endpoint validation; usage counting; scrubs the key from errors. |
| `workspace.cjs` | 175 | Approved project folders, bounded reads of project text files, safe relative writes, protected file names. |
| `terminal-sessions.cjs` | 221 | Project terminal sessions (spawn in an approved folder, output buffer, timeouts, attach/stop). |
| `github-updates.cjs` | 108 | Finds the newest GitHub release for this OS/arch and its installer + SHA-256. |
| `response-limits.cjs` | 91 | Size-limited readers for JSON, text and binary HTTP bodies. |
| `ipc-validation.cjs` | 70 | Validators for request IDs, provider names, chat payloads. |
| `model-path-safety.cjs` | 42 | Keeps model downloads inside the model folder (no `..`, no symlinks). |
| `child-environment.cjs` | 20 | Copies `process.env` for child processes minus secret-looking variables. |
| `bounded-file.cjs` | 23 | Size-limited text file reader for the encrypted stores. |
| `github-app.json` | – | Optional built-in GitHub App client ID (empty in the repo). |

---

## 5. The native app: `native/`

```
native/
  ServerRock.Native.sln, Directory.Build.props/.targets, Directory.Packages.props, global.json (Uno.Sdk 6.7.30)
  ServerRock.Core/            class library, no UI (net10.0)
    Core/Models/              AppSettings, ChatMessage, ChatSession, ChatAttachment, ChatSource, LocalProject
    Core/Services/            OpenAiCompatibleProvider, ModelLibraryService, WebSearchProvider
    Services/                 ComfyImageProvider, GitHubApiClient, GitHubDeviceFlow, GitHubWorkspaceService
    Infrastructure/           JSON stores (settings, conversations, projects, undo), archive import,
                              workspace file editor/context/diff/command runner/terminal, bounded IO,
                              attachment policy + PDF text extraction, GitHub credential store
  ServerRock.Core.Tests/      console program (Program.cs, ~1,200 lines) run with `dotnet run`
  ServerRock.Native/          Uno app
    App.xaml(.cs)             app + window startup
    MainPage.xaml             the single page's layout (147 lines)
    MainPage.xaml.cs          3,882 lines: fields, constructor and every UI feature (see §6.3)
    GitHubWorkspaceUi.cs      already a `partial class MainPage`: GitHub dialog + actions (617 lines)
    MarkdownUiRenderer.cs     Markdown → XAML (Markdig)
    Platforms/Desktop/Program.cs, Platforms/Windows/…
```

Native data lives in `%AppData%/ServerRock/AI/` (`settings.json`, `conversations.json`,
`projects.json`, `workspace-undo.json`) and `%LocalAppData%/ServerRock/AI/github-user-token.bin`.

---

## 6. PROPOSED split plan (not done yet)

Rules for every round: code is moved, never rewritten. Function and variable names stay the same
unless a rename is approved separately (§7). Every new file starts with a header comment: what it
does, what it depends on, what uses it. Each round must pass `npm run check:syntax`, `npm test`
(101/101 today), and for native rounds `dotnet build` + Core tests, exactly as before.

### 6.1 `electron/main.cjs` → `main.cjs` + `electron/ipc/*.cjs`

Each `ipc/*.cjs` file exports one `register…Ipc(ipcMain, ctx)` function. `main.cjs` keeps the
window, the app lifecycle, and the `whenReady` callback, which now builds `ctx` and calls the
register functions. `ctx` = `{ workspace, terminalManager }` (the only objects created at ready
time). Everything else each file needs it `require()`s itself.

| New file | Moves out of `main.cjs` | ≈ lines |
|---|---|---|
| `electron/main.cjs` (kept) | requires, `createWindow`, `whenReady` (build `ctx`, call the register functions, `createWindow`, `activate`), `window-all-closed`, `before-quit`, the module-level `terminalManager` | ~90 |
| `electron/secure-store.cjs` (helper, not IPC) | `chatGPTEncryptionReady`, `writeProtectedFile`, `quarantineUnreadableFile`, `readProviderSecrets`, `writeProviderSecret`, plus `readSecureProviderSecret` (today a closure inside `whenReady`, but it doesn't use anything created there) | ~110 |
| `electron/ipc/secrets.cjs` | `secrets:status`, `secrets:set` | ~25 |
| `electron/ipc/chatgpt.cjs` | `CHATGPT_*`, store path, `read/saveChatGPTState`, `getChatGPTProfile`, `chatGPTStatus`, `chatGPTSignIn`, `getChatGPTAccessToken`, `chatGPTModels`, `chatGPTStream`, `activeChatGPTRequests`, `chatGPTJwks`; channels `chatgpt:*` | ~330 |
| `electron/git-runner.cjs` (helper) | `runGit`, `githubAuthHeaders`, `getGitRoot`, `githubChangeSnapshot`, `MAX_GITHUB_DIFF_BYTES` | ~150 |
| `electron/ipc/github.cjs` | `readGithubClientId`, `githubFetch`, credential read/save/refresh, repo-name helpers, `normalizeGithubRepository`, `githubDeviceRequests`; channels `github:*` | ~330 |
| `electron/ipc/providers.cjs` | `scanModelHosts`, `activeProviderRequests`, `createProviderBridge` setup; channels `provider:stream`, `provider:cancel`, `models:scan-provider`, `models:scan-hosts` | ~200 |
| `electron/ipc/huggingface.cjs` | channels `hf:search`, `hf:inference`, `models:open-hub` | ~140 |
| `electron/ipc/local-models.cjs` | `MAX_MODEL_BYTES`, `getTransformers`, `modelDirectory`, `localPipelines`, `activeGenerations`, `resultAudio`; channels `models:download`, `models:cancel`, `models:generate` | ~420 |
| `electron/ipc/projects.cjs` | channels `projects:read-files`, `projects:write-file`, `projects:pick-folder`, `history:export-native-migration` | ~50 |
| `electron/ipc/terminal.cjs` | channels `terminal:*` | ~25 |
| `electron/ipc/updates.cjs` | `checkGitHubRelease`; channels `github-updates:*` | ~20 |

Notes: the order in which `ipcMain.handle` is called doesn't change behavior (each channel has one
handler), but the register calls will follow today's order anyway. Closure state that becomes module
state (`githubRefreshPromise`) is safe because `whenReady` runs once. `build.files` already includes
`electron/**/*`; only `check:syntax` needs the new files.

Proposed rounds: (a1) `secure-store` + `secrets` + `chatgpt` · (a2) `git-runner` + `github` ·
(a3) `providers` + `huggingface` · (a4) `local-models` · (a5) `projects` + `terminal` + `updates`.

### 6.2 `app.js` → ordered classic scripts in `renderer/`

**Shared state: keep the globals for now.** Recommendation and why:

* Classic scripts on one page share a single global scope. A top-level `let state` in
  `renderer/state.js` is readable and assignable from `renderer/send.js`, exactly as it is inside
  one file today. Moving code between files therefore changes no behavior, and every round stays a
  pure cut-and-paste that is easy to review in a diff.
* A `window.SR` namespace would mean rewriting nearly every line that touches shared state
  (`state.` → `SR.state.`, `active` → `SR.active`, …), hundreds of edits mixed into the moves. That is
  a large mechanical rewrite, not a move, and much harder to review.
* To keep the globals understandable, each file header lists **"Defines (globals)"** and
  **"Uses (globals from other files)"**. A move to `window.SR` can be a separate, later round if you
  still want it once the files exist.

**Constraints that set the order:** a file may only use, *at load time*, things defined in files
loaded before it (calls inside event handlers are fine). The document-level listeners keep their
current order (chat-menu listeners before the account-menu `pointerdown` and before the Escape
`keydown`). Load order = file order below. `commands.js` stays at the root (tests `require()` it).

| # | New file | What moves there (today's `app.js` lines) | ≈ lines |
|---|---|---|---|
| 1 | `renderer/util.js` | `$`, `MAX_LOCAL_*_BYTES`, `read*ResponseLimited` (1–47), `formatSize` (1225) | ~60 |
| 2 | `renderer/state.js` | `KEY`, `MAX_STATE_CHARACTERS`, `loadState`, `state` defaults (48–84), `persist` (178–212); shared session vars: `active`, `pendingBranchBase`, `activeRequest`, `activeLocalRequestId`, `activeProviderRequestId`, `pendingImageData/Name`, `messageQueue`, `providerSecretsReady` | ~110 |
| 3 | `renderer/providers.js` | `localHfTasks`, `mediaHfTasks`, `defaults`, `catalogProviders` (92–177), `getProviderSecretStatus`, `migrateAndLoadProviderSecrets` (243–275), `providerUsesLocalInference` (994), `refreshProviderSecretStatus` (1038), `attachmentReachesModel/IgnoredNote` (2005–2015) | ~170 |
| 4 | `renderer/sidebar.js` | `renderHistory`, chat ⋯ menu + its 2 document listeners, rename dialog (392–598), `setMobileSidebar` + drawer buttons (1659–1694), New chat (1961), `clearChatHistory` + Clear (1965–1980) | ~270 |
| 5 | `renderer/pets.js` | pet companion (599–735, 762–767, ends with the load-time `renderPet()`) | ~150 |
| 6 | `renderer/account-menu.js` | account menu toggle, its items, its outside-click listener (736–761, 768–774) | ~40 |
| 7 | `renderer/chat-view.js` | `openChat`, branch bar, `selectBranch`, `conversationMatches`, `regenerateLastTurn`, `forkConversation`, `renderMessages`, `newChat`, `showLocalReply` (776–959) | ~185 |
| 8 | `renderer/settings.js` | `exportNativeMigration` (213), `settingLoad`, `syncProviderFields`, ChatGPT account, `saveSettings`, open/close Settings, update check, provider select/forget key, `#provider`, temperature (960–1037, 1186–1220, 1486–1546, 1645–1658, 1695–1741, 1931–1960, 1981–1984, 3139–3145) | ~300 |
| 9 | `renderer/github.js` | GitHub status, project push button, push review dialog, sign-in, repo browser, clone, disconnect, sync (88–89, 286–288, 1046–1185, 1742–1930) | ~400 |
| 10 | `renderer/model-picker.js` | `discoveredModels` … `scanModels`, `recordModelPerformance` (1221–1485), picker buttons (1985–1987), `openModelPicker` (3132–3138) | ~270 |
| 11 | `renderer/projects.js` | `renderProjects`, editor/dialog (320–391), `editingProjectId`, `pendingProjectFolders`, dialog handlers (1547–1614), `retrieveProjectKnowledge` (2439–2473) | ~200 |
| 12 | `renderer/marketplace.js` | Settings HF search + download (2087–2105, 2218–2245, 2421–2438), AI SuperMarket page (2246–2420), `openModelLibrary` + its buttons (3124–3130) | ~250 |
| 13 | `renderer/generation.js` | `generateLocalHf`, `generateHfImage`, `generateHostedHfTask`, `streamChat`, `generateImage` (2046–2086, 2106–2217) | ~165 |
| 14 | `renderer/composer.js` | `updateQueueHint`, `updateSendControl` (289–319), mode menu (1615–1644, keeps load-time `updateAgentModeLabel()`), outside-click closing (1988–2004), attach (2016–2038), suggestions (2039–2045), Send/Stop/Enter (2867–2887) | ~140 |
| 15 | `renderer/send.js` | `send()` (2474–2775) | ~300 |
| 16 | `renderer/slash-menu.js` | descriptions, slash menu, global Escape `keydown`, prompt `input` listener (2776–2866) | ~90 |
| 17 | `renderer/pages.js` | `setPage`, `renderActivity` (2888–2949), rail + mobile nav (3118–3123), `#welcomeSettings` (3131) | ~75 |
| 18 | `renderer/terminal.js` | `terminalState` (91), terminal (2950–3117) | ~170 |
| 19 | `renderer/images.js` | Images page (3146–3220) | ~75 |
| 20 | `renderer/voice.js` | `recognition`, `voiceOn`, `voiceReply`, `speakReply`, `startRecognition`, `#voice` (276–278, 3234–3318) | ~90 |
| 21 | `renderer/boot.js` | the startup block (3221–3232); runs last | ~15 |

`app.js` shrinks each round and is deleted in the last one. While it still exists it loads *after*
all `renderer/` files, so rounds go top to bottom of the table. The one ordering change is that
`#voice`'s handler is now attached before the startup block instead of after it. Nothing happens
in between (no events fire during script loading), so behavior is the same. Each round also updates
the `<script>` tags at the bottom of `index.html` (only those), `check:syntax`, and `build.files`
(`"renderer/**/*.js"`, added once).

Proposed rounds, since 21 one-file rounds is a lot of approvals: (d1) 1–3 · (d2) 4–7 · (d3) 8–9 ·
(d4) 10–12 · (d5) 13–16 · (d6) 17–21 and remove `app.js`. Or one file per round if you prefer.

### 6.3 `native/ServerRock.Native/MainPage.xaml.cs` → partial classes

All **fields and the constructor stay in `MainPage.xaml.cs`.** Reason: the C# spec leaves the
order of field initializers *across* partial files unspecified (only within a file is it top to
bottom), so moving `new ConversationStore()` and friends to other files could change construction
order. Only methods move.

| New file | Moves (today's lines) | ≈ lines |
|---|---|---|
| `MainPage.xaml.cs` (kept) | usings, records, `SlashCommandCatalog`, fields, constructor, `MainPageLoaded`, runtime-status banner, `RunUiActionAsync`, `IsActionableRuntimeStatus`, `FriendlyError` | ~260 |
| `MainPage.Navigation.cs` | responsive sidebar, `NavigateClick`, mode switch, `AppKeyDown`, `UpdateModeControls` (166–200, 334–436) | ~150 |
| `MainPage.History.cs` | chat export/import (202–323) | ~120 |
| `MainPage.Workspace.cs` | workspace bar, undo, `SamePath`, `AttachmentConsentKey`, project file list (437–560, 713–820) | ~230 |
| `MainPage.Terminal.cs` | `OpenTerminalClick(Async)` (561–712) | ~150 |
| `MainPage.Projects.cs` | new/edit/remove/select project, folders, `SaveProjects` (872–1303), `ChooseProjectAsync` (3843) | ~440 |
| `MainPage.Sessions.cs` | new chat, recents list, rename/delete session, persist, `SaveCurrentSession` (1304–1521, 1718–1860), search (3321–3344) | ~400 |
| `MainPage.ChatFolders.cs` | chat folders (1522–1717) | ~200 |
| `MainPage.Chat.cs` | suggestions, prompt keys, `SendClick`, streaming, perf, provider messages (1861–2197), web search, temporary chat, edit/stop controls (3181–3320, 3345–3378) | ~540 |
| `MainPage.WorkspaceProposals.cs` | proposed file changes + command review (2198–2503) | ~310 |
| `MainPage.Messages.cs` | `AddMessage`, rendering, copy/edit/retry, sources (2519–2760) | ~245 |
| `MainPage.Models.cs` | model button, hotswap, pins, `ModelLibraryClickAsync`, endpoints, labels, `RefreshModelConnectionAsync`, `IsLocalEndpoint` (829–871, 2761–3180) | ~470 |
| `MainPage.Settings.cs` | Advanced, Settings dialog, `SaveSettings` (3379–3417, 3504–3537, 3869–3882) | ~90 |
| `MainPage.ImageStudio.cs` | (3418–3503) | ~85 |
| `MainPage.Attachments.cs` | (3538–3710) | ~175 |
| `MainPage.SlashCommands.cs` | commands button, prompt suggestions, `ExecuteSlashCommandAsync`, help (3711–3868) | ~160 |

Verification needs a .NET 10 SDK, which isn't installed on this machine. Options: install it in
the session scratchpad (it lives in a 7.8 GB RAM disk, which an earlier session filled), or rely
on Native CI after each push.

Proposed rounds: (b1) Navigation + History + Terminal · (b2) Workspace + WorkspaceProposals ·
(b3) Projects · (b4) Sessions + ChatFolders · (b5) Chat + Messages · (b6) Models · (b7) Settings +
ImageStudio + Attachments + SlashCommands.

---

## 7. Suggestions (not done; each needs approval)

### Dead code (with proof)

| What | Proof |
|---|---|
| `main.cjs`: `protocol` is imported from `electron` but never used | only other `protocol` is a local `const` inside `provider:stream` that shadows it |
| `main.cjs`: `const { spawn } = require("node:child_process")` unused | `spawn` appears only in `terminal-sessions.cjs` |
| `main.cjs`: `modelRoot` (in `whenReady`) is never read | 1 occurrence: its declaration |
| `main.cjs`: `githubAuthHeaders(token)` builds 6 env vars; only `.GIT_CONFIG_VALUE_1` is ever used (in `runGit`) | 2 occurrences |
| `app.js`: `providerSecretsReady` is written 3 times and never read | 4 occurrences, all assignments + declaration |
| `app.js`: in `send()`, `if (cmd.status) instruction = …` can't run | `/status` returns earlier in `send()` with a local reply |
| `package.json`: `electron-updater` dependency is never `require`d | the only hit outside `package-lock.json` is `package.json`; `github-updates.cjs` replaced it |
| `package.json`: `build.files` lists `runtime/**/*` | there is no `runtime/` folder |
| Native: `HelpClick`, `NavigateClick` handlers aren't wired in `MainPage.xaml` or code | 1 occurrence each (OLD has it: FIX item 26 "native dead handlers") |
| Native: `ModelClickAsync()` is a one-line alias for `ModelLibraryClickAsync()` | used once, by `/model` |

### Duplication

* `quarantineUnreadableFile` exists twice (`main.cjs` and `workspace.cjs`).
* The Hugging Face task lists exist 4 times: `app.js` `localHfTasks`/`mediaHfTasks`, and in
  `main.cjs` `models:generate` (`supported`, `imageTasks`, `audioTasks`) and `hf:inference`.
* The renderer's `readResponseBytesLimited` mirrors `electron/response-limits.cjs`. They can't easily
  be shared: the sandboxed renderer can't `require`.
* `searchMarketplace` is wrapped by a second function that redefines it (`marketplaceSearchImplementation`),
  so the loading state is turned on twice. Folding the wrapper in would be clearer, but it reorders
  logic, so it's a separate decision.

### Unclear or inconsistent names (rename candidates)

* `window.desktopModels` is the whole OS bridge (GitHub, terminal, files…), not just models. A name
  like `desktopBridge` would be clearer, but it touches preload, `app.js`, both test files and the audit script.
* One page, three names: "AI SuperMarket" (title), "marketplace" (`#marketplacePage`, `renderMarketplace`),
  "model library" (`openModelLibrary`, `#openModelLibrary`), and the page key `"models"`.
* `settingLoad` → `loadSettingsIntoForm`; `paintProjectFolders` → `renderProjectFolders` (everything
  else uses `render…`); `active` → `activeChat`; `out` (in generation code) → `assistantMessage`;
  `s` (settings snapshot passed to every generator) → `requestSettings`; `KEY` → `STORAGE_KEY`
  (value `"local-studio-v1"` must stay); `defaults` → `providerDefaults`; `generateImage` →
  `generateComfyImage`; `searchHub` (Settings) vs `searchMarketplace` (page) both search the HF Hub.
* `main.cjs`: `chatGPTEncryptionReady()` guards *all* encrypted secrets, not just ChatGPT →
  `secureStorageReady`. `Github` vs `GitHub` casing is mixed (`readGithubClientId`, `githubFetch` vs
  `checkGitHubRelease`, `GitHubWorkspaceUi`).
* Many one-letter locals in `app.js` (`b`, `h`, `m`, `o`, `q`, `p`, `d`, `v`).
* `electron/ipc-validation.cjs` would sit better as `electron/ipc/validation.cjs` once `ipc/` exists.

### Layout

* `native/ServerRock.Core` has both `Core/Services/` and `Services/` (same namespace
  `ServerRock.Core.Services`), and models under `Core/Models/`. Flatten to `Models/`, `Services/`,
  `Infrastructure/` (the `.csproj` globs `**/*.cs`, so it's a `git mv` only).
* `native/ServerRock.Native/GitHubWorkspaceUi.cs` is a `MainPage` partial; rename to `MainPage.GitHub.cs`.
* `native/ServerRock.Native/ReadMe.md` is the Uno template's boilerplate.
* `check:syntax` skips `electron/response-limits.cjs`, `child-environment.cjs`,
  `model-path-safety.cjs`, `scripts/check-repo-hygiene.cjs`, `scripts/ui-shots.cjs` (OLD has it:
  FIX item 24 "check:syntax coverage").

## 8. Bugs seen while reading (logged, not fixed)

* BUG: `main.cjs` `chatGPTSignIn()` reads the token-exchange error body with an unbounded
  `tokenResponse.text()`. Every other remote read goes through `readTextBodyLimited`. Low severity.
