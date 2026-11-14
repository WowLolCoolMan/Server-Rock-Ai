# Goal execution ledger

This log records evidence-backed slices for the long-running implementation
goal. It is not a claim that the full handoff package is implemented.

## Slice 270 — Restore readable code surfaces across appearances

Status: `implemented_live_electron_verified`

The Coding Workspace editor, diff preview, and chat code blocks now use
dedicated semantic code-surface tokens. Code remains on a stable dark surface
in light, dark, and system appearances, with readable source text, an accent
caret, and dark native control rendering instead of inheriting the surrounding
page text color.

Changed files:

- `apps/desktop/src/design-system/tokens.css`
- `apps/desktop/src/styles.css`

Evidence:

- `npm run build` — passed.
- `npm run typecheck` — passed.
- Live Electron rendered a real connected local project with `README.md`
  filtered and open in the Coding Workspace.
- The live screenshot showed readable README source text on the dark editor
  surface in the light application appearance.

Remaining: continue the next evidence-backed handoff slice after the shared
keyboard/focus and packaged Electron acceptance checks are audited.

## Slice 261 — Make Percy a true desktop companion overlay

Status: `implemented_native_wander_live_electron_verified`

Percy now behaves as a real desktop companion rather than a sprite that only
animates in place. The existing transparent Electron child window remains
separate from the Skai workspace, while the native main process gives it a
bounded, gentle horizontal wander loop inside the current monitor work area.
Manual dragging cancels the active walk target, persists the dropped position,
and resumes wandering after release. Reduced motion and the existing “Let the
pet walk around” preference both stop native movement as well as the sprite
animation. The sprite overlay no longer shows a permanent caption or panel;
only the completion bubble and an unobtrusive hide control are visible when
needed.

Changed files:

- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/components/overlays/PetWindow.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- `npm run typecheck` — passed.
- `npm run build` — passed; the Percy walk and success six-frame strips were
  emitted as production assets.
- `node --check electron/main.cjs` — passed.
- `node --check electron/preload.cjs` — passed.
- `git diff --check` — passed.
- Live Electron AX state exposed a separate `Percy · Skai companion` window
  with the accessible “Percy desktop companion. Drag to move.” surface.
- Native window bounds changed from `875,826` to `1112,826` during a five
  second idle observation, proving the overlay moved independently of the main
  window.

The completion-message bridge remains local-only: successful agent-host
requests are queued to the standalone pet window and rendered as a short
status bubble. No cloud transport or cloud execution was introduced.

Remaining: run a live successful local provider request to capture the bubble
in Electron, and add a native UI harness for drag/drop bounds if the desktop
test harness is expanded.

## Slice 262 — Bound the active Coding Workspace workbench

Status: `implemented_renderer_workbench_viewport_live_electron_verified`

The selected-project Coding Workspace now uses the same bounded desktop
viewport discipline as Chat. The active project header, session bar, and
project switcher stay fixed while the project browser owns the remaining
height. The explorer and editor are independent scroll regions, the terminal
dock has an explicit bounded height, and the task brief expands inside the
workbench instead of turning the entire route into a long dashboard document.
The empty project state and the existing local-only task environment contract
remain unchanged.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/features/coding/CodingPage.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- `npm run build` — passed after the viewport changes.
- `npm run typecheck` — passed.
- Live Electron rendered the active project state from a temporary local
  project with no connected folders; the project header, task brief, explorer
  empty state, and guarded editor state remained in one workbench surface.
- Live AX state exposed the task brief controls, Local selection, disabled
  Worktree option, file filter, and editor empty state without renderer errors.
- The expanded task brief was captured at the normal 1440×900 desktop window
  size; the file region remained reachable below the brief instead of being
  rendered as a second page.
- The temporary project store was moved out of the user data directory after
  verification; no user project existed before the test.

Remaining: verify the same bounded behavior with a real connected folder and
an opened file at constrained desktop widths, then continue the shared
keyboard-focus and secondary-workspace audit.

## Slice 263 — Bound the Agent Workbench viewport

Status: `implemented_renderer_workbench_viewport_live_electron_verified`

The Agent Workbench now has an independent desktop route viewport instead of
expanding into one long page. Its summary, filters, and page header remain
stable while the task list and selected-task detail panels scroll independently;
the board view and scheduled-run panel are bounded as well. Narrow windows keep
the natural single-column document flow so the route remains usable without
introducing a second desktop layout.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/features/agents/WorkbenchPage.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- Source audit confirmed the live Workbench rendered task activity, selected
  task detail, and scheduled runs as one unbounded route.
- Live Electron at the normal 1440×900 desktop size showed the Workbench with
  six tasks and three scheduled runs; the list/detail surface extended below
  the first viewport before this change.
- The rebuilt Electron list view now keeps the task list and selected-task
  detail in independent scroll regions, with the scheduled-run card visible as
  its own bounded surface below them.
- The rebuilt Electron board view keeps its eight status columns in a bounded
  board region and preserves the scheduled-run surface below it.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run check:syntax` — passed.
- `npm run audit:local-only` — passed.
- `npm run audit:electron-runtime` — passed.
- `git diff --check` — passed.

## Slice 264 — Make Settings a bounded control workspace

Status: `implemented_renderer_settings_workspace_live_electron_verified`

Settings now shares the desktop workspace discipline used by Chat, Coding, and
Agent Workbench. The route header is compact, the settings shell fills the
available desktop height, and the navigation rail and active settings panel
scroll independently instead of turning the whole route into a long form.
Category navigation now uses the shared icon system for faster scanning while
preserving searchable categories, local-only controls, and the existing save
contract. Narrow windows fall back to the existing horizontal category rail
and natural document flow.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/features/settings/SettingsPage.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- Before the change, live Settings rendered as a generic page header followed
  by an unbounded settings card.
- Rebuilt Electron at 1440×900 rendered the compact Settings route, icon-marked
  category rail, and local General controls without clipping or renderer
  errors.
- Live category navigation from General to Appearance updated the active panel
  and kept the settings workspace in the same bounded surface.
- `npm run typecheck` — passed.
- `npm run build` — passed.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

## Slice 265 — Dock the Image Studio workflow

Status: `implemented_renderer_image_studio_viewport_live_electron_verified`

Image Studio now behaves as a bounded two-pane creation workspace. The local
workflow editor and generation history share the available desktop height;
long endpoint, prompt, recipe, and workflow content scroll inside the editor,
while readiness and Generate image remain visible in a persistent action
footer. Narrow windows retain the natural stacked layout and page scrolling.
The existing local ComfyUI bridge, recipe actions, readiness states, and output
history contracts are unchanged.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/features/catalog/CatalogPages.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- Before the change, the live 1440×900 Image Studio editor extended below the
  viewport and its primary Generate image action was not visible.
- Rebuilt Electron rendered the editor and Recent outputs as a bounded pair;
  the editor scrollbar is internal and the readiness/action footer remains
  visible at the bottom of the workspace.
- Clicking the Product scene recipe populated the prompt and updated the
  readiness state to Workflow required without leaving the workspace.
- `npm run build` — passed.
- `npm run typecheck` — passed.
- `git diff --check` — passed.

## Slice 260 — Dock the Electron Chat composer

Status: `implemented_renderer_chat_viewport_live_electron_verified`

Chat now treats the conversation and composer as a desktop workspace rather
than one page-length document. The message list owns its scroll position and
restores that position per conversation, while the composer is reserved as a
bottom dock with responsive bounds and space preserved below the thread. The
shared shell now provides a bounded flex/overflow contract for route content,
and the composer remains reachable when the message history grows.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/features/chat/ChatPage.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run check:syntax` — passed.
- `npm run audit:local-only` — passed.
- `npm run audit:electron-runtime` — passed.
- `git diff --check` — passed.
- Live Electron acceptance at the normal desktop window size showed the
  composer fully visible and reachable at the bottom of the window while the
  thread remained above it; the accessibility tree exposed the message field,
  Attach, and Send controls.

Privacy/security/licensing: this is renderer-only layout and local scroll
state; no provider, network, filesystem, or permission boundary changed.

Rollback: remove the route-specific shell class, message-list scroll ref, and
Chat viewport rules to restore the prior page-flow layout.

Remaining: wider packaged visual comparison at all target sizes, attachment
selection preview acceptance, and the remaining Coding Workspace and
secondary-surface fidelity gaps remain open.

## Slice 259 — Add reusable local attachment previews

Status: `implemented_renderer_attachment_preview_build_and_picker_verified`

Checkpoint: this commit on `main`.

Electron chat attachments now use one reusable attachment-card component in
both the composer and message history. Image attachments render bounded local
thumbnails, text files show a short local-content preview, PDFs identify that
text was extracted locally, and other files show a type mark. Composer cards
retain accessible remove actions, while message cards stay read-only.

Changed files:

- `apps/desktop/src/features/chat/AttachmentList.tsx`
- `apps/desktop/src/features/chat/ChatMessage.tsx`
- `apps/desktop/src/features/chat/ChatPage.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run check:syntax` — passed.
- `npm run audit:local-only` — passed.
- `npm run audit:electron-runtime` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run smoke:pdf-attachment` — passed bounded invalid-content and
  protected-name rejection assertions.
- Live Electron acceptance confirmed the local attachment picker opens and
  cancels cleanly without leaving a dialog or mutating chat state.
- `git diff --check` — passed.

Privacy/security/licensing: previews use the already validated local picker
payload; no upload, cloud route, dependency, or new permission was added.

Rollback: remove `AttachmentList.tsx` and restore the two existing filename
strips; attachment selection and host validation remain independent.

Remaining: live selection of a representative image/PDF in the packaged
renderer, richer preview/editing acceptance, and broader RAG coverage remain
separate slices.

## Slice 258 — Add opt-in Electron memory injection

Status: `implemented_host_renderer_opt_in_memory_smoke_verified`

Electron settings now persist an explicit “Inject approved memory into chat”
toggle. When enabled, the host loads only enabled global or active-project
entries through `MemoryPromptContext`, bounds the injected section, and marks it
as untrusted reference material. The default remains disabled; no implicit
learning or automatic memory capture was introduced.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/src/App.tsx`
- `scripts/smoke-memory.cjs`

Evidence:

- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run smoke:memory` — passed settings persistence plus explicit memory
  save, search, disable, export, and removal assertions.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm test` — passed the Core memory injection safety tests and the full native
  Core suite.
- `git diff --check` — passed.

Privacy/security/licensing: injection is local, opt-in, bounded, scoped, and
framed as untrusted reference data. It does not route memory to a provider
unless the user sends a chat request with the toggle enabled.

Rollback: disable the preference or remove the host injection block; memory
management and ordinary chat remain available.

Remaining: live provider acceptance of the injected system context and memory
import UI remain separate slices.

## Slice 257 — Port explicit Electron local memory management

Status: `implemented_host_renderer_memory_smoke_verified`

Electron now exposes the Core memory store through a dedicated Memory view.
Users can explicitly save bounded notes with scope/tags, search enabled notes,
disable or remove entries, and export a validated JSON backup. The host keeps
the operation local and approval-labelled; no implicit learning or prompt
injection was added by this slice.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`
- `scripts/smoke-memory.cjs`
- `package.json`

Evidence:

- `npm run check:syntax` — passed.
- `npm run smoke:memory` — passed disposable save, search, disable, export,
  and removal assertions.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors after
  isolating a verified orphaned host process lock.
- `git diff --check` — passed.

Privacy/security/licensing: memory remains local, explicit, bounded, and
user-controlled. Backup export is initiated by the user and no cloud route was
added.

Rollback: remove the memory routes, bridge allowlist entries, Memory view, and
smoke command; existing chat and settings remain functional.

Remaining: opt-in memory injection into Electron chat, memory import UI, and
full memory acceptance coverage remain separate slices.

## Slice 256 — Expand Electron workspace customization options

Status: `implemented_renderer_settings_host_persistence_build_verified`

The Electron settings surface now exposes the existing preference contract for
accent color, interface density, deep-sea and deep-space backdrops, reduced
motion, and developer diagnostics. Preferences are applied live through data
attributes and persisted by the .NET host; backdrop animation is disabled by
the existing reduced-motion setting.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`
- `services/agent-host/Protocol/AgentHostLoop.cs`

Evidence:

- `node --check apps/desktop/electron/main.cjs` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Privacy/security/licensing: settings remain local and use existing host
persistence; no remote assets, third-party runtime, or new permission was
added.

Rollback: remove the added settings fields, data attributes, and preset CSS;
the existing dark/light controls remain available.

Remaining: visual screenshot acceptance, native parity for every preset, and
additional theme customization remain separate validation slices.

## Slice 255 — Port bounded Electron chat text/image attachments

Status: `implemented_renderer_bridge_host_provider_build_verified`

Electron now exposes an explicit file picker for approved text and image
attachments. The main process reads only selected files, caps selection at five
files and two megabytes per file, and returns content through the isolated
bridge; the host/provider path remains responsible for canonical extension,
secret-name, byte, image-signature, and aggregate-content validation. User-turn
attachments are passed into `chat.complete` and retained in local conversation
history.

Changed files:

- `apps/desktop/electron/main.cjs`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/App.tsx`

Evidence:

- `node --check apps/desktop/electron/main.cjs` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Privacy/security/licensing: selection is explicit, files are not uploaded by
the picker itself, and existing provider consent/attachment policy remains the
authority before a model request. No new dependency or web capability was
introduced.

Rollback: remove the picker branch, attachment state, and attachment contract;
plain text chat remains unchanged.

Remaining: PDF extraction, queued-message attachments, and a live provider
acceptance test remain separate slices.

## Slice 254 — Port Electron conversation archive, export, and fork controls

Status: `implemented_host_renderer_archive_export_fork_smoke_verified`

The replacement host now accepts `includeArchived` for conversation listing and
returns archive and folder metadata consistently. Electron exposes an explicit
archived-chat view plus Archive/Restore actions, while active history continues
to hide archived conversations by default. The host also serializes a bounded
Markdown export and the renderer downloads it without filesystem access. Core's
bounded fork service is now reachable from Electron for independent full-chat
or message-prefix branches.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/src/App.tsx`
- `apps/desktop/shared/contracts.ts`
- `scripts/smoke-conversation-metadata.cjs`

Evidence:

- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run smoke:conversation-metadata` — passed disposable labels,
  archive persistence, active filtering, archived-view, and Markdown export
  assertions plus persisted message-prefix fork assertions.
- `git diff --check` — passed.

Remaining: archived search filtering, bulk archive actions, folder export
parity, and selecting a specific message prefix from the renderer remain
separate slices.

## Slice 251 — Port the durable Electron chat message queue

Status: `implemented_host_renderer_queue_smoke_verified`

The replacement host now exposes bounded `chat.queue.list`,
`chat.queue.enqueue`, and `chat.queue.remove` routes backed by the Core
`ChatMessageQueueStore`. The renderer can type while a response is active,
queue the follow-up explicitly, view queued FIFO messages, and remove one by
index. Queue state is persisted locally and never sent to a provider until a
future dispatch flow consumes it.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/src/App.tsx`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/styles.css`
- `scripts/smoke-chat-queue.cjs`
- `package.json`
- `.github/workflows/electron-ci.yml`

Evidence:

- `npm run check:syntax` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors after
  isolating a stale smoke process lock.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run smoke:chat-queue` — passed disposable persistence, FIFO enqueue,
  and indexed removal assertions.
- `git diff --check` — passed.

Privacy/security/licensing: queued text stays in the explicit local queue
store, uses Core bounds, and is not silently dispatched or routed to a model.
No dependency or network capability was added.

Rollback: remove the queue routes, renderer controls, smoke command, and CI
step; normal synchronous chat remains available.

Remaining: queue consumption after responses, attachments in queued turns,
fork/export workflows, RAG, and final replacement retirement gates remain
open.

## Slice 250 — Add Electron conversation folder and tag controls

Status: `implemented_renderer_metadata_controls_build_verified`

The Electron active-chat header now exposes bounded folder and comma-separated
tag fields with an explicit Save labels action. The controls use the already
verified `conversation.metadata.save` host route, and the renderer restores
the saved labels when selecting or reloading a conversation.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/styles.css`

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run smoke:conversation-metadata` — passed pin, folder, tag, and
  persistence assertions through the host.
- `git diff --check` — passed.

Privacy/security/licensing: labels remain local conversation metadata and are
sent only to the local agent host; no provider or network behavior changed.

Rollback: remove the header fields and local contract property; the existing
Pin/Unpin control and host metadata route remain available.

Remaining: archive UI, richer folder/tag navigation, forks/export, queued
messages, attachments/RAG, and final replacement retirement gates remain open.

## Slice 249 — Add bounded Electron conversation organization

Status: `implemented_host_metadata_pin_smoke_verified`

The replacement chat now exposes `conversation.metadata.save`, with Core-backed
bounds for pin state, archive state, folder names, and tags. The renderer has a
visible Pin/Unpin control for the active conversation. A disposable smoke
verifies metadata persistence through the host and the existing conversation
store; the route is also available for the remaining folder/tag/archive UI.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`
- `scripts/smoke-conversation-metadata.cjs`
- `package.json`
- `.github/workflows/electron-ci.yml`

Evidence:

- `dotnet build services/agent-host/ServerRock.AgentHost.csproj` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run smoke:conversation-metadata` — passed pin, folder, tag, and
  persistence assertions against disposable paths.
- `git diff --check` — passed.

Privacy/security/licensing: metadata changes are local-only, bounded, and
validated by the existing conversation archive/store before atomic persistence.
No message content or external provider route is introduced.

Rollback: remove the metadata route, pin control, smoke command, and workflow
step; existing history navigation and search remain available.

Remaining: full folder/tag/archive management UI, forks/export, queued
messages, attachments/RAG, and final replacement retirement gates remain open.

## Slice 248 — Add bounded Electron conversation search

Status: `implemented_host_renderer_search_smoke_verified`

The Electron replacement now exposes `conversations.search`, backed by the
existing Core `ConversationSearchService`. Search is bounded and local-only;
it matches conversation titles, messages, project/folder labels, tags,
attachment names, and source metadata. The renderer adds a history search
field and navigates matching saved conversations without exposing filesystem
access or provider data.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`
- `scripts/smoke-conversation-search.cjs`
- `package.json`
- `.github/workflows/electron-ci.yml`

Evidence:

- `dotnet build services/agent-host/ServerRock.AgentHost.csproj` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run smoke:conversation-search` — passed persisted message matching
  and bounded display assertions.
- `git diff --check` — passed.

Privacy/security/licensing: search runs over the explicitly persisted local
history through Core bounds; it adds no external search or cloud routing.

Rollback: remove the search route, field, smoke command, and workflow step;
existing conversation history navigation remains available.

Remaining: folders/tags/pins/forks/export controls, queued messages,
attachments/RAG, and final replacement retirement gates remain open.

## Slice 247 — Port reviewed project-file editing to Electron

Status: `implemented_host_renderer_write_undo_smoke_verified`

The replacement project browser now supports a reviewed text-file editing
journey. `project.file.read` returns the bounded content and current hash;
`project.file.write` requires that hash and creates a durable local undo
receipt; `project.file.undo` restores the approved prior content. The renderer
shows an editor only for a selected safe file and keeps all filesystem access
behind the allowlisted host bridge.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- `dotnet build services/agent-host/ServerRock.AgentHost.csproj` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run smoke:project-editor` — passed hash-checked write, readback, and
  undo assertions against disposable paths.
- Full native Core checks — passed.
- `git diff --check` — passed.

Privacy/security/licensing: writes remain explicit and bounded by the Core
workspace editor, which enforces path containment, protected-file rules,
content limits, and optimistic concurrency hashes. No dependency or cloud
route was added.

Rollback: remove the two mutation routes and editor controls; metadata browser
and read-only preview remain available.

Remaining: multi-file tabs, richer conflict review, terminal history, Git
review, attachments/RAG, and final replacement retirement gates remain open.

## Slice 245 — Add an isolated Electron project-terminal smoke gate

Status: `implemented_disposable_terminal_smoke_verified`

The Electron project terminal now has a repository-level disposable smoke
command. It saves a temporary project, runs one platform-appropriate explicit
command through `project.command.run`, and verifies successful output, exit
status, timeout state, and project-root execution. The smoke creates and
removes only its own temporary directory and is part of Electron CI.

Changed files:

- `scripts/smoke-project-terminal.cjs`
- `package.json`
- `.github/workflows/electron-ci.yml`

Evidence:

- `npm run check:syntax` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run smoke:project-terminal` — passed explicit command and output
  round-trip assertions.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the command is explicitly user-triggered, runs in
the selected project root, removes sensitive-shaped environment variables,
and enforces Core time/output bounds. No dependency or network capability was
added.

Rollback: remove the terminal route, UI, smoke command, and workflow step;
project browsing and preview remain available.

Remaining: richer terminal history/session behavior, mutation-aware editing,
Git review, attachments/RAG, and final replacement retirement gates remain
open.

## Slice 246 — Add the bounded Electron project terminal route

Status: `implemented_host_renderer_terminal_build_verified`

The replacement host now advertises `project.command.run`. The renderer has a
dedicated Terminal route with project selection, relative working-directory
input, explicit Run action, timeout/truncation status, and bounded output. Core
`WorkspaceCommandRunner` owns shell selection, containment, sensitive
environment filtering, timeout, and output limits.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`

Evidence from the pushed slice:

- Agent-host build — passed.
- Electron renderer typecheck/build — passed.
- Terminal stdio smoke — passed.
- `git diff --check` — passed.

Privacy/security/licensing: no shell access is exposed to the renderer; only
the allowlisted host method can launch the bounded Core command runner.

Rollback: remove `project.command.run` and the Terminal route; the rest of the
project workbench remains functional.

Remaining: interactive session persistence, command history, Git review,
attachments/RAG, and final replacement retirement gates remain open.

## Slice 243 — Add an isolated Electron project-browser smoke gate

Status: `implemented_disposable_browser_smoke_verified`

The project browser now has a repository-level disposable smoke command. It
creates a temporary workspace containing a readable text file, a protected
`.env`, and an excluded dependency directory; saves a project through the
host; requests `project.files`; and verifies the safe file list and exclusion
summary. The smoke uses an explicit temporary project store and removes only
its own directory.

Changed files:

- `scripts/smoke-project-browser.cjs`
- `package.json`
- `.github/workflows/electron-ci.yml`

Evidence:

- `npm run check:syntax` — passed.
- `npm run smoke:project-browser` — passed project round trip, filtering, and
  exclusion-summary assertions.
- `git diff --check` — passed.

Privacy/security/licensing: the fixture is disposable, never uses the user
profile, and asserts that protected content is not returned. No dependency or
network capability was added.

Rollback: remove the smoke command and workflow step; runtime behavior is
unchanged.

Remaining: preview/editing acceptance, terminal, Git review, attachments/RAG,
and the broader replacement parity and Uno-retirement gates remain open.

## Slice 244 — Port bounded project-file preview to Electron

Status: `implemented_host_renderer_preview_build_verified`

The replacement host now exposes `project.file.read` for a selected project
and relative file. It reuses the Core workspace editor’s containment,
protected-name, extension, UTF-8, and size checks. The renderer opens previews
from the bounded browser and displays text only through the host bridge.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`

Evidence from the pushed slice:

- Agent-host build — passed.
- Electron renderer typecheck/build — passed.
- Disposable README preview request — passed.
- `git diff --check` — passed.

Privacy/security/licensing: preview requests are constrained to persisted
project roots and return bounded text; the renderer has no direct filesystem
or Node access.

Rollback: remove `project.file.read` and the preview panel; the metadata-only
browser remains available.

Remaining: mutation-aware editing, multi-file tabs, terminal, Git review,
attachments/RAG, and final replacement retirement gates remain open.

## Slice 242 — Port the bounded project browser to Electron

Status: `implemented_host_renderer_safety_smoke_verified`

The Electron replacement now exposes `project.files` through the persistent
host. It reuses `WorkspaceFileContext.EnumerateTextFileBrowser`, returning only
relative paths, byte counts, and the existing safe-boundary summary. Protected
files, dependency/build directories, linked paths, and the 128-entry limit
remain enforced by Core. The renderer can open a selected project and inspect
the bounded file list without receiving file contents or direct filesystem
access.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `services/agent-host/README.md`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`

Evidence:

- `dotnet build services/agent-host/ServerRock.AgentHost.csproj` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- Disposable stdio smoke verified project-file round trip, protected-file and
  dependency filtering, and the bounded browser summary.
- `git diff --check` — passed.

Privacy/security/licensing: the route reads only explicitly selected project
folders through the existing Core safety boundary and returns metadata only;
no file content, credential, or external network capability was added.

Rollback: remove `project.files` and the renderer browser; persisted project
metadata and the legacy browser remain readable.

Remaining: file preview/editing, multi-file tabs, terminal, Git review,
attachments/RAG, and the remaining replacement parity gates remain open.

## Slice 241 — Enforce the Electron replacement boundary

Status: `implemented_audit_and_build_verified`

The replacement runtime now has a repository audit that fails when root
commands drift back to Uno or when Electron loses its sandbox, disabled Node
integration, context isolation, or preload-only renderer boundary. The audit
is part of the Electron replacement CI workflow alongside renderer and host
builds and disposable protocol smoke.

Changed files:

- `scripts/audit-electron-runtime.cjs`
- `package.json`
- `.github/workflows/electron-ci.yml`

Evidence:

- `npm run check:syntax` — passed.
- `npm run audit:electron-runtime` — passed.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the audit is read-only and adds no runtime
permissions, dependencies, or network behavior.

Rollback: remove the audit command and workflow; the runtime behavior remains
unchanged.

Remaining: complete replacement feature parity, packaged startup, accessibility,
release signing, data migration, and final Uno retirement gates remain open.

## Slice 240 — Make Electron the default desktop runtime

Status: `implemented_default_runtime_and_ci_verified`

Root `start`, `build`, and `open` commands now target the Electron/React
renderer and persistent .NET agent host. The previous Uno entry points remain
available only as explicit `start:legacy-native`, `build:native`, and
`open:legacy-native` commands so rollback and comparison remain possible while
parity is completed. A dedicated workflow runs the replacement build and
protocol smoke on pushes and pull requests.

Changed files:

- `scripts/start-desktop.cjs`
- `scripts/open-desktop.cjs`
- `.github/workflows/electron-ci.yml`
- `docs/ELECTRON_DEFAULT_RUNTIME.md`
- `package.json`

Evidence:

- `npm run build` — passed through the Electron renderer build.
- `npm run desktop:typecheck` — passed.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run smoke:agent-host` — passed against disposable profile paths.
- `git diff --check` — passed.

Privacy/security/licensing: the default runtime continues to use the
sandboxed renderer and local host boundary; no user profile or legacy data was
deleted and no dependency was added.

Rollback: use the explicit legacy-native commands while a replacement parity
gate is being repaired.

Remaining: full Electron parity, packaged startup, accessibility, release
signing, data migration, and Uno retirement gates remain open.

## Slice 239 — Add an isolated Electron host replacement smoke gate

Status: `implemented_protocol_smoke_script_verified`

The repository now has `npm run smoke:agent-host`, a bounded JSON-lines smoke
that starts the built .NET host with disposable settings and conversation
paths. It verifies the runtime capability advertisement, empty-history
precondition, conversation save/list round trip, and settings get/save round
trip, then removes only its own temporary directory. This makes the Electron
replacement boundary executable in CI/local verification without reading or
mutating the user's profile.

Changed files:

- `scripts/smoke-agent-host.cjs`
- `package.json`
- `docs/ELECTRON_MIGRATION_CUTOVER.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run check:syntax` — passed, including the new script.
- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run smoke:agent-host` — passed health, disposable conversation
  persistence, and disposable settings persistence.
- `git diff --check` — passed.

Privacy/security/licensing: the script sets explicit temporary profile paths,
does not use credentials or providers, and removes only its own temporary
directory. No dependency or third-party code was added.

Rollback: remove the smoke script and package command; runtime behavior and
user data are unaffected.

Remaining: CI wiring, packaged Electron startup, full feature parity,
accessibility, release signing, and Uno retirement gates remain open.

## Slice 238 — Add explicit retry for interrupted agent tasks

Status: `implemented_core_tests_verified`

Blocked tasks created by startup reconciliation can now be explicitly
re-queued through `AgentTaskArtifactStore.RetryBlocked`. The transition clears
stale result labels, records that retry was approved locally, and never starts
an executor. Tasks with retained worktrees are refused until the worktree is
reviewed or released, preventing implicit reuse of an unresolved filesystem
state.

Changed files:

- `native/SkaiDesktop.Core/Services/AgentTaskArtifactStore.cs`
- `native/SkaiDesktop.Core.Tests/AgentTaskArtifactTests.cs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including restart, explicit retry, and no-dispatch assertions.
- `git diff --check` — passed.

Privacy/security/licensing: retry changes only bounded local task metadata;
no model, tool, process, network, or worktree deletion is started. No
dependency or third-party code was added.

Rollback: remove `RetryBlocked`; blocked tasks remain reviewable and existing
startup reconciliation remains safe.

Remaining: renderer retry/approval controls, actual model/tool dispatch,
parallel isolation, background execution, and cross-platform acceptance remain
open.

## Slice 237 — Add selectable Electron chat history

Status: `implemented_renderer_history_navigation_build_verified`

The Electron sidebar now presents up to eight bounded saved conversations,
highlights the active conversation, restores a selected conversation's user and
assistant messages, and provides an explicit New chat action. Saving a response
updates the local history list without changing the host protocol or provider
routing. This makes the durable chat route a navigable user journey rather than
an implicit startup-only restore.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`
- `docs/ELECTRON_MIGRATION_CUTOVER.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the sidebar renders only conversation metadata and
the bounded messages already returned by the local host; no new filesystem or
network capability was introduced.

Rollback: remove the history panel and selection handlers; durable host storage
and the existing chat route remain unchanged.

Remaining: folders/tags/pins/search/forks/export, attachments/RAG, queue,
memory, comparison, and broader Electron parity remain open.

## Slice 236 — Port durable chat history to the Electron host

Status: `implemented_host_persistence_renderer_build_verified`

The replacement shell now has a real durable chat-history journey. Protocol v1
adds `conversations.list` and `conversation.save`, backed by the existing
bounded `ConversationStore`; the renderer restores the newest non-archived
conversation on host connection, saves the user turn before provider dispatch,
and saves the completed assistant response afterward. A disposable
`SRAI_CONVERSATIONS_PATH` seam keeps protocol smoke isolated from the user
profile. Provider routing remains unchanged and no automatic cloud fallback is
introduced.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `services/agent-host/README.md`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/App.tsx`
- `docs/ELECTRON_MIGRATION_CUTOVER.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build services/agent-host/ServerRock.AgentHost.csproj --no-restore` — passed with 0 warnings and 0 errors.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.
- Disposable live stdio smoke returned an empty history, saved one conversation, and returned that same conversation from a second list request.

Privacy/security/licensing: only explicitly supplied local conversation text is
persisted; the renderer remains sandboxed, the host uses Core validation, and
the disposable smoke path avoids the real profile. No dependency or third-party
code was added.

Rollback: remove the two conversation protocol routes and renderer restore/save
calls; existing Core conversation files remain readable by the legacy and
replacement boundaries.

Remaining: chat folders/tags/pins/search/forks/export, attachments/RAG, queue,
memory, model comparison, and full Electron settings/provider parity remain
open.

## Slice 235 — Approve Electron replacement boundary and port persisted projects

Status: `implemented_host_protocol_renderer_build_verified`

The owner decision is now explicit: Electron/React plus the persistent .NET
agent host will fully replace the Uno/XAML shell. This slice moves the first
real persisted application journey across that boundary: `projects.list` reads
the existing bounded Core `ProjectStore`, the renderer displays the actual local
projects, and the host advertises the capability through protocol v1. The
renderer remains sandboxed and receives no direct filesystem access.

Changed files:

- `services/agent-host/Protocol/AgentHostLoop.cs`
- `services/agent-host/README.md`
- `apps/desktop/shared/contracts.ts`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/styles.css`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/ELECTRON_MIGRATION_CUTOVER.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build services/agent-host/ServerRock.AgentHost.csproj --no-restore` — passed with 0 warnings and 0 errors.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.
- Live stdio protocol smoke returned `runtime.health` with `projects.list` advertised and `projects.list` returned a valid empty array without modifying local data.

Privacy/security/licensing: the project list is read locally through the
existing Core store, no project file contents are opened, and the renderer has
no Node.js or direct filesystem capability. No dependency or third-party code
was added.

Rollback: remove the `projects.list` host route and renderer view wiring; the
existing Core store and Uno compatibility surface remain unchanged.

Remaining: Electron parity is far from complete. Chat persistence, attachments,
file/editor/terminal/Git workflows, agents, approvals, skills, MCP, settings,
accessibility, packaging, and all Uno retirement gates remain open.

## Slice 234 — Persist project-tree expansion state

Status: `implemented_core_tests_native_build_audit_verified`

The Coding Agent project browser now persists a bounded list of expanded folder
paths per local project. On reload it restores only folders still present in
the current bounded text-file view; invalid, control-character, traversal,
oversized, and over-limit settings are normalized away. Projects without saved
state retain the existing top-level-folder default. Folder toggles save through
the normal settings store, while provider context sampling and file-content
access remain unchanged.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including settings round-trip coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 322 contracts.
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed.
- `npm run audit:future-features` — passed with 160 items and 25 checked.
- `git diff --check` — passed.

Privacy/security/licensing: only local non-secret folder paths are persisted;
path traversal and linked/content access are not introduced. No dependency or
third-party code was added.

Rollback: remove the new settings property and restore the existing default
folder-expansion branch; unknown JSON fields remain ignored by the serializer.

Remaining: full repository-tree loading, ignore-state presentation, tab
reordering, conflict review, and packaged/live acceptance remain open.

## Slice 233 — Reconcile interrupted agent tasks at startup

Status: `implemented_core_tests_native_build_audit_verified`

Startup recovery now reconciles persisted agent tasks that were left in
`running` state by an application stop. It durably marks them `blocked`, adds a
bounded review reason, and surfaces an Activity & tasks warning. The operation
never retries a handler, dispatches a provider/tool/process, deletes a retained
worktree, or silently clears local task evidence. Repeated startup loads are
idempotent.

Changed files:

- `native/SkaiDesktop.Core/Services/AgentTaskArtifactStore.cs`
- `native/SkaiDesktop.Core/Services/StartupRecoveryService.cs`
- `native/SkaiDesktop.Core.Tests/StartupRecoveryTests.cs`
- `native/ServerRock.Native/MainPageServices.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including interrupted-task and idempotence checks.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 322 contracts.
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local task metadata is read and
updated; no external work is started and retained worktrees are preserved for
explicit review. No dependency or third-party code was added.

Rollback: remove the startup reconciliation call and result field; existing
task files remain readable and no external side effects need reversal.

Remaining: explicit retry/approval UX, actual model/tool dispatch, parallel
specialist isolation, and background task execution remain open.

## Slice 232 — Expose bounded project-browser safety summary

Status: `implemented_core_tests_native_build_audit_verified`

The native project browser now uses a content-free browser snapshot that keeps
the existing 128 readable-text-file bound while reporting whether protected
files, dependency/build directories, linked paths, or the entry limit affected
the view. The sidebar header reports the number of shown files, and its
accessible help text explains the observed exclusions without naming or
disclosing protected paths. Provider context sampling remains on its original
separate 16-file/48k-character path.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/WorkspaceFileContext.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed after updating its source-contract check.
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the browser still does not read file contents,
protected names are counted but never surfaced, linked paths remain excluded,
and no dependency or network behavior changed.

Rollback: restore the prior `EnumerateTextFiles` call and header help text; no
persistent data or provider payloads are affected.

Remaining: full tree mutation, richer preview lifecycle, conflict review, and
packaged/live acceptance remain open under the project workbench requirement.

## Gate 0 — live capability audit

Completed at commit `3e34156` before the next implementation slice.

- Current branch: `main`, synchronized with `origin/main`.
- The user-provided `ServerRock_AI_FINAL_COMPLETE_HANDOFF/` directory is
  untracked input and was preserved without modification.
- The 126-row combined register was inspected; it contains 86 premium
  requirements and 40 uploaded remediation issues, all marked
  `requires_live_repo_recheck`.
- A conservative domain capability matrix and dependency order are recorded in
  [`CAPABILITY_MATRIX.md`](CAPABILITY_MATRIX.md).
- No handoff document was treated as proof of implementation or release.

The next slice is the durable operation/task state contract, because it is a
dependency for workflow execution, activity visibility, cancellation,
reconciliation, parallel agents and recovery.

## Baseline

- Repository: `WowLolCoolMan/skai-desktop`
- Branch: `main`
- Baseline commit: `3e34156` (`feat: add bounded chat tags`)
- Working tree: clean apart from the preserved untracked handoff directory
- Host: Windows `10.0.26200`, x64
- SDK: .NET `10.0.401`
- Architecture preserved: Uno Platform/XAML native app with C#/.NET 10 Core

## Slice 1 — persisted light/dark theme

Status: `implemented_unverified`

Requirement alignment: the handoff targets light/dark themes and the live README
previously identified dark mode as unavailable. This slice adds a persisted
light/dark choice to Settings → Appearance, applies the selected native theme
to the application root and standardized dialogs, and switches the shared
Skai Desktop design-token palette without changing provider or workspace data.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/UiTheme.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `README.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed (`Native core checks passed.`).
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Live validation note: Windows accessibility inspection confirmed the bounded
checkbox picker and its local-only disclosure. A manual coordinate journey
created a task but persisted an empty dependency list, so live dependency
selection is deliberately not claimed as verified. The implementation now
captures selected IDs at `ContentDialog.PrimaryButtonClick` before Uno closes
the dialog; a deterministic packaged/UI harness journey remains required.

Not yet verified:

- Live packaged Windows visual acceptance for light/dark switching.
- Linux/Skia visual acceptance and accessibility behavior.
- Every non-standardized dialog inheriting the selected theme at runtime.

Known repository gate:

- `npm run audit:native-ui` currently fails on ten existing focus/recovery
  contract checks covering project cancellation/focus, terminal keyboard
  navigation, GitHub retry/cancellation state, and command-review focus. Those
  failures are outside this theme slice and remain open for a later bounded
  UI-reliability slice.

Privacy/security/licensing: no new dependencies, network calls, credentials,
or external code were added. Theme state remains a non-secret local setting.

Rollback: revert the slice commit; the new `ThemeMode` property is additive and
older settings files continue to load as light mode.

## Slice 2 — Windows line-ending-safe native UI audit

Status: `verified_windows` (static contract audit only)

The native UI audit reported ten missing multiline contracts. Inspection showed
the source already contained the required contracts, but the checker compared
LF-only strings against CRLF files. The audit now normalizes CRLF to LF for
XAML and C# inputs before matching, preserving the same contract definitions
while removing Windows false negatives.

Changed file:

- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`

This improves evidence quality only; it does not replace live Windows/Linux UI
acceptance or accessibility testing. No new dependencies, network calls,
credentials, or runtime behavior were added.

Next unblocked slice: inspect the handoff-to-repository feature map and choose
the smallest missing user-facing capability that can be implemented with a
real backend and focused tests.

## Slice 3 — pinned conversations

Status: `implemented_unverified`

The handoff calls for pinned chats alongside folders and tags. Saved
conversations now persist a `Pinned` flag, pinned conversations sort above
other conversations within their mode/list, and the chat actions menu exposes
Pin/Unpin with truthful status text and focus-preserving rerender behavior.

Evidence:

- Core persistence test covers the pinned state through save/reload.
- Native UI build and full Core suite were green before this additive model/UI
  change; the focused suite will be rerun before commit.
- No new dependency, network call, credential, or external code was added.

Not yet verified: live Windows/Linux visual and keyboard acceptance of the pin
menu and ordering. Tags remain a separate future slice.

## Slice 4 — bounded chat tags

Status: `implemented_unverified`

Saved chats now support up to eight local-only tags, each limited to 32
characters. The existing chat action menu can edit tags, normalizes duplicate
and empty entries, shows tags under the chat title, and persists them through
the existing validated conversation store/archive path. Invalid imported or
saved tag lists are rejected before replacing healthy history.

Evidence:

- Core persistence test covers two tags through save/reload.
- Conversation storage validates tag count, length, control characters, and
  case-insensitive duplicates before atomic write.
- No new dependency, network call, credential, or external code was added.

Not yet verified: live Windows/Linux visual, keyboard, and screen-reader
acceptance of the tag editor and tag rendering.

## Slice 5 — durable operation state and journal foundation

Status: `implemented_unverified`

`SkaiDesktop.Core` now has a small UI-independent operation contract with stable
run IDs, bounded instructions, explicit state transitions, monotonic per-run
event sequences, atomic JSON persistence, and restart reconciliation. In-flight
runs are changed to `needs_reconciliation` after restart; the journal never
replays uncertain Git, shell, provider, or other side effects automatically.
This is the foundation for a later workflow/task UI and parallel agent host.

Changed files:

- `native/SkaiDesktop.Core/Models/OperationRun.cs`
- `native/SkaiDesktop.Core/Infrastructure/OperationJournal.cs`
- `native/SkaiDesktop.Core.Tests/OperationJournalTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence, privacy, and rollback will be appended after the focused suite and
native build complete. No external code, network call, credential, or
destructive data migration was introduced.

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including operation creation, monotonic transitions, invalid-transition rejection, restart reconciliation, idempotence, and explicit cancellation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the journal stores local task metadata and bounded
user instructions only; it adds no network traffic, credentials, third-party
code, or automatic side-effect replay. Atomic writes preserve the previous
journal if the process fails during a write.

Rollback: remove the additive operation journal files and test registration;
existing conversation, project, and settings stores remain unchanged.

Next dependency: expose durable operation state through an activity surface,
then add workflow execution only after approval, cancellation, and
reconciliation are visible to the user.

## Slice 6 — durable operation service in the chat journey

Status: `implemented_unverified`

The chat send flow now uses `OperationService` to create a durable operation,
record validating/queued/running transitions, and record completion, provider
failure, timeout, or user cancellation. The UI remains a client of the Core
service; it does not write journal events or choose transition rules directly.

Changed files:

- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/SkaiDesktop.Core.Tests/OperationServiceTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including service start, completion, cancellation, and the existing Core suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: operation instructions remain local bounded
metadata and use the existing atomic journal; no new network call, credential,
third-party code, or automatic side-effect replay was introduced.

Rollback: remove the service, test, and chat lifecycle calls; the existing
chat provider and conversation persistence paths remain independent.

Not yet verified: live cancellation/timeout visuals and activity-history UI.
The next slice is the user-visible activity surface and restart reconciliation
status.

## Slice 7 — activity and restart reconciliation surface

Status: `implemented_unverified`

The existing scheduled-task navigation seam now opens an Activity & tasks
surface backed by the durable operation service. It lists recent operation
previews, statuses, timestamps, and sequence checkpoints. Startup and surface
open both reconcile interrupted runs to `needs_reconciliation`; the UI tells
the user that no provider, shell, Git, or file side effect was replayed.
Future scheduling remains explicitly unimplemented rather than being implied
by the label.

Changed files:

- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/SidebarView.xaml`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including journal restart-reconciliation coverage.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed before commit.

Privacy/security/licensing: the surface displays bounded local instruction
previews and operation metadata only. Reconciliation is a journal transition
and never retries external side effects. No network call, credential, or
third-party code was added.

Rollback: revert the Activity & tasks navigation and service load/reconcile
calls; the underlying journal and chat lifecycle remain independently
recoverable.

Not yet verified: live Windows keyboard/screen-reader rendering and packaged
Linux parity. The next dependency is a real user decision path for
`needs_reconciliation` runs, followed by explicit workflow/schedule models.

## Slice 8 — reviewed reconciliation decisions

Status: `implemented_unverified`

Activity & tasks now gives a user-visible `needs_reconciliation` run two
explicit outcomes: mark cancelled or mark failed. Both outcomes append a
bounded reason event and close the dialog without resuming or replaying any
provider, shell, Git, or file side effect. The Core service rejects decisions
for any other status, keeping the recovery path state-safe.

Changed files:

- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/SkaiDesktop.Core.Tests/OperationServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including restart reconciliation and reviewed decision coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after fixing the automation metadata import.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the decision records only local operation metadata
and a fixed bounded reason. It does not expose secrets, resume uncertain work,
or add network access, credentials, or third-party code.

Rollback: revert the reconciliation decision method, test, and two Activity
buttons; unresolved runs remain visible as `needs_reconciliation` in the
existing history surface.

Not yet verified: live keyboard/screen-reader use of the decision buttons and
packaged Linux parity. The next dependency is a versioned workflow/task model
with approvals, retries, cancellation, and schedule semantics.

## Slice 9 — versioned workflow definition foundation

Status: `implemented_unverified`

`WorkflowService` now persists versioned workflow definitions and validates the
typed DAG before saving. Validation enforces bounded IDs and labels, supported
node types, exactly one root, connectivity, acyclicity, bounded configuration,
and indirect secret references. Duplicate `(workflowId, version)` entries are
rejected so edits must create a new version. This slice is a definition and
validation boundary only; no workflow is executed or scheduled yet.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowDefinition.cs`
- `native/SkaiDesktop.Core/Services/WorkflowService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowServiceTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including valid DAG persistence, cycle rejection, and inline-secret rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: workflow configuration is local bounded JSON and
rejects secret-like keys rather than persisting credential material. Atomic
writes preserve the previous file on write failure. No execution, network
access, credential use, or third-party code was added.

Rollback: remove the workflow model, service, and test registration; existing
operation, conversation, project, and settings stores are independent.

Not yet verified: workflow editor, dry-run, step runner, approval inbox,
idempotent retries, background scheduling, or packaged Linux parity. The next
dependency is durable per-step run state pinned to a workflow version before
any tool/provider execution is connected.

## Slice 10 — durable workflow step runs

Status: `implemented_unverified`

`WorkflowRunService` now creates durable runs pinned to a workflow ID and
version, tracks each node through pending/executing/waiting-approval/complete/
failed/cancelled states, persists approval IDs and event sequences, allows
retries only for explicitly safe node types, preserves stable idempotency keys,
and reconciles in-flight runs after restart. Approval completion and final
verification are recorded as events. No run method dispatches tools, shell
commands, Git, or model providers yet.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowRun.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including durable approval pause/resume, safe retry attempt increment, cancellation, and restart reconciliation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: run state and event payloads are bounded local
metadata. Retry is conservative and refuses non-idempotent tool-like steps;
approval identity is validated and recorded. No external dispatch, network
call, credential, or third-party code was added.

Rollback: remove the workflow run model, service, and tests; versioned
definitions and all existing application stores remain independent.

Not yet verified: workflow editor/dry-run, tool adapter capability gates,
parallel resource limits, background scheduler/OS restart behavior, approval
inbox UI, and packaged Linux parity. The next dependency is an explicit
workflow-run activity surface before connecting any executor.

## Slice 11 — workflow runs in Activity & tasks

Status: `implemented_unverified`

Activity & tasks now loads durable workflow runs alongside chat operations,
showing workflow ID/version, run status, each step state, attempt number, and
waiting approvals. Waiting approval steps expose an accessible Approve action
that records a stable UI approval identity and refreshes truthfully by closing
the surface; it does not dispatch a tool, provider, shell, Git, or file action.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed in the preceding workflow-run slice, including durable approval transitions and restart reconciliation.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the UI displays local run metadata and bounded
step IDs only. Approval is explicit and auditable; no external action is
implicitly authorized or dispatched. No network call, credential, or
third-party code was added.

Rollback: revert the workflow service fields and Activity rendering; durable
workflow runs remain available to Core callers and existing chat activity is
unchanged.

Not yet verified: live UI approval keyboard/screen-reader behavior, workflow
editor/dry-run, tool adapter capability gates, parallel limits, scheduling,
and packaged Linux parity. The next dependency is a dry-run/execution-plan
view that can explain graph order without invoking tools.

## Slice 12 — dependency-ordered workflow plan

Status: `implemented_unverified`

Workflow definitions now produce a deterministic topological execution plan.
Each durable step run stores its node type, label, dependency IDs, stable
idempotency key, and attempt number in plan order. Activity renders the
dependency context for each step, making the current run plan inspectable
without invoking any tool or provider. Cycles remain rejected at definition
validation, and no bounded retry node is implied.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowDefinition.cs`
- `native/SkaiDesktop.Core/Models/WorkflowRun.cs`
- `native/SkaiDesktop.Core/Services/WorkflowService.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowServiceTests.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including dependency-order plan and durable plan-order assertions.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the plan contains bounded local node metadata and
dependency IDs only. It cannot execute work, interpolate secrets, or change
provider routing. No network call, credential, or third-party code was added.

Rollback: revert plan fields, topological planning, rendering, and tests;
durable workflow runs remain readable only through their prior state fields.

Not yet verified: live graph editor/dry-run controls, tool adapter capability
gates, parallel limits, scheduling, and packaged Linux parity. The next
dependency is a read-only dry-run action in the Activity surface, followed by
executor adapters.

## Slice 13 — read-only workflow dry-run preview

Status: `implemented_unverified`

Activity & tasks now provides a Preview plan action for workflow runs. The
preview explains persisted dependency order, node type/label, approval gates,
safe-retry policy, current state, and attempt number. It explicitly performs
no model, tool, shell, Git, network, or file operation and does not mutate the
run.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the preview reads only already-persisted local
workflow metadata and makes no authorization or provider decision. No network
call, credential, or third-party code was added.

Rollback: remove the Preview plan button and dialog; the existing inline
workflow state and Core run service remain available.

Not yet verified: live keyboard/screen-reader behavior, graph editing,
tool-adapter capability gates, parallel limits, scheduling, and packaged Linux
parity. The next dependency is a capability-gated executor adapter for one
safe read-only node type.

## Slice 14 — capability-gated condition executor

Status: `implemented_unverified`

The workflow runtime now supports one explicitly safe executor: a pure
condition node that compares a bounded local input field with a bounded
expected value. It records the boolean result and completes the step/run when
appropriate. The run service rejects tool-call and other unsupported node
types through the same entry point, preventing accidental expansion of the
execution surface.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowRun.cs`
- `native/SkaiDesktop.Core/Services/ReadOnlyConditionExecutor.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including local condition evaluation, durable result completion, and unsupported-node rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: execution is in-process, local, deterministic,
bounded, and has no filesystem, shell, network, provider, Git, or credential
access. Tool-like nodes are refused rather than silently downgraded. No
third-party code was added.

Rollback: remove the condition executor, persisted config fields, and tests;
workflow definitions and non-executing run state remain intact.

Not yet verified: richer conditions/branch semantics, tool adapter grants,
parallel limits, scheduler/OS restart behavior, full approval inbox, and
packaged Linux parity. The next dependency is to expose condition results and
capability refusal in Activity, then add another safe adapter only with a
separate permission contract.

## Slice 15 — workflow executor observability

Status: `implemented_unverified`

Condition step runs now persist their boolean result. Activity shows that
result and labels the executor as the local condition executor; every other
node type is labeled executor unavailable rather than appearing runnable.
This makes capability refusal visible without widening permissions.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowRun.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including persisted true condition result and unsupported-node refusal.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local result metadata is persisted;
unsupported nodes remain non-executable. No network, provider, shell, Git,
filesystem, credential, or third-party code path was added.

Rollback: remove the result field and Activity capability/result text; the
condition executor and prior run state remain usable.

Not yet verified: richer branching, tool adapter grants, parallel limits,
scheduler/OS restart behavior, full approval inbox, and packaged Linux parity.
The next dependency is a separate permission contract for any adapter beyond
pure local conditions.

## Slice 16 — exact-scope workflow capability contract

Status: `implemented_unverified`

Added a UI-free capability contract for future external workflow adapters.
Requests and grants must match the exact run ID, node ID, and scope; requested
capabilities must be in the explicit allowlist; the grant must cover the full
request and include an approval identity. Pure condition nodes are forbidden
from requesting external capabilities. The gate authorizes metadata only and
does not execute anything.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowCapability.cs`
- `native/SkaiDesktop.Core/Services/WorkflowCapabilityGate.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowCapabilityGateTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including exact-scope acceptance, cross-run refusal, and condition external-capability refusal.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: capability names and scopes are bounded and
allowlisted; approval identity is required and no credential values are
accepted. The contract adds no network, shell, filesystem, provider, Git, or
third-party code path.

Rollback: remove the capability model, gate, and test registration; current
condition-only execution and workflow persistence remain unchanged.

Not yet verified: adapter-specific grants, approval inbox queuing, parallel
resource limits, scheduling, and packaged Linux parity. The next dependency
is to connect this contract to a deliberately read-only workspace adapter
with a separate effect audit.

## Slice 17 — capability-gated read-only workspace adapter

Status: `implemented_unverified`

Added `ReadOnlyWorkspaceAdapter`, which requires an exact run/node/scope grant
for `workspace.read`, reads a bounded supported text file through the existing
workspace path, symlink, extension, and protected-name safeguards, and returns
a `WorkspaceReadEffect` with relative path, character count, and
`Mutated=false`. It cannot write files or run commands.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkspaceReadEffect.cs`
- `native/SkaiDesktop.Core/Services/ReadOnlyWorkspaceAdapter.cs`
- `native/SkaiDesktop.Core.Tests/ReadOnlyWorkspaceAdapterTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including approved read/effect reporting, cross-run grant refusal, and protected-file refusal.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the adapter is local-only, bounded, read-only,
scope-authorized, and reuses existing symlink/path/protected-file controls.
No network, shell, provider, Git, credential, or third-party code path was
added.

Rollback: remove the adapter, effect model, and tests; the capability gate and
existing Coding Agent file controls remain unchanged.

Not yet verified: adapter invocation from a workflow run, effect timeline UI,
write/shell/network adapters, parallel limits, scheduling, and packaged Linux
parity. The next dependency is wiring this adapter to a workflow step with a
durable effect event while preserving approval and cancellation boundaries.

## Slice 18 — durable workspace-read workflow effect

Status: `implemented_unverified`

`WorkflowRunService.ReadWorkspaceStep` now wires the exact-scope read grant to
an executing tool-call step and the read-only workspace adapter. Successful
reads complete the step and persist a `workspace.read_completed` event with
node ID, relative path, character count, and `mutated=false`; file contents and
absolute paths are not placed in the event payload. A completed final step
advances through verifying to completed.

Changed files:

- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including workflow adapter invocation and durable non-mutating effect event coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the adapter remains local, read-only, bounded,
path-safe, and grant-scoped. Effect events deliberately omit file contents
and absolute paths. No network, shell, provider, Git, credential, or
third-party code path was added.

Rollback: remove `ReadWorkspaceStep` and its test; the standalone adapter and
condition-only executor remain available without changing existing files.

Not yet verified: effect timeline UI, write/shell/network adapters, approval
inbox queuing, parallel limits, scheduling, and packaged Linux parity. The
next dependency is exposing this durable effect in Activity before any new
side-effecting adapter is considered.

## Slice 19 — workflow effect timeline in Activity

Status: `implemented_unverified`

Activity & tasks now renders a bounded effect timeline from the persisted
workflow event journal. It summarizes workspace reads (relative path,
characters, no mutation), condition outcomes, approvals, reconciliation,
completion, and cancellation, capped to the latest 12 events per run. Raw
file contents, absolute paths, and arbitrary payloads are not rendered.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the timeline is local-only, bounded, and uses an
allowlisted event-summary mapping. It deliberately omits raw event payloads
and file contents. No network, credential, shell, provider, Git, or
third-party code path was added.

Rollback: remove the event-summary rendering block; persisted workflow events
remain available to Core callers.

Not yet verified: live timeline keyboard/screen-reader behavior, approval
inbox queuing, write/shell/network adapters, parallel limits, scheduling, and
packaged Linux parity. The next dependency is a UI-visible approval inbox
model for concurrent workflow requests.

## Slice 20 — durable approval inbox

Status: `implemented_unverified`

`WorkflowRunService.LoadPendingApprovals` derives up to 100 pending approval
requests from durable runs, ordered by request time. Activity & tasks now
renders one bounded Pending approvals section before individual workflow
details; each accessible Approve action targets the exact run and node and
uses the existing approval event path. Concurrent requests do not require
nested modal dialogs.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowApprovalRequest.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including two concurrent approvals surfaced through one inbox.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the inbox shows bounded local workflow/node IDs
and labels only; approval remains exact-run/node scoped and auditable. No
network, credential, shell, provider, Git, or third-party code path was added.

Rollback: remove the pending-request model/query and inbox rendering; per-run
approval controls and durable run events remain available.

Not yet verified: live concurrent approval keyboard/screen-reader acceptance,
approval expiry/denial, write/shell/network adapters, parallel limits,
scheduling, and packaged Linux parity. The next dependency is durable approval
denial/expiry semantics before any broader adapter surface.

## Slice 21 — approval denial and expiry

Status: `implemented_unverified`

Workflow approvals now support explicit bounded-reason denial, which marks the
step denied, fails the run, and records `approval.denied`. Pending approvals
older than the 30-minute lifetime are marked expired, fail their run, and
record `approval.expired`. Activity expires stale requests before rendering
the inbox, and exposes accessible Approve and Deny actions for current
requests.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowRun.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including durable denial and 30-minute expiry event coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: denial reasons are bounded local metadata;
expired approvals cannot be acted on. No network, credentials, shell,
provider, Git, or third-party code path was added.

Rollback: remove denial/expiry transitions and inbox actions; existing approve
and reconciliation paths remain available.

Not yet verified: live keyboard/screen-reader approval acceptance, approval
identity display/expiry messaging, write/shell/network adapters, parallel
limits, scheduling, and packaged Linux parity. The next dependency is a
durable approval identity and visible expiry state in the effect timeline.

## Slice 22 — approval identity and expiry visibility

Status: `implemented_unverified`

Pending approval records now expose a deterministic expiry timestamp derived
from the 30-minute lifetime. The Activity inbox displays it, while event
summaries show shortened approval identities for grants and explicit outcomes
for denial and expiry. Denial reasons remain bounded; expired requests are
not actionable.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowApprovalRequest.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including expiry timestamp assertions, denial, and expiration events.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local identities, timestamps, and
denial metadata are displayed. No credentials or file contents are exposed;
no network, shell, provider, Git, or third-party code path was added.

Rollback: remove expiry metadata and event-summary additions; durable approval
denial/expiry transitions remain intact.

Not yet verified: live keyboard/screen-reader behavior, approval countdown
UX, write/shell/network adapters, parallel limits, scheduling, and packaged
Linux parity. The next dependency is broader branching semantics or another
adapter only after its separate permission contract.

## Slice 23 — bounded chat message queue

Status: `implemented_unverified`

Chat now accepts follow-up sends while a response is active. A bounded FIFO
queue holds up to eight validated requests, preserves text and attachments,
reports queue position, and automatically dispatches the next request only
after the active request reaches its cleanup path. Full queues are refused
without losing the current draft or response.

Changed files:

- `native/SkaiDesktop.Core/Services/ChatMessageQueue.cs`
- `native/SkaiDesktop.Core.Tests/ChatMessageQueueTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including FIFO ordering, attachment preservation, capacity refusal, and drain behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: queued requests stay in local process memory and
use existing attachment consent/provider paths when dispatched. The queue is
bounded and does not add network routing or credential storage. No third-party
code was added.

Rollback: remove the queue service, test, field, and SendClick/finally hooks;
the existing single-request chat flow remains unchanged.

Not yet verified: live multi-send keyboard/accessibility behavior, queue
cancel/reorder controls, persistence across app restart, and packaged Linux
parity. The next dependency is a user-visible queue management action before
adding persistent or parallel message behavior.

## Slice 24 — chat queue management controls

Status: `implemented_unverified`

The active-response queue now exposes a Queue control in the runtime status
banner when items exist. Users can inspect bounded prompt previews and
attachment counts, remove individual queued items, or explicitly clear the
entire queue. FIFO dispatch remains unchanged for retained items; clearing the
queue does not stop or alter the active response.

Changed files:

- `native/SkaiDesktop.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.Chat.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including queue FIFO/capacity/drain coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.
- Live Windows check: the built `Skai Desktop` window exposed accessible names for the message composer, Send message, Activity & tasks, and the Queue control contract. End-to-end queue interaction was not claimed because the automation helper returned no click geometry for the Send action while multiple native windows were present.

Privacy/security/licensing: queue contents remain local process memory and the
management surface renders bounded previews only. Explicit clear is reversible
only by resubmitting; it never changes the active request. No network,
credential, provider, shell, Git, or third-party code was added.

Rollback: remove Queue control, management dialog, and queue mutation methods;
bounded automatic FIFO dispatch remains available.

Not yet verified: live keyboard/screen-reader queue interaction, reorder or
persisted queue behavior, and packaged Linux parity. The next dependency is
live accessibility validation before adding persistence or parallel dispatch.

## Slice 30 — durable queued follow-up messages

Status: `implemented_unverified`

Queued follow-up requests are now saved atomically in local app data and
restored at startup. The active response is never persisted or replayed: a
request is removed from the durable queue immediately before dispatch. Queue
mutations (enqueue, dequeue, remove, and clear) update the bounded store, and
restored requests remain available through the existing Queue management
surface for user review before sending.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/ChatMessageQueueStore.cs`
- `native/SkaiDesktop.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/SkaiDesktop.Core.Tests/ChatMessageQueueStoreTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including FIFO/attachment round-trip and persisted empty-queue behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: queued prompts and explicitly attached content
remain local app data, bounded by the existing eight-item and attachment
limits. Persistence adds no network or credential path. Corrupt queue data is
left untouched and reported; it is not overwritten with an empty queue.
No third-party code was added.

Rollback: remove queue-store load/save calls and the local queue store; the
in-memory FIFO behavior remains available.

Live Windows evidence: after stopping the native app while a response was still
active, the persisted follow-up remained in `queued-messages.json`; relaunching
the rebuilt app exposed `Restored 1 queued message; review the Queue control
before sending.` and an accessible `Manage queued messages` button. The startup
rendering path required an additional `UpdateQueueControl()` call, recorded in
the follow-up fix below. End-to-end completion of the active request and
automatic queued dispatch was not claimed because the local model response did
not complete during the bounded observation window.

Not yet verified: queue reordering, cross-device synchronization, durable
context continuity, packaged Linux parity, and live end-to-end send/dispatch
acceptance. The next dependency is queue reordering only after the restored
Queue surface receives a complete live send/dispatch journey.

## Slice 31 — restore queued-message controls on startup

Status: `implemented_unverified`

Startup now refreshes the Queue control after loading durable queued requests,
so restored items are immediately visible and actionable instead of remaining
hidden in memory. This closes the live restart visibility defect found during
the Slice 30 Windows check without auto-sending restored messages.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.
- Live Windows check: after relaunch, the accessibility tree exposed `Restored 1 queued message; review the Queue control before sending.` and `Manage queued messages` while the queue file still contained the bounded follow-up.

Privacy/security/licensing: startup restoration remains local-only, bounded,
and review-first. It does not replay an active request, transmit queue content,
or add credentials/network access. No third-party code was added.

Rollback: remove the startup `UpdateQueueControl()` call; durable data remains
readable but restored items would again require another queue mutation to make
the control visible.

Not yet verified: live completion/dispatch acceptance, queue reordering,
cross-device synchronization, durable context continuity, and packaged Linux
parity. The next dependency is a complete user-driven live queue journey.

## Slice 32 — live restored-queue dispatch evidence

Status: `implemented_unverified`

The live Windows journey was extended using the restored queued follow-up. A
first request was submitted while the restored message remained queued. The
first request reached a provider failure, and the queued follow-up then
appeared in the conversation and was automatically dispatched. The durable
queue file was empty after dispatch, proving the queued item was removed before
the next send. Both local provider calls returned an empty stream, so this
slice records queue ordering and failure-path dispatch only, not successful
model-response acceptance.

Evidence:

- Live Windows accessibility tree showed the restored queued message and
  `Manage queued messages` before the first send.
- Live Windows accessibility tree then showed the first user request, its
  `Connection error`, the restored `Queued follow-up validation message`, and
  the second `Connection error` after automatic dispatch.
- `C:\Users\basic\AppData\Roaming\ServerRock\AI\queued-messages.json` was
  `[]` after the dispatch path.
- The local `/v1/models` endpoint returned HTTP 200 before the journey, but
  the selected model closed both chat streams without content; provider
  availability is therefore not claimed.

Privacy/security/licensing: the journey used local model routing only. Queue
content stayed in local app data, no remote endpoint or credential was added,
and failure handling did not replay the request indefinitely. No third-party
code was added.

Rollback: no production behavior change was made in this evidence slice;
retain the existing queue and provider failure behavior.

Not yet verified: successful live provider response, queue reordering,
cross-device synchronization, durable context continuity, and packaged Linux
parity. The next dependency is provider/runtime health or a deterministic
local test endpoint before claiming successful end-to-end dispatch.

## Slice 33 — queued-message reorder controls

Status: `implemented_unverified`

The Queue management dialog now supports bounded up/down reordering alongside
remove and clear. Moves rebuild the same validated FIFO queue, persist through
the existing atomic queue store, and disable at the first/last boundary. The
active response is unaffected; only messages waiting for dispatch change order.

Changed files:

- `native/SkaiDesktop.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/SkaiDesktop.Core.Tests/ChatMessageQueueTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including valid move ordering, invalid/no-op rejection, attachment preservation, persistence, and drain behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: reordering stays local and bounded; it neither
changes the active request nor adds network, credential, shell, Git, or
third-party behavior.

Rollback: remove the Move method and three Queue dialog controls; existing
FIFO, remove, clear, and durable queue behavior remains available.

Not yet verified: live reorder accessibility, richer search, durable context
continuity, successful live provider-response acceptance, and packaged Linux
parity. The next dependency is live reorder acceptance or a separate
conversation-search slice.

## Slice 34 — richer local conversation search

Status: `implemented_unverified`

Saved-chat search now uses a bounded Core service and searches title, project,
folder, tags, message text, attachment filenames, and web-source title/URL
metadata. Results remain local and are capped at 100 recent hits. The search
dialog displays archive/folder/tag context without exposing full message text,
and oversized or control-character queries are rejected safely.

Changed files:

- `native/SkaiDesktop.Core/Services/ConversationSearchService.cs`
- `native/ServerRock.Native/MainPage.Composer.cs`
- `native/SkaiDesktop.Core.Tests/ConversationSearchTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including tag, attachment, source metadata, recent ordering, and query-bound coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: search is local-only and does not send query text
or saved content to a provider. Results expose bounded labels and organization
metadata, not full message bodies. No third-party code was added.

Rollback: restore the inline title/message/project predicate and list binding;
saved conversations remain unchanged.

Not yet verified: live search dialog accessibility, durable context
continuity, successful live provider-response acceptance, and packaged Linux
parity. The next dependency is live search acceptance or context continuity.

## Slice 25 — durable chat operation provider provenance

Status: `implemented_unverified`

Chat operation records now persist the selected model provider, model ID, and a
bounded endpoint privacy classification (`local`, `remote`, or `unknown`). The
Activity & tasks surface shows this provenance without exposing endpoint URLs,
so a user can tell which configured route handled a request after the response
has completed or failed. Existing journals remain readable because missing
legacy fields normalize to `unknown` in memory.

Changed files:

- `native/SkaiDesktop.Core/Models/OperationRun.cs`
- `native/SkaiDesktop.Core/Infrastructure/OperationJournal.cs`
- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/SkaiDesktop.Core.Tests/OperationServiceTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including provider/model/local provenance persistence.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only bounded provider/model labels and a local or
remote classification are persisted and rendered. Endpoint URLs, API keys,
and prompt contents are not added to the Activity metadata. No third-party
code or new network path was added.

Rollback: remove the provenance fields, journal metadata, and Activity label;
the existing durable operation lifecycle remains usable with unknown metadata.

Not yet verified: provenance for non-chat workflow/provider attempts, live
Activity accessibility behavior, capability-aware routing, and packaged Linux
parity. The next dependency is provider capability/profile modeling before
implementing fallback or multi-provider routing.

## Slice 26 — conservative model capability profiles

Status: `implemented_unverified`

The core provider path now resolves a bounded capability profile for the
selected provider/model: provider label, model label, local/remote endpoint
privacy, streaming support, and vision support confidence. Unknown models stay
allowed so compatible OpenAI-style servers are not silently rejected. Model
identifiers with definitive non-vision markers such as embedding, reranker,
ASR, or TTS are rejected before an image-bearing request reaches the network.

Changed files:

- `native/SkaiDesktop.Core/Models/ModelCapabilityProfile.cs`
- `native/SkaiDesktop.Core/Infrastructure/ModelCapabilityResolver.cs`
- `native/SkaiDesktop.Core/Services/OpenAiCompatibleProvider.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including local/remote profile facts, unknown-model tolerance, definitive non-vision detection, and pre-network image rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the resolver uses only the configured provider,
model, and endpoint locality; it stores no credentials and emits no endpoint
URL. Definitive rejection happens before request serialization/network access.
No third-party code was added.

Rollback: remove the profile model, resolver call, and focused tests; existing
provider validation and compatible unknown-model behavior remain available.

Not yet verified: user-editable capability overrides, provider capability
discovery, fallback consent, multi-provider routing, live accessibility, and
packaged Linux parity. The next dependency is an explicit profile/override
surface with durable validation before adding fallback behavior.

## Slice 27 — persisted vision capability override

Status: `implemented_unverified`

Settings now provide a local Vision capability profile choice: automatic
conservative detection, explicitly supports images, or explicitly does not
support images. The choice is normalized and persisted with the existing
settings store, exposed in the Advanced settings page with an accessible name
and help text, and honored by provider preflight. This lets a user correct a
model-name heuristic without silently changing endpoint routing or sending
metadata to a server.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core/Infrastructure/ModelCapabilityResolver.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including settings round-trip and explicit capability override behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the override is a non-secret local preference;
automatic detection and the override run before request serialization. No
endpoint URL, credential, prompt, or third-party code is added.

Rollback: remove the override setting/control and resolver branch; automatic
conservative detection remains the default.

Not yet verified: provider capability discovery, user-editable profiles for
streaming/context limits, fallback consent, multi-provider routing, live
settings accessibility, and packaged Linux parity. The next dependency is
capability discovery/profile validation before selecting another route.

## Slice 28 — bounded provider model capability discovery

Status: `implemented_unverified`

The OpenAI-compatible model discovery path now has a profile-aware API that
parses bounded provider metadata without changing the existing model-ID API.
It recognizes explicit `supports_vision`, `capabilities`, and `modalities`
signals, limits each advertised label and list, drops malformed or oversized
model IDs, and preserves `unknown` when the provider does not declare vision
support. Existing connection and selection callers continue using the legacy
string list, while capability-aware callers can opt into descriptors.

Changed files:

- `native/SkaiDesktop.Core/Models/ProviderModelDescriptor.cs`
- `native/SkaiDesktop.Core/Services/OpenAiCompatibleProvider.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including explicit capability metadata, boolean vision flags, unknown metadata, and existing model-ID discovery.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only provider-returned bounded model labels and
capability strings are retained in memory; credentials and endpoint URLs are
not returned in descriptors. Malformed metadata is ignored rather than trusted.
No third-party code was added.

Rollback: retain `DiscoverModelsAsync` and remove the descriptor API/parser;
existing connection and model selection behavior remains unchanged.

Not yet verified: UI display of discovered capability evidence, provider
discovery beyond OpenAI-compatible `/models`, streaming/context-limit
discovery, fallback consent, multi-provider routing, live accessibility, and
packaged Linux parity. The next dependency is surfacing descriptor evidence in
model selection before routing decisions consume it.

## Slice 29 — visible selected-model capability evidence

Status: `implemented_unverified`

The live connection refresh now uses the profile-aware discovery API and
reports the selected model's provider-advertised vision state in the runtime
status: `vision advertised`, `non-vision metadata`, or `vision capability
unknown`. Existing model fallback and connection state behavior is unchanged;
the evidence is informational and does not silently route, upload, or alter
the user's model choice.

Changed files:

- `native/ServerRock.Native/MainPage.Connection.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including provider discovery and all existing connection/provider coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the status uses only bounded metadata returned by
the configured provider. It does not expose credentials or endpoint URLs in
the capability label, and it does not enable fallback or additional routing.
No third-party code was added.

Rollback: restore the legacy model-ID discovery call and generic connected
status; model selection and connection checks remain functional.

Not yet verified: live runtime status accessibility, provider metadata support
beyond `/models`, streaming/context-limit discovery, fallback consent,
multi-provider routing, and packaged Linux parity. The next dependency is a
tested capability-aware selection decision, still requiring explicit consent
before any alternate route is used.

## Slice 35 — live conversation search journey

Status: `implemented_verified_windows`

The bounded local conversation search added in Slice 34 was exercised in the
live Windows native app. The search surface exposed an accessible dialog title,
query field, result list, and Open chat action. Searching for `FIRST_DONE`
returned the saved conversation `Reply with exactly: FIRST_DONE`; opening it
loaded the conversation and displayed the persisted `Queued follow-up
validation message`.

Evidence:

- Live Windows accessibility inspection — passed: `Search your workspace`,
  `Search chats in both workspaces`, the matching result, and `Open chat` were
  exposed; the opened conversation displayed both saved user messages.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: search remains local-only and bounded; the live
journey used saved conversation metadata/content and did not send a query to a
provider. No third-party code was added.

Rollback: revert `c13db14` and the associated documentation evidence; the
existing conversation list remains available without the richer search path.

Not yet verified: durable context continuity, successful live provider
response acceptance, Linux parity, and packaged-release acceptance.

## Slice 36 — accessible workflow approval countdown

Status: `implemented_verified_with_live_surface_boundary`

Workflow approvals now expose a bounded remaining-time label in the Activity &
tasks surface. The label reports sub-minute urgency, rounded minutes/hours, or
`Expired`; a five-second native timer refreshes it, marks the status as a
polite accessibility announcement, and disables Approve/Deny after expiry.
The pure timing policy is covered independently so UI rendering does not own
expiry arithmetic.

Changed files:

- `native/SkaiDesktop.Core/Services/WorkflowApprovalTiming.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowApprovalTimingTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including sub-minute, minute, hour, and expired countdown cases.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (85 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed for the `Activity & tasks`
  dialog and existing reconciliation controls. The current local state had no
  pending approval, so the countdown text and disabled-after-expiry behavior
  were not claimed as live-rendered evidence.

Privacy/security/licensing: the countdown is derived from locally persisted
approval timestamps; it does not call a provider, execute a tool, or add a
credential. Expiry disables the visible actions and the existing durable
service remains the authority for approval state.

Rollback: remove the timing helper, timer, and two audit contracts; the prior
fixed expiry timestamp and durable expiry service remain available.

Not yet verified: a seeded live pending-approval countdown, workflow write/
shell/network adapters, richer branching, parallel limits, scheduling, full
audit/export, Linux parity, and packaged-release acceptance.

## Slice 37 — global command palette

Status: `implemented_verified_windows`

The native shell now opens a searchable command palette with `Ctrl+K`. It
filters existing routed actions by title and keywords, exposes the query and
results through accessibility names, restores composer focus when dismissed,
and executes the selected action only after the palette closes. The first live
journey filtered for `settings` and opened the existing Settings surface;
provider, file, Git, and model behavior remains owned by their existing routes.

Changed files:

- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- Live Windows accessibility inspection — passed: `Ctrl+K` exposed
  `Command palette`, `Search command palette actions`, `Command palette
  results`, and the full routed action list; filtering `settings` left
  `Connection & settings`, and running it opened the Settings dialog.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (89 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the palette searches a fixed local action catalog
and does not transmit query text, invoke a provider, or add a dependency.
Actions retain their existing consent and permission gates.

Rollback: remove `MainPage.CommandPalette.cs`, the `Ctrl+K` branch, and the
three audit contracts; all existing sidebar and slash-command routes remain.

Not yet verified: navigation back/forward history, persistent tabs/layout,
inspector state, live resize/focus acceptance at every target width, Linux
parity, and packaged-release acceptance.

## Slice 38 — live compatible local-provider response

Status: `implemented_verified_windows_with_model_compatibility_boundary`

The live Windows chat journey was tested against the configured local
OpenAI-compatible endpoint. The original selected model,
`huihui_ai/Qwen3.8-abliterated:latest`, was advertised by `/v1/models` but
closed the stream without returning content; the app rendered the distinct
`The model server closed the stream without returning a response.` failure.
Using the existing Settings route, a locally installed compatible model,
`wtwk-qwen35-4b:latest`, connected successfully and returned `LIVE_OK` in the
native conversation, including Copy and Retry actions. The original model
selection was restored after validation.

Evidence:

- Local runtime inventory — passed: `/api/tags` and `/v1/models` exposed five
  local models; no remote endpoint was used.
- Direct local OpenAI-compatible probe — passed for
  `wtwk-qwen35-4b:latest`, returning `RUNTIME_OK`.
- Live Windows app — passed for Settings model selection, connection status,
  prompt submission, assistant `LIVE_OK` rendering, and Copy/Retry actions.
- Live Windows app — failed for the original selected model with the truthful
  stream-closure error; this is retained as a model-specific compatibility
  limitation rather than treated as a generic provider success.
- The original `huihui_ai/Qwen3.8-abliterated:latest` selection was restored
  before ending the live validation.

Privacy/security/licensing: all probes and the successful response stayed on
the local Ollama endpoint. No prompt, response, credential, or context was
sent to a cloud provider, and no new dependency was added.

Rollback: no source rollback is required; the only runtime setting changed
during validation was restored to its original value.

Not yet verified: durable restart/reopen of this successful response, automatic
model compatibility recovery, explicit fallback consent, multi-provider
routing, Linux parity, and packaged-release acceptance.

## Slice 39 — explicit local model recovery after provider failure

Status: `implemented_verified_windows`

When a local model request fails, the conversation error now offers an
accessible `Choose another local model` action. The action is rendered only
for loopback/localhost endpoints, opens the existing installed-model library,
and explicitly does not switch models automatically. Remote failures retain
the existing Settings guidance, so this slice cannot silently route prompts or
context to another provider.

Changed files:

- `native/ServerRock.Native/MainPage.Messages.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (91 contracts).`
- `git diff --check` — passed.
- Live Windows failure journey — passed: the incompatible local model
  produced the truthful stream-closure message and accessible recovery button;
  activating it opened `Model Library`, whose accessible results listed five
  installed local models.

Privacy/security/licensing: the recovery action is gated by
`LocalEndpoint.IsLocal`; it reads the existing local model inventory and sends
no prompt or workspace context anywhere. No third-party code or dependency was
added.

Rollback: remove the optional message-content callback and recovery button;
the existing error bubble and Settings guidance remain unchanged.

Not yet verified: automatic compatibility detection, an explicit fallback
confirmation flow for switching, multi-provider routing, durable provider
identity across fallback attempts, Linux parity, and packaged-release
acceptance.

## Slice 40 — explicit fallback confirmation

Status: `implemented_verified_windows`

The local failure recovery path now carries an explicit confirmation boundary.
When the user selects another installed local model from the recovery library,
the app shows `Switch local model?`, names the current and proposed models, and
states that the change affects only the selected local model with no cloud
route or automatic retry. Choosing Keep current model leaves the preference
unchanged; only Switch model calls the existing selection path.

Changed files:

- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (94 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: failure recovery opened
  Model Library; selecting `wtwk-qwen35-4b:latest` exposed the accessible
  confirmation with current/target models and explicit no-cloud language;
  `Keep current model` preserved `huihui_ai/Qwen3.8-abliterated:latest`.

Privacy/security/licensing: switching remains local-only and user initiated.
The confirmation prevents an implicit model change after a provider failure;
no prompt, context, credentials, or third-party code was added.

Rollback: remove the optional confirmation parameter and dialog; the existing
local model library and explicit recovery chooser remain available.

Not yet verified: automatic compatibility detection, multi-route provider
selection, durable provider identity across fallback attempts, Linux parity,
and packaged-release acceptance.

## Slice 41 — model-specific provider failure classification

Status: `implemented_verified_windows`

Provider failures now have a bounded Core classification separating endpoint
unavailability, server rejection, image rejection, timeout, unknown errors,
and the model-compatibility case where a stream closes without content. The
native chat surface uses that classification to announce `Model compatibility
issue` for the failing model, preserve the local recovery chooser, and record a
model-compatibility operation failure reason without attempting an automatic
switch.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/ProviderErrorText.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including model, endpoint, image, and timeout classification cases.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (96 contracts).`
- `git diff --check` — passed.
- Live Windows provider failure — passed: the original local model's closed
  stream exposed `Model compatibility issue · choose another local model or
  check Settings` and retained the accessible recovery action.

Privacy/security/licensing: classification uses only the local exception and
request context already in memory. It does not inspect or transmit prompt
content, does not select a route, and adds no dependency.

Rollback: remove `ProviderFailureKind` and restore the generic failure status;
existing error descriptions and explicit recovery remain available.

Not yet verified: proactive compatibility detection before a request,
multi-route provider selection, durable provider identity across fallback
attempts, Linux parity, and packaged-release acceptance.

## Slice 130 — extract conversation navigation from the sidebar

Status: `implemented_verified_build`

The folders/import-export/recents block is now a dedicated
`ConversationNavigationView` UserControl. `SidebarView` remains the shell and
forwards the existing conversation events and compatibility accessors, while
`MainPage` continues to own all chat persistence, folder actions, rendering, and
focus recovery. This is a visual decomposition only; no chat, settings, provider,
or pet behavior changed.

Changed files:

- `native/ServerRock.Native/ConversationNavigationView.xaml`
- `native/ServerRock.Native/ConversationNavigationView.xaml.cs`
- `native/ServerRock.Native/SidebarView.xaml`
- `native/ServerRock.Native/SidebarView.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `npm run check:syntax` — passed.
- `npm run audit:native-ui` — passed: `Native UI audit passed (181 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Privacy/security/licensing: the extraction adds no storage, provider, network,
credential, dependency, or third-party path. It does not modify the pet-owned
companion surface.

Rollback: restore the conversation section in `SidebarView.xaml`, remove
`ConversationNavigationView.xaml` and `.xaml.cs`, and restore the direct accessors
and handlers in `SidebarView.xaml.cs`.

Not yet verified: live packaged interaction after this extraction, narrow-window
visual QA, Linux parity, and the remaining handoff requirements.

## Slice 132 — live acceptance of the decomposed sidebar

Status: `verified_windows_accessibility_inspection`

The current Windows desktop build was opened from the synchronized checkout and
the navigation drawer was expanded with its keyboard shortcut. The accessibility
tree visibly exposed the product identity, Chat/Coding Agent switch, new chat,
Activity, Images, Extensions & skills, Model store, More, GitHub, project and
project-file navigation containers, conversation-folder creation, chat import and
export, recent-chat rows/actions, and the existing pet controls. The inspection
was read-only; no chat, project, folder, import/export, or pet action was invoked.

Evidence:

- Native Windows app launched from the current `net10.0-desktop` build.
- Accessibility inspection exposed `PROJECT CONTEXT`/project navigation, the
  `CONVERSATIONS` block, `Create conversation folder`, `Import saved chats`,
  `Export saved chats`, recent conversation rows, and companion controls.
- No provider request, file picker, persistence mutation, or pet setting change
  occurred; the process was stopped after inspection.

Privacy/security/licensing: inspection read only existing local UI state and
introduced no network, credential, provider, dependency, or third-party path.

Rollback: documentation-only; remove this evidence entry if the live surface is
later invalidated.

Not yet verified: packaged-artifact interaction after the extraction, narrow-window
visual QA, Linux parity, and full handoff completion.

## Slice 134 — disposable live organization persistence journey

Status: `verified_windows_disposable_mutation_and_reload`

The app now has an explicit `SRAI_SETTINGS_PATH` boundary in addition to the
existing conversations-path override, allowing a disposable Windows acceptance
profile. Against that profile, a seeded local chat was folder-created, pinned,
tagged with `handoff` and `disposable`, moved into the new folder, and then
reloaded in a fresh app process. The post-reload accessibility tree showed the
folder with one chat, the pinned title, both tags, and `All chats are in folders.`
The real user profile and pet state were not used or changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `docs/ARCHITECTURE.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Disposable profile used `SRAI_SETTINGS_PATH` and `SRAI_CONVERSATIONS_PATH`
  under a temporary workspace directory; the temporary files were removed after
  the journey.
- Live Windows accessibility inspection exposed the folder creation dialog,
  saved folder, pin state, tag editor and saved tags, move action, folder row
  with one chat, and post-restart persistence of all organization state.
- `npm run audit:future-features` — passed: `160 items, 11 sections, 3 checked`.
- No provider request, cloud route, credential, real-profile mutation, or pet
  action occurred.

Privacy/security/licensing: the disposable environment prevented test prompts,
folder names, and organization metadata from entering the user's real profile.
The environment variables are explicit local test controls and add no dependency
or third-party code.

Rollback: remove the temporary-path constructor argument and restore the checked
backlog item only if this evidence is invalidated; no production user data needs
restoration.

Not yet verified: live per-message fork activation, live memory setting save/send
with an eligible note, semantic compaction and 1M+ context, packaged interaction,
Linux parity, and the remaining handoff requirements.

## Slice 135 — live per-message fork activation

Status: `verified_windows_disposable_fork_journey`

The per-message `Fork from here` action was exercised against a disposable seeded
three-message conversation. The second user message was forked from the live
conversation; the UI reported `Fork created from message 3; the original
conversation is unchanged.` and opened the new fork titled with the fork marker.
The disposable archive then contained the original conversation plus a new session
with `forkedFrom` set to the original ID and the copied first three messages.

Evidence:

- Disposable Windows profile used `SRAI_SETTINGS_PATH` and
  `SRAI_CONVERSATIONS_PATH`; temporary files were removed after inspection.
- Live accessibility tree exposed a `Fork conversation from this message` action
  on each rendered message.
- Live screenshot and status text showed the fork result and unchanged-original
  claim; the saved archive independently showed the new fork identity, source ID,
  title marker, and copied prefix.
- No provider request, cloud route, credential, real-profile mutation, or pet
  action occurred.
- `npm run audit:future-features` — passed after this documentation update.

Privacy/security/licensing: the journey used only bounded synthetic local content
and an isolated profile; no conversation data left the device and no dependency or
third-party code was added.

Rollback: documentation-only after the existing tested fork implementation; remove
this evidence entry and restore the backlog checkbox only if the live result is
invalidated.

Not yet verified: live memory setting save/send acceptance with an eligible note,
durable cross-restart context continuity, semantic compaction and 1M+ context,
packaged interaction, Linux parity, and the remaining handoff requirements.

## Slice 136 — disposable eligible-memory review and save

Status: `verified_windows_disposable_review_and_save`

`MemoryStore` now honors the explicit `SRAI_MEMORY_PATH` environment variable;
the default remains the normal platform application-data memory file. A synthetic
approved global note was loaded from a disposable profile. The live command palette
opened `Review approved memory context`, which exposed the local/read-only and
untrusted-reference disclosure, the injection checkbox, and the approved-memory
preview. Enabling the checkbox and saving reported `Approved memory injection
enabled. Review the local context before sending your next prompt.`; the disposable
settings file independently recorded `memoryInjectionEnabled: true`.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `docs/ARCHITECTURE.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Disposable profile used `SRAI_SETTINGS_PATH`, `SRAI_CONVERSATIONS_PATH`, and
  `SRAI_MEMORY_PATH`; temporary files were removed after the journey.
- Live Windows accessibility inspection exposed the review dialog, safety
  disclosure, eligible-note preview, injection control, and saved status.
- The saved disposable settings file contained `memoryInjectionEnabled: true`;
  the seeded memory note remained local and unchanged.
- `npm run audit:native-ui` — passed: `Native UI audit passed (181 contracts).`
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- No provider request or cloud route occurred because the seeded selected model
  was unavailable; this slice does not claim live send acceptance.

Privacy/security/licensing: synthetic memory and settings stayed in an isolated
temporary profile; no real memory, credentials, provider, network, dependency, or
third-party path was used.

Rollback: remove the environment-variable constructor argument and restore
`new MemoryStore()`; remove this evidence entry if the live result is invalidated.

Not yet verified: live memory send acceptance with an eligible note and a healthy
compatible model, durable cross-restart context continuity, semantic compaction and
1M+ context, packaged interaction, Linux parity, and remaining handoff work.

## Slice 137 — live approved-memory injection with local response

Status: `verified_windows_disposable_local_send`

The approved-memory path was completed end to end in a disposable profile. The
profile selected the locally advertised Ollama model `wtwk-qwen35-4b:latest`,
enabled `memoryInjectionEnabled`, and loaded one synthetic approved global note
whose content requested the unique response marker `MEMORY_CONTEXT_OK`. A single
prompt was sent through the native app to `http://localhost:11434/v1`; after the
model loaded, the live response was exactly `MEMORY_CONTEXT_OK`. The saved
disposable conversation archive independently retained the prompt and assistant
response, while no cloud route was used.

Evidence:

- Local runtime inventory reported `wtwk-qwen35-4b:latest` from Ollama.
- Live Windows app exposed the compatible local model and completed response
  `MEMORY_CONTEXT_OK`; the stop control disappeared after completion.
- The disposable archive contained the prompt and assistant response, including
  the marker; disposable settings retained the selected local model and
  `memoryInjectionEnabled: true`.
- Disposable settings, conversation, and memory files were removed after the
  journey; the real profile and pet state were not used or changed.
- No cloud endpoint, API key, external search, credential, or third-party
  dependency was involved.

Privacy/security/licensing: the synthetic note and prompt stayed on the local
Ollama route and in the isolated profile. The memory content remained bounded,
explicitly enabled, and treated as untrusted reference context by the prompt
builder.

Rollback: documentation-only after the existing tested memory injection path;
remove this evidence entry if the local runtime or saved transcript is later
invalidated. The `SRAI_MEMORY_PATH` boundary remains a safe disposable-test aid.

Not yet verified: durable context continuity across restart and profile migration,
semantic compaction and 1M+ context, packaged interaction, Linux parity, and the
remaining handoff requirements.

## Slice 133 — add a disposable settings profile boundary

Status: `implemented_verified_build`

The native app now honors the explicit `SRAI_SETTINGS_PATH` environment variable
when constructing its settings store. The default remains the normal platform
application-data path; the override exists so live acceptance journeys can use a
disposable settings file alongside the existing `SRAI_CONVERSATIONS_PATH` history
override. No provider, credential, pet, or default-profile behavior changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `npm run check:syntax` — passed.
- `npm run audit:native-ui` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Privacy/security/licensing: the override is opt-in, local-only, and explicit;
without it the existing profile path is unchanged. It adds no network, credential,
dependency, or third-party path.

Rollback: remove the environment-variable argument and restore `new
JsonSettingsStore()`; the default profile behavior is unchanged.

Not yet verified: disposable live folder/tag/pin mutation and reload, packaged
interaction, Linux parity, and the remaining handoff requirements.

## Slice 131 — extract project navigation from the sidebar

Status: `implemented_verified_build`

The Coding Agent project-context and project-file containers are now hosted by
`ProjectNavigationView`. `SidebarView` continues to expose the same compatibility
accessors, and `MainPage` retains project selection, file rendering, editing, and
Git behavior. This is a visual-only extraction and does not touch pet UI or project
data.

Changed files:

- `native/ServerRock.Native/ProjectNavigationView.xaml`
- `native/ServerRock.Native/ProjectNavigationView.xaml.cs`
- `native/ServerRock.Native/SidebarView.xaml`
- `native/ServerRock.Native/SidebarView.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `npm run check:syntax` — passed.
- `npm run audit:native-ui` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the extraction adds no storage, provider, network,
credential, dependency, or third-party path. Existing project-file access controls
remain in the feature code.

Rollback: restore the project-context/file section in `SidebarView.xaml`, remove
`ProjectNavigationView.xaml` and `.xaml.cs`, and restore the direct accessors in
`SidebarView.xaml.cs`.

Not yet verified: live packaged interaction after this extraction, narrow-window
visual QA, Linux parity, and the remaining handoff requirements.

## Slice 108 — preserve cancellation for approved workspace commands

Status: `implemented_verified_core`

The approved workspace-command path now has a regression check for user
cancellation in addition to its timeout check. Cancelling an in-flight command
must terminate the process tree, wait for the process to exit, and propagate
`OperationCanceledException` to the caller. This confirms the cancellation
boundary does not leave a background command running or silently convert a
user cancellation into a successful result.

Changed files:

- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including `project command cancellation kills the process tree and propagates cancellation`.
- `git diff --check` — passed.

Privacy/security/licensing: this slice adds no provider request, credential,
network route, dependency, or new execution capability. It verifies the
existing explicit-approval command runner's local cancellation behavior only.
The command runner still is not an OS sandbox; that limitation remains
explicit in the capability matrix.

Rollback: remove the focused cancellation test and the matrix wording; the
existing timeout and process-tree cleanup implementation remains unchanged.

Not yet verified: OS-level sandboxing, richer tool transport, cross-platform
PTY acceptance, parallel-agent isolation, Linux parity, and packaged-release
acceptance.

## Slice 109 — live Activity & Tasks action surface rechecked

Status: `verified_windows_accessibility_inspection`

The current Windows desktop build was launched from the synchronized checkout
and Activity & Tasks was opened through the command palette. The live
accessibility tree exposed the bounded action surface and the existing local
evidence records: Export activity evidence, Search activity evidence, Schedule
task, completed scheduler work with the explicit no-provider-dispatch outcome,
provider-attempt run IDs and sequence numbers, and the two interrupted-operation
decisions (mark cancelled or failed).

Evidence:

- `npm run start:native` — launched the current `net10.0-desktop` build.
- Live Windows accessibility inspection — passed for command-palette routing
  and the Activity & Tasks action/evidence surface.
- The inspection did not mutate tasks, export data, send a provider request, or
  choose an interrupted-operation decision.

Privacy/security/licensing: inspection read existing local activity records and
bounded labels only. No prompt, credential, provider route, network request,
or third-party dependency was added.

Rollback: documentation-only; remove this evidence entry if a later live run
contradicts the observed surface.

Not yet verified: keyboard activation of every Activity action, seeded live
workflow-event search matching, full status-history keyboard/screen-reader
acceptance, OS session hosting, parallel-agent artifacts, Linux parity, and
packaged acceptance.

## Slice 110 — make Activity dialog entry keyboard predictable

Status: `implemented_verified_build`

The dynamically created Activity & Tasks dialog now has an explicit accessible
name and moves focus to the non-mutating Export evidence action when it opens.
This gives keyboard and assistive-technology users a deterministic entry point
while leaving task scheduling, run-now, approval, and reconciliation actions
explicitly user initiated.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (148 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: this is a local focus and accessibility change.
It adds no provider request, credential, network route, task mutation,
dependency, or third-party code.

Rollback: remove the dialog accessible-name/focus wiring and the two audit
contracts; the existing Activity actions remain available with their existing
explicit approval behavior.

Not yet verified: live keyboard activation of every Activity action, seeded
live workflow-event search matching, full status-history keyboard/screen-reader
acceptance, OS session hosting, parallel-agent artifacts, Linux parity, and
packaged acceptance.

## Slice 112 — make status history keyboard-readable

Status: `implemented_verified_build`

The actionable status-history dialog now has an explicit accessible name and a
focusable, named scroll region containing the bounded history entries. Opening
the dialog moves focus to that region so keyboard and screen-reader users land
on the status content instead of an unspecified dialog surface.

Changed files:

- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (153 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: status history remains local, bounded, and
read-only. This slice adds no provider request, credential, network route,
task mutation, dependency, or third-party code.

Rollback: remove the named scroll region, dialog focus handler, and their audit
contracts; the existing status-history dialog remains available.

Not yet verified: live Windows keyboard/screen-reader acceptance of status
history, live keyboard activation of every Activity action, seeded live
workflow-event search matching, OS session hosting, parallel-agent artifacts,
Linux parity, and packaged acceptance.

## Slice 113 — add bounded dependency-aware agent task artifacts

Status: `implemented_verified_core`

The Core layer now has a local-only `AgentTaskArtifactStore` for structured
agent task planning. It persists bounded task metadata, specialist roles,
parent operation IDs, dependency IDs, terminal result summaries, and bounded
artifact labels. Tasks can move from proposed to queued, a ready queued task
can be claimed for a future executor only after all dependencies are completed,
and terminal completion/failure/cancellation remains explicit. Search is local
and bounded. The store does not invoke a provider, shell, Git, file tool, MCP
server, sub-agent, or background process.

Changed files:

- `native/SkaiDesktop.Core/Models/AgentTaskArtifact.cs`
- `native/SkaiDesktop.Core/Services/AgentTaskArtifactStore.cs`
- `native/SkaiDesktop.Core.Tests/AgentTaskArtifactTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including dependency gating, persistence/reload, search, and unknown-dependency rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (153 contracts).`
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed: 326 tracked files.
- `npm run check:release-config` — passed.
- `npm run audit:future-features` — passed: 159 items, 11 sections, 0 checked.
- `git diff --check` — passed.

Privacy/security/licensing: task records contain only bounded local metadata;
they do not store credentials, prompt attachments, command output, file
contents, or provider responses. No new dependency, network route, or
execution capability was introduced.

Rollback: remove the task model/store/test and the Program registration, then
restore the prior Agent workbench matrix line. Existing operation journaling
and reviewed Coding Agent flows remain independent.

Not yet verified: task-artifact UI, actual sub-agent delegation, parallel
execution limits, process/container/worktree isolation, background execution,
cross-platform acceptance, and packaged release acceptance.

## Slice 114 — surface agent task artifacts in Activity & Tasks

Status: `implemented_verified_build`

Activity & Tasks now includes a read-only `AGENT TASK ARTIFACTS` section backed
by the bounded local task store. It shows status, specialist role, updated time,
title, dependency count, bounded result summaries, and artifact labels. Empty
state copy makes the no-dispatch boundary explicit. No button or timer executes
tasks, starts a model, invokes a tool, or grants a capability.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (154 contracts).`
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the Activity section reads bounded local metadata
only. It does not expose credentials, file contents, command output, prompts,
provider responses, or absolute paths, and adds no network or execution path.

Rollback: remove the task-store field, read-only Activity section, and audit
contract; the durable task store and existing Activity operation records remain
independent.

Not yet verified: live packaged Activity rendering with seeded task artifacts,
actual sub-agent delegation, parallel execution limits, process/container/
worktree isolation, background execution, cross-platform acceptance, and
packaged release acceptance.

## Slice 115 — add an explicit local agent-task planning journey

Status: `implemented_verified_build`

Activity & Tasks now offers a `Plan agent task` action. The dialog collects a
bounded title, instruction, and role, clearly states that saving is local-only,
and persists a proposed task artifact only after the user chooses `Save task
plan`. It focuses the title field on open, exposes stable accessible labels,
and closes the Activity dialog after a successful save so the user can reopen it
and review the new record. It never starts an agent, calls a model, invokes a
tool, or grants a capability.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (157 contracts).`
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the dialog writes only bounded local task metadata;
it does not accept credentials or file contents and adds no network, provider,
shell, Git, MCP, or background execution path.

Rollback: remove the planning button/dialog and its audit contracts; the
read-only task artifact section and Core store remain available.

Not yet verified: live packaged creation/review with seeded task artifacts,
actual sub-agent delegation, parallel execution limits, process/container/
worktree isolation, background execution, cross-platform acceptance, and
packaged release acceptance.

## Slice 116 — queue proposed agent tasks for a future executor

Status: `implemented_verified_build`

Proposed task artifacts in Activity & Tasks now expose an explicit `Queue for
future executor` action. The action transitions only the selected local record
from `proposed` to `queued`, then closes the review dialog and reports that no
agent or provider was started. It is disabled by absence for already queued or
terminal tasks, preserving the store's lifecycle rules.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (159 contracts).`
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: queueing stores a bounded local status change
only. It does not claim the task, call a provider, invoke a tool, access a
workspace, or create background work.

Rollback: remove the queue button/handler and its audit contracts; proposed
tasks remain viewable and can still be created locally.

Not yet verified: live packaged queue/review with seeded task artifacts, actual
sub-agent delegation, parallel execution limits, process/container/worktree
isolation, background execution, cross-platform acceptance, and packaged
release acceptance.

## Slice 117 — live Windows agent-task planning and queue journey

Status: `verified_windows_accessibility_inspection`

The current `net10.0-desktop` build was launched and Activity & Tasks was opened
through the command palette. The live accessibility tree exposed the Agent task
artifacts section and `Plan agent task` action. A disposable task plan was saved
with title `Disposable live agent task`, verifier role, and a local-only
instruction. Reopening Activity showed the persisted task as `proposed` with
the `Queue agent task for future executor` action. Activating it produced the
visible status `Agent task queued locally for a future executor. No agent or
provider was started.`

Evidence:

- `npm run start:native` — launched the current desktop build.
- Live Windows accessibility inspection — passed for Activity routing, task
  planning dialog fields/disclosure, proposed-task rendering, and explicit
  queue status.
- No model request, tool call, shell command, workspace edit, or background
  task was started during the journey.

Privacy/security/licensing: the journey wrote only the explicitly entered
disposable task metadata to the app's local task store. No credential, file
content, prompt attachment, provider response, or network destination was
introduced.

Rollback: remove the disposable live task from the local app profile if desired;
the code change is already covered by slices 113–116 and the live evidence is
documentation-only.

Not yet verified: packaged-artifact task journey, live task dependency picker,
actual sub-agent delegation, parallel execution limits, process/container/
worktree isolation, background execution, cross-platform acceptance, and
packaged release acceptance.

## Slice 118 — author bounded agent-task prerequisites

Status: `implemented_verified_build`

The local `Plan agent task` dialog now includes an optional prerequisite picker.
It is populated only from task artifacts already in the bounded local store and
saves selected artifact IDs as dependencies. The picker does not dispatch an
agent, claim work, call a provider, invoke a tool, or create background work;
dependency-aware claiming remains an executor concern.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/FUTURE_FEATURES.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the dependency-picker accessibility
  contract.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: prerequisite selection reads only bounded local
task metadata and writes only selected local task IDs. No task instruction,
file content, credential, provider, or network state leaves the application.

Rollback: remove the dependency picker and pass no `dependsOn` values when
creating plans; the existing local task planning and queue lifecycle remains.

Not yet verified: deterministic live multi-selection of prerequisites,
packaged-artifact acceptance, actual sub-agent delegation, parallel execution limits,
process/container/worktree isolation, background execution, cross-platform
acceptance, and packaged release acceptance.

## Slice 119 — render agent-task prerequisite titles

Status: `implemented_verified_build`

Activity now resolves each persisted prerequisite ID against the bounded local
task artifact set and renders the prerequisite title, falling back to a short
task ID when the referenced artifact is unavailable. The display is capped at
eight dependency labels and remains read-only; it does not claim, execute, or
dispatch tasks.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the dependency-label contract.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local task titles and IDs already
stored on this device are rendered. No provider, network, credential, file,
or background execution path is involved.

Rollback: restore the previous count-only dependency label; task persistence
and executor gating are unchanged.

Not yet verified: deterministic live selection/persistence through the missing
Uno App MCP harness, packaged-artifact acceptance, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 120 — preview the next dependency-ready agent task

Status: `implemented_verified_build`

The Activity surface now offers `Preview next ready agent task`. It uses a
read-only dependency-aware store query, chooses the oldest queued task whose
prerequisites are completed, and leaves the task queued. Proposed tasks and
queued tasks with incomplete prerequisites are not reported as ready. The
surface explicitly states that previewing does not claim, start, dispatch, or
call a provider.

Changed files:

- `native/SkaiDesktop.Core/Services/AgentTaskArtifactStore.cs`
- `native/SkaiDesktop.Core.Tests/AgentTaskArtifactTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the readiness-preview contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including non-mutating readiness and dependency gating assertions.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: readiness preview reads only bounded local task
metadata. It does not mutate task status, access credentials, send network
requests, invoke tools, or create background work.

Rollback: remove `GetNextReady` and the Activity preview button; queue and
future-executor state remain unchanged.

Not yet verified: deterministic live activation of the preview control through
the unavailable Uno App MCP harness, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 121 — cancel proposed or queued agent-task plans

Status: `implemented_verified_build`

Activity now exposes `Cancel plan` for proposed and queued local task plans.
Cancellation is allowed only before execution, records a bounded local reason,
and prevents the task from appearing in the dependency-ready preview. Running
tasks still require the existing executor-owned completion/failure/cancel
path; this control does not stop processes or dispatch any work.

Changed files:

- `native/SkaiDesktop.Core/Services/AgentTaskArtifactStore.cs`
- `native/SkaiDesktop.Core.Tests/AgentTaskArtifactTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the cancellation contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including pre-execution cancellation and readiness exclusion.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: cancellation writes only a bounded local status
and reason. It does not access credentials, files, providers, network routes,
or process controls.

Rollback: remove `CancelPlanned` and the Activity cancel button; proposed and
queued plans remain viewable and queueable.

Not yet verified: deterministic live activation through the unavailable Uno
App MCP harness, packaged-artifact acceptance, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 122 — search bounded agent-task artifacts

Status: `implemented_verified_build`

Activity now offers `Search agent tasks`, a read-only dialog that searches the
bounded local task store by title, instruction, role, or status and renders up
to twenty matching plan summaries. The query is local-only, updates as typed,
and opening the dialog focuses the search field. No task status changes and no
agent, provider, tool, or background work is started.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the task-search accessibility
  contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including bounded local task search coverage.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: task search reads only bounded local metadata and
does not transmit query text or task content to any provider or network route.

Rollback: remove the search action/dialog and its audit contracts; the Activity
task cards and local task store remain available.

Not yet verified: deterministic live activation through the unavailable Uno
App MCP harness, packaged-artifact acceptance, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 123 — derive export filenames from product identity

The native chat, Activity evidence, and memory backup save dialogs now derive
their suggested filename stem from `ProductIdentity.ShortName`. This removes
the remaining hardcoded legacy product slug from those export workflows while
keeping the user-facing display identity centralized for the planned rebrand.

Evidence:

- `ProductIdentity.FileNameStem` strips punctuation and spaces and lowercases
  the current short name in one shared implementation.
- Chat archive, Activity evidence, and memory backup pickers all use that stem.
- The native core runner checks the current stem contract.
- No credentials, chat contents, or files are changed by this slice.

Privacy/security/licensing: this only changes local suggested filenames. It
does not alter archive contents, network behavior, or provider routing.

Rollback: remove `FileNameStem` and restore the three picker interpolations;
the export payloads and storage paths remain otherwise unchanged.

Not yet verified: live file-picker acceptance in a packaged Windows run and
Linux picker parity. The repository has no Uno App MCP surface available for
that UI automation path.

## Slice 124 — close the active-response message-queue backlog item

The future-features register now marks message queueing complete. This is
limited to the requirement's actual scope: while a response is active, a
bounded follow-up prompt can be accepted locally, reviewed through the Queue
control, and dispatched after the active request reaches its cleanup path.

Evidence already recorded in Slices 23, 24, 30–33, and 76:

- Core tests cover bounded capacity, attachment preservation, FIFO/reorder,
  remove/clear, persistence, restore, and drain behavior.
- Windows accessibility evidence shows `Stop generating response`, accepts a
  second prompt during the active request, exposes `Manage queued messages`,
  and dispatches the queued follow-up after the first request fails.
- The queue remains local and bounded; no provider request is made until the
  existing send lifecycle reaches its dispatch path.

Privacy/security/licensing: queued prompts and explicitly attached content
remain in the existing local bounded queue and use the existing attachment
consent/provider policy at dispatch. No new network, credential, shell, Git,
or third-party behavior is introduced.

Rollback: revert the checkbox and this evidence entry; the implemented queue
code remains unchanged and can be re-audited if the acceptance scope changes.

Limitations: successful live provider response, cross-device synchronization,
and Linux/package parity remain separate gaps and are not implied by this
backlog closure.

## Slice 125 — inspect live conversation organization controls

The exact Windows desktop build was launched and its navigation/sidebar
conversation surface was inspected without changing saved user data. The live
accessibility tree and rendered dialog exposed:

- folder creation plus chat archive import/export controls;
- per-chat Pin chat, Edit tags, Archive chat, and Fork from here actions;
- the tag editor's bounded comma-separated input, local-only disclosure, Save
  tags, and Cancel controls.

The action menu was opened and the tag editor was opened and cancelled. No
folder was created, no tag was saved, and no chat was pinned, archived, forked,
deleted, or exported. Companion visibility was briefly toggled by an imprecise
coordinate during navigation and restored to its prior hidden state; no pet
code or persisted pet setting was changed by the repository work.

Evidence:

- Live Windows accessibility state showed `Create conversation folder`,
  `Import saved chats`, `Export saved chats`, `Pin chat`, `Edit tags…`,
  `Archive chat`, and `Fork from here`.
- Live tag dialog showed `Chat tags` with the help text “Enter up to eight
  comma-separated tags. Each tag can contain up to 32 characters.” and the
  local-only disclosure.
- Existing Core persistence tests cover folder assignment, pins, tags,
  archived/fork metadata, and archive round trips.

Privacy/security/licensing: inspection was read-only for user conversation
data and made no provider, network, credential, shell, Git, or third-party
calls. The live check did not transmit or export content.

Rollback: documentation-only; no runtime or persisted data changes require
rollback.

Not yet verified: a complete disposable-data mutation/save/reload journey for
folder, pin, tags, archive, and fork actions, plus packaged/Linux accessibility
parity. The future-features item `Folders, tags, and pinned chats` therefore
remains unchecked.

## Slice 126 — fork a conversation from any message

The existing full-chat fork path now supports a `Fork from here` action on
every rendered message. The new fork copies the selected message prefix into a
new conversation, preserves the source chat and selected branch, and deep-copies
attachment/source lists so later edits cannot mutate the original conversation.
The existing recent-chat `Fork from here` action remains a full-conversation
fork.

Changed files:

- `native/SkaiDesktop.Core/Services/ConversationForkService.cs`
- `native/SkaiDesktop.Core.Tests/ConversationForkTests.cs`
- `native/ServerRock.Native/MainPage.Branches.cs`
- `native/ServerRock.Native/MainPage.Messages.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (172 contracts)`.
- `npm run check:syntax` — passed.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including selected-prefix bounds, full-fork behavior, attachment/source list isolation, and invalid-index rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after stopping the exact validation executable that held the output assembly.
- `git diff --check` — passed.

Privacy/security/licensing: forking is local-only and copies bounded existing
conversation data. It does not send content to a provider, create credentials,
invoke tools, access files, or add dependencies. The original conversation is
left unchanged.

Rollback: remove the per-message action and `ConversationForkService`; the
existing full-chat fork path can remain by restoring its prior clone expression.

Not yet verified: live activation of the per-message action and save/reload
acceptance in a disposable chat, plus packaged/Linux accessibility parity. The
future-features item `Fork a chat from any message` remains unchecked until
those journeys are exercised.

## Slice 127 — bounded search within the current conversation

The command palette now exposes `Find in current chat`. The dialog searches
only the messages already loaded in the active conversation, bounds the query
to 256 characters and results to 64 matches, renders bounded previews, and
offers `Jump to match`. The jump preserves the existing conversation state and
scrolls the selected message into view; no provider or web request is made.

Changed files:

- `native/SkaiDesktop.Core/Services/ConversationMessageSearchService.cs`
- `native/SkaiDesktop.Core.Tests/ConversationMessageSearchTests.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Composer.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (177 contracts)`.
- `npm run check:syntax` — passed.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including role/index matching, blank-query handling, query-length rejection, and bounded previews.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Privacy/security/licensing: search runs over the current in-memory message
list only. It adds no network, provider, credential, file, shell, Git, or
third-party behavior and does not persist a search query.

Rollback: remove the command-palette entry, search service, dialog, and audit
contracts; existing workspace-wide conversation search remains available.

Not yet verified: live Windows activation of the command-palette item, result
selection, and scroll jump; packaged/Linux accessibility parity. The new
backlog item remains unchecked until that native journey is exercised.

## Slice 128 — live current-chat search and jump acceptance

The exact Windows desktop executable was launched with an existing saved chat.
The command palette was opened with `Ctrl+K`, filtered to `Find in current
chat`, and the action was run. The dialog exposed the local-only help text,
bounded search field, result list, `Jump to match`, and `Close` controls. The
query `validation` returned both matching saved messages; the second result
was selected and the jump action closed the dialog, scrolled to the selected
message, exposed the accessible group `Conversation message 2`, and announced
`Jumped to message 2 of 2.`

No provider request, web search, file access, edit, fork, or saved-chat
mutation occurred. The exact validation executable was stopped after the
journey.

Evidence:

- Live Windows accessibility state exposed `Find in current chat` in the
  command-palette results.
- Live search state exposed `Search current conversation messages`, its
  no-provider help text, `Current conversation search results`, two matching
  list items, and `Jump to match`.
- Live post-jump state exposed `Conversation message 2` and the announced
  `Jumped to message 2 of 2.`
- The native Core search tests, UI contract audit, and desktop build remain
  green from Slice 127.

Privacy/security/licensing: the journey searched only already-loaded local
conversation content and transmitted no data. No dependency or third-party
code was added.

Rollback: documentation-only closure; reverting the checkbox and evidence
does not alter the implementation.

Limitations: Linux and packaged accessibility parity remain unverified. The
new feature is checked in the future-features register because the native
Windows journey is now evidenced; broader global search and semantic search
remain separate capabilities.

## Slice 129 — live review of approved memory context

The exact Windows desktop executable was launched and the command palette was
filtered to `Review memory context`. The dialog exposed a local read-only
disclosure, `Allow approved memory injection`, a bounded `Approved memory
context preview`, `Save memory setting`, and `Cancel`. With the current local
profile there were no eligible global or active-project notes, so the preview
truthfully stated that no notes were eligible. The dialog was cancelled; the
setting and memory store were not changed.

Evidence:

- Live Windows accessibility state exposed the dialog title and the complete
  injection/preview/save/cancel control set.
- The checkbox help text stated that only enabled global or active-project
  notes become untrusted reference context for the next request.
- The preview was read-only and contained no provider or web content.
- Native UI audit passed with 181 contracts, the full Core runner passed, and
  the desktop build passed before the live inspection.

Privacy/security/licensing: this journey read only local memory metadata and
did not transmit, persist, or alter it. Enabling the setting remains an
explicit user action and does not automatically send a prompt.

Rollback: revert the implementation commit to remove the command-palette
review surface; existing Settings memory management and the existing guarded
injection path remain independently removable.

Not yet verified: saving the toggle with an eligible disposable note, observing
the resulting prompt context before provider dispatch, cross-restart behavior,
and packaged/Linux accessibility parity. The broad persistent-memory backlog
item remains unchecked.

## Slice 111 — make Activity search keyboard-ready

Status: `implemented_verified_build`

The Activity search dialog now has an explicit accessible name, its query field
has a stable accessible label, and opening the dialog focuses the query field.
Search remains read-only and refreshes bounded local operation and workflow
event results as the query changes.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (150 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: Activity search reads only bounded local evidence
already present in the journal. Query text is not sent to a provider and no
task, file, credential, or network state is changed.

Rollback: remove the query/dialog labels and focus handler plus their audit
contracts; the existing read-only search behavior remains.

Not yet verified: live keyboard activation of every Activity action, seeded
live workflow-event search matching, full status-history keyboard/screen-reader
acceptance, OS session hosting, parallel-agent artifacts, Linux parity, and
packaged acceptance.

## Slice 96 — extract the main page header into a dedicated visual control

Status: `implemented_verified_build`

The giant page visual tree now delegates its header to `MainPageHeader`, a
dedicated UserControl. The extracted control owns navigation buttons, route
badges, branch controls, connection status, and the new-chat action. It
forwards semantic events to `MainPage` and exposes only the named controls
that existing feature partials need, preserving behavior while making the
layout independently maintainable. The pet-owned companion overlay was not
changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPageHeader.xaml`
- `native/ServerRock.Native/MainPageHeader.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`

Privacy/security/licensing: this is a local visual decomposition only. It
changes no persistence, provider route, credential handling, prompt content,
or third-party dependency.

Rollback: restore the header Grid in `MainPage.xaml`, remove
`MainPageHeader.xaml` and `.xaml.cs`, remove the header event subscriptions and
compatibility accessors, and keep the lifecycle split intact.

Not yet verified: live packaged-release interaction after the visual split,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 97 — continue the main page visual split with the Coding Agent bar

Status: `implemented_verified_build`

Continuing the Slice 95/96 UI decomposition, the Coding Agent workspace bar is
now a dedicated `CodingWorkspaceBar` UserControl. It owns the project summary,
folder count, project/GitHub actions, terminal action, undo-history action, and
responsive toolbar visual tree. MainPage keeps the existing feature methods
through forwarded events and compatibility accessors, so project behavior and
responsive layout logic remain unchanged. The pet-owned companion overlay was
not changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no project persistence, GitHub credential handling, command execution,
provider route, or third-party dependency.

Rollback: restore the Coding Agent workspace-bar Grid in `MainPage.xaml`,
remove `CodingWorkspaceBar.xaml` and `.xaml.cs`, and remove its lifecycle
subscriptions and accessors.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 101 — live Activity & Tasks acceptance evidence

Status: `verified_windows_accessibility_inspection`

The current Windows desktop build was launched from the synchronized checkout
and the existing Activity & Tasks journey was inspected without creating,
editing, cancelling, or deleting user data. The navigation surface exposed
`Open activity and tasks`; the dialog exposed `Activity & tasks`, `Export
activity evidence`, `Search activity evidence`, and `Schedule task`. It also
rendered completed and failed provider-attempt records with model/provider,
local-route, run identity, and sequence metadata, plus an interrupted
operation with separate accessible `Mark interrupted operation cancelled` and
`Mark interrupted operation failed` decisions. This closes the prior gap for
live rendering of provider-attempt events and reconciliation choices; it does
not claim that every action was activated.

Evidence:

- Windows native app launched from `scripts/start-native.cjs` and remained live.
- Computer-use accessibility tree exposed the Activity & Tasks popup and its
  accessible action labels and result rows.
- The rendered surface showed completed/failed entries and an interrupted run
  with both bounded decision buttons.

Privacy/security/licensing: inspection read only existing local activity
records. No provider request, export, schedule creation, credential use,
network transmission, or third-party dependency was introduced.

Rollback: documentation-only; remove this evidence entry and the corresponding
capability-matrix wording if the live surface is later invalidated.

Not yet verified: keyboard activation of every Activity action, seeded live
workflow-event matching, full status-history screen-reader acceptance, OS
session hosting, parallel-agent artifacts, Linux parity, and packaged
acceptance.

## Slice 102 — verify scheduled work surfaces for review without dispatch

Status: `verified_windows_accessibility_inspection`

The live Windows Activity & Tasks surface contained the completed scheduler
record `Review scheduler acceptance` with the explicit outcome `Surfaced in the
composer for review; no provider request was sent.` This verifies the intended
safe boundary between due scheduling and model dispatch: a due task can become
reviewable work without silently sending a provider request. The evidence came
from the existing local activity record; no new task was created and no task
was edited or deleted.

Evidence:

- The Activity & Tasks accessibility tree exposed the completed scheduler row,
  its one-time workspace metadata, and the no-dispatch outcome text.
- The same surface exposed the separate `Schedule task` action for future
  work, confirming scheduling remains an explicit UI path.

Privacy/security/licensing: this was read-only inspection of local scheduler
and activity data. No prompt was sent, no provider or cloud route was used,
and no dependency or third-party code was added.

Rollback: documentation-only; remove this evidence entry if a later live run
contradicts the recorded scheduler outcome.

Not yet verified: recurring-task creation and pause/resume in the current
build, reviewed workflow execution adapters, OS session hosting, automatic
result capture, timezone/backoff policy, parallel agents, Linux parity, and
packaged acceptance.

## Slice 103 — fresh Windows target build evidence

Status: `verified_windows_build`

After stopping only the disposable native development process launched for
live inspection, the synchronized checkout built the Windows App SDK target
successfully. This confirms that the recent visual-control decomposition and
the current handoff state compile for both the shared Skia desktop target and
the Windows target; it does not claim packaged installer or clean-machine
acceptance.

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — previously passed for the current slice.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors; produced `bin/Debug/net10.0-windows10.0.26100/win-x64/ServerRock.Native.dll`.

Privacy/security/licensing: compilation only; no application data, provider,
credential, network, installer, or third-party dependency was accessed or
changed.

Rollback: documentation-only; remove this evidence entry if the build is not
reproducible from the recorded commit.

Not yet verified: signed installers, clean-machine install/upgrade/rollback/
uninstall, Linux artifact/build/run evidence from this commit, packaged UI
interaction, and release-feed publication.

## Slice 104 — current Linux validation boundary recorded

Status: `blocked_by_host_toolchain`

The current checkout was checked against the installed Ubuntu 24.04 WSL
environment. WSL launched the distro, but the validation command stopped before
the repository build because `dotnet` is not installed in that distro:

```text
wsl.exe -d Ubuntu-24.04 -- bash -lc "cd '/mnt/e/Server Rock Ai' && dotnet --version ..."
bash: line 1: dotnet: command not found
```

This is a host-toolchain blocker for fresh Linux evidence, not a code failure.
The Windows desktop and Windows App SDK builds remain independently verified.
The matrix continues to mark Linux parity `unverified` until a supported .NET
10 SDK is available in WSL or a Linux CI runner can execute the build, Core
checks, display smoke, and packaging checks.

Privacy/security/licensing: the attempt only read the repository and queried
the local WSL toolchain. No application data, provider, credentials, network
route, or dependency was changed.

Rollback: documentation-only; remove this boundary entry if a later Linux
environment produces current-checkout evidence.

Not yet verified: Linux build/run, accessibility, packaging, model-server
connectivity, writable profile paths, and exact supported distro scope.

## Slice 105 — repository hygiene, release configuration, and rebrand gate

Status: `verified_windows_repository_gates`

The current synchronized repository passed the release/security hygiene gates.
The runtime display identity search also confirmed that the product name is
centralized in `native/SkaiDesktop.Core/Models/ProductIdentity.cs` rather than
duplicated through feature UI code, preserving the ability to rebrand the app
without editing every surface. Release packaging metadata remains an explicit
manifest/configuration input, not a hidden runtime fallback.

Evidence:

- `npm run check:hygiene` — passed: `Repository hygiene check passed (325 tracked files).`
- `npm run check:release-config` — passed: `Release configuration is internally consistent.`
- `npm run check:syntax` — passed.
- Source search across `native`, `scripts`, and `packaging` found runtime
  display identity in `ProductIdentity` and build/package identity inputs, with
  no scattered feature-level hardcoded display-name literals.

Privacy/security/licensing: the hygiene gate rejects tracked secrets and
generated artifacts; the release validator does not access credentials unless
signing mode is explicitly enabled. The identity check changes no data flow,
provider routing, persistence, or dependency set.

Rollback: documentation-only; remove this evidence entry if the gate outputs or
identity ownership changes.

Not yet verified: signed release publication, clean-machine install/upgrade/
rollback/uninstall, OS isolation, keyring parity, full negative security suite,
and Linux parity.

## Slice 106 — future-features backlog baseline verified

Status: `verified_repository_backlog`

The requested future-features list is present and tracked at
`docs/FUTURE_FEATURES.md`. It currently contains 159 unchecked feature items
across Core chat, Agents and tools, Retrieval and knowledge, Workspace and
desktop experience, Models/runtime/extensibility, Administration and teams,
Files/environments/delivery, Content/meetings, and Ecosystem/platform. The
document explicitly requires each item to be implemented as a bounded vertical
slice with privacy, platform, licensing, failure-state, and acceptance
evidence; it also preserves the AirLLM link as a research reference rather
than an automatic dependency or implementation instruction.

Evidence:

- Repository inspection: `docs/FUTURE_FEATURES.md` exists on synchronized
  `main` and contains 159 unchecked `- [ ]` feature entries.
- The backlog includes the requested future areas such as multi-model
  selection/comparison, RAG, computer use, MCP, workflows, persistent tabs,
  memory, integrations, meetings, release/runtime options, and platform
  differentiators.

Privacy/security/licensing: this is a local documentation baseline. No feature
was silently enabled, no external project was copied, and no dependency,
credential, provider route, or user data was changed.

Rollback: documentation-only; remove this evidence entry if the backlog source
is replaced by a newer approved product register.

Not yet verified: each future item remains independently tracked as incomplete
until its implementation and acceptance evidence are recorded in a later
slice.

## Slice 107 — future-features backlog integrity audit

Status: `implemented_verified_repository_tooling`

Added a deterministic repository audit for `docs/FUTURE_FEATURES.md`. It
requires the core backlog sections, validates checkbox syntax, rejects empty
or duplicate feature labels, and reports the item/section/checked counts. The
audit is available as `npm run audit:future-features` and its script is included
in `npm run check:syntax`, so future backlog edits fail visibly before they
become the planning source for another implementation slice.

Changed files:

- `scripts/audit-future-features.cjs`
- `package.json`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:future-features` — passed: `159 items, 11 sections, 0 checked`.
- `npm run check:syntax` — passed, including the new audit script.
- `npm run check:hygiene` — passed: `325 tracked files`.
- `git diff --check` — passed.

Privacy/security/licensing: this is local documentation validation only. It
does not execute feature content, access user data, add dependencies, or alter
runtime/provider behavior.

Rollback: remove `scripts/audit-future-features.cjs`, its package scripts, and
this evidence entry; the future-features document remains readable manually.

Not yet verified: implementation and acceptance of the individual future
features, which remain deliberately unchecked.

## Slice 100 — extract the composer input surface from MainPage.xaml

Status: `implemented_verified_build`

The rest of the composer visual tree is now a dedicated `ComposerInputView`
UserControl. It owns the attachment/status area, queue and status-history
actions, slash-command hints, prompt editor, voice controls, stop/send actions,
and privacy footnote. It hosts the already extracted `ComposerToolbarView` and
forwards existing events and control accessors, preserving chat, queue,
attachments, commands, voice, and focus behavior. The pet-owned companion
overlay was not changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/ComposerInputView.xaml`
- `native/ServerRock.Native/ComposerInputView.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no attachment policy, prompt content, provider request, voice data
handling, persistence, credential handling, or third-party dependency.

Rollback: restore the composer StackPanel and children in `MainPage.xaml`,
remove `ComposerInputView.xaml` and `.xaml.cs`, and remove its compatibility
accessors. The separate toolbar extraction can remain or be reverted
independently.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 99 — extract the composer toolbar from MainPage.xaml

Status: `implemented_verified_build`

The composer toolbar is now a dedicated `ComposerToolbarView` UserControl. It
owns the model picker, tools disclosure, optional web/temporary/project-file
toggles, message-edit cancellation, smart rewrite, image studio, and advanced
controls. It forwards the existing feature actions and responsive size-change
hook to MainPage, preserving the current compact/expanded layout behavior.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/ComposerToolbarView.xaml`
- `native/ServerRock.Native/ComposerToolbarView.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no model routing, web-search behavior, local-file consent, persistence,
credential handling, or third-party dependency.

Rollback: restore the composer-toolbar Grid in `MainPage.xaml`, remove
`ComposerToolbarView.xaml` and `.xaml.cs`, and remove its lifecycle
subscriptions and compatibility accessors.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 98 — extract the conversation surface from MainPage.xaml

Status: `implemented_verified_build`

The remaining conversation viewport above the composer is now a dedicated
`ConversationSurface` UserControl. It owns the welcome state, connection setup
action, prompt suggestions, and scrollable message host. MainPage retains the
existing chat/message behavior through forwarded setup and suggestion events
and bounded compatibility accessors for the scroll and message children. The
composer and pet-owned companion overlay were not changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/ConversationSurface.xaml`
- `native/ServerRock.Native/ConversationSurface.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no conversation persistence, provider request, credential handling,
or third-party dependency.

Rollback: restore the welcome/conversation Grid and its children in
`MainPage.xaml`, remove `ConversationSurface.xaml` and `.xaml.cs`, and remove
the lifecycle subscriptions and accessors.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 81 — deterministic date/time variables in reviewed skill prompts

Status: `implemented_verified_core`

Built-in and installed Markdown skill instructions now expand the bounded,
deterministic variables `{{DATE}}`, `{{TIME}}`, and `{{DATETIME}}` before the
prompt is placed in the composer for user review. Unknown variables remain
unchanged, and the prompt is still visible and editable before it can be sent.
Expansion is limited to local date/time values; it does not read account data,
environment variables, files, or network state.

Changed files:

- `native/SkaiDesktop.Core/Services/PromptVariableExpander.cs`
- `native/SkaiDesktop.Core.Tests/PromptVariableTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including deterministic expansion, case-insensitive names, unknown-variable preservation, and control-character rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (131 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only the local clock is consulted. No username,
credentials, files, provider metadata, network data, dependency, or
third-party code is introduced. The existing explicit skill enablement and
user-review-before-send boundary remains in place.

Rollback: remove `PromptVariableExpander`, its Core test and runner entry, the
two skill-loading expansion calls, and the UI audit contract. Skill loading
then returns the original Markdown instructions unchanged.

Not yet verified: live Windows skill insertion with a variable-bearing
installed package, user-configurable variables such as `{{USER_NAME}}`, and
cross-platform/package acceptance.

## Slice 82 — explicit opt-in user-name prompt variable

Status: `implemented_verified_core`

The reviewed skill prompt path now supports `{{USER_NAME}}` only when the user
has entered an explicit local `Prompt variable name (optional)` setting. An
unset value leaves the token visible for review, and the app never reads the
Windows account name automatically. The setting is normalized through the
existing version-tolerant local settings store and is only substituted into a
prompt the user can inspect and edit before sending.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core/Services/PromptVariableExpander.cs`
- `native/SkaiDesktop.Core.Tests/PromptVariableTests.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including explicit-name substitution, unset-token preservation, and unsafe-value rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (133 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the value is user-entered, bounded, stored in
local non-secret settings, and never inferred from the operating system. No
new network request, credential, dependency, or third-party code is involved.

Rollback: remove `PromptUserName`, its settings normalization and UI field, and
the optional expander argument; existing date/time variables and reviewed
skill loading can remain, or the full variable expander can be removed with
Slice 81's rollback.

Not yet verified: live Windows persistence and insertion of a variable-bearing
installed package, cross-platform/package acceptance, and additional
user-configurable variables beyond `{{USER_NAME}}`.

## Slice 83 — durable provider-attempt provenance

Status: `implemented_verified_core`

Chat operation runs now append bounded provider-attempt evidence for started,
completed, failed, and user-cancelled requests. Each event records the
provider, model, privacy classification, outcome, and an optional bounded
reason, while intentionally excluding endpoint URLs, API keys, prompts, and
response content. This makes the selected route and failure outcome auditable
without changing provider routing or adding automatic fallback behavior.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/OperationJournal.cs`
- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/SkaiDesktop.Core.Tests/OperationServiceTests.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including durable started/failed provider-attempt events and bounded provenance fields.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (134 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: event payloads are local-only, bounded, and
redacted by design; no endpoint URL, credential, prompt, response, dependency,
or third-party code is recorded. The existing explicit model-selection and
no-automatic-fallback boundary remains unchanged.

Rollback: remove `OperationJournal.AppendEvent`,
`OperationService.RecordProviderAttempt`, the Chat lifecycle calls, its test,
and the UI audit contract. Existing status-transition journal evidence remains
available.

Not yet verified: live Windows Activity rendering of provider-attempt events,
workflow/provider attempts outside chat, and cross-platform/package acceptance.

## Slice 84 — bounded OpenAPI 3 operation discovery

Status: `implemented_verified_core`

Core now parses OpenAPI 3.x JSON documents into bounded, reviewable operation
descriptors. It discovers path/method names, summaries, parameter schemas, and
JSON request-body schemas; classifies safe read methods versus mutating
methods; and attaches the corresponding MCP read/write capability requirement.
The slice is discovery-only: it does not fetch a document, resolve `$ref`
values, create credentials, execute an operation, or grant permissions.

Changed files:

- `native/SkaiDesktop.Core/Models/OpenApiDiscoveredOperation.cs`
- `native/SkaiDesktop.Core/Services/OpenApiToolDiscovery.cs`
- `native/SkaiDesktop.Core.Tests/OpenApiToolDiscoveryTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including read/write classification, bounded schema preservation, unsupported-version rejection, and credential-bearing source-identity rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (134 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the parser consumes caller-provided JSON only and
performs no network or code execution. Discovered metadata is untrusted and
does not authorize MCP calls; future execution must reuse explicit grants and
the existing redirect, credential, capability, and payload boundaries. No
dependency or third-party code was added.

Rollback: remove the operation model, parser, tests, and runner entry. Existing
MCP discovery and permission gates remain unchanged.

Not yet verified: UI presentation, authenticated document retrieval, `$ref`
resolution, explicit per-operation grants, execution adapters, live
integration acceptance, Linux parity, and packaged acceptance.

## Slice 85 — visible local OpenAPI review journey

Status: `implemented_verified_windows_with_execution_boundary`

The Plugins surface now exposes a reachable `Review local OpenAPI schema`
action. It opens a local JSON-only file picker, reads the selected document
through the bounded UTF-8 reader, parses it with the Core discovery service,
and presents the resulting operations in a dedicated review dialog with method,
path, capability, mutating status, and input-schema details. The review dialog
does not fetch a server, execute an operation, create credentials, or grant
MCP permission.

Changed files:

- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after the dev process was closed.
- `npm run audit:native-ui` — passed: `Native UI audit passed (137 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility/surface inspection — passed: the Plugins dialog visibly exposed `Review local OpenAPI schema`; activating it opened the native picker filtered to `JSON files`. The picker was cancelled without selecting a file, and the dev app was closed afterward. No user data, settings, pet state, credential, or network route changed.

Privacy/security/licensing: only a user-selected local JSON file is read,
within the existing 1,000,000-character parser bound and UTF-8 validation. The
schema remains untrusted; no operation can run from this surface. No
dependency, network request, credential, or third-party code was added.

Rollback: remove the OpenAPI review button, picker/review dialog, and UI audit
contracts. Core schema discovery remains available for a future integration
surface.

Not yet verified: selecting and rendering a real user-provided OpenAPI file,
authenticated document retrieval, explicit per-operation
grants, execution adapters, live integration acceptance, Linux parity, and
packaged acceptance.

## Slice 86 — bounded local OpenAPI component references

Status: `implemented_verified_core`

Core OpenAPI discovery now resolves bounded local component schema references
from `#/components/schemas/...` while building reviewable input schemas. It
rejects external, malformed, missing, or excessively nested references before
returning a descriptor; it does not retrieve remote schemas or execute an
operation.

Changed files:

- `native/SkaiDesktop.Core/Services/OpenApiToolDiscovery.cs`
- `native/SkaiDesktop.Core.Tests/OpenApiToolDiscoveryTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed: `Native core checks passed.`
- `git diff --check` — passed.

Privacy/security/licensing: resolution is limited to the in-document
`components.schemas` object, with a maximum reference depth of 8 and bounded
component names. External references fail closed, so this slice adds no
network access, credentials, dependency, or implicit authorization.

Rollback: remove the local reference resolver and its focused tests; direct
inline schemas remain supported.

Not yet verified: successful selection/rendering of a real user-provided file,
authenticated retrieval, explicit per-operation grants, execution adapters,
live integration acceptance, Linux parity, and packaged acceptance.

## Slice 87 — manifest-driven release launchers

Status: `implemented_verified_script_level`

The native packaging layer now reads display and launcher metadata from
`release-manifest.json`. Windows Start Menu shortcuts and optional launches
target the executable inside the versioned `current` directory instead of a
copied top-level EXE shim, preserving adjacent runtime files. Rollback no
longer recreates a stale shim, and Linux uses the same manifest metadata for
its install folder, command launcher, and desktop entry. The Windows setup
default now resolves the archive root from the packaging script directory,
matching the native artifact workflow.

Changed files:

- `packaging/native/release-manifest.json`
- `packaging/native/install.ps1`
- `packaging/native/setup.ps1`
- `packaging/native/rollback.ps1`
- `packaging/native/uninstall.ps1`
- `packaging/native/setup.sh`
- `packaging/native/README.md`

Evidence:

- PowerShell parser validation — passed for all four `.ps1` scripts.
- `npm run audit:native-ui` — passed: `Native UI audit passed (137 contracts).`
- `git diff --check` — passed.
- Linux shell syntax validation was not available on this Windows host because
  WSL could not start; no Linux acceptance claim is made.

Privacy/security/licensing: the change only affects installation paths,
launcher targets, and display metadata. The existing user-data location is
preserved, package contents are still copied into a versioned directory, and
no credentials, network route, dependency, or third-party code was added.

Rollback: restore the prior packaging scripts and release manifest. Existing
explicit `-InstallRoot` overrides remain supported for recovery.

Not yet verified: clean-machine Windows install/upgrade/rollback/uninstall,
unsigned-package rejection, Linux packaging, signed artifacts, and packaged
release acceptance.

## Slice 88 — bounded actionable status history

Status: `implemented_verified_core_static_ui_and_windows_surface`

The runtime status banner now keeps a bounded in-memory history of actionable
messages. Dismissing a banner no longer loses the failure context: the banner
exposes an accessible `Status history` action that shows the newest messages
first in a scrollable review dialog. Informational messages continue to use
the existing short-lived banner behavior, and consecutive duplicate failures
are coalesced.

Changed files:

- `native/SkaiDesktop.Core/Services/StatusMessageHistory.cs`
- `native/SkaiDesktop.Core.Tests/StatusMessageHistoryTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including bounded ordering, duplicate suppression, blank-message handling, and oversized-message truncation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (140 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility/screenshot inspection — passed at the surface
  boundary: after an actionable `Unknown command. Type / to see available
  commands.` status, the app exposed `View status history` with its help text
  and retained the status banner. Dialog activation was not claimed because
  this run's native geometry bridge did not provide a reliable click target.

Privacy/security/licensing: history is process-local and bounded to 50
messages of at most 2,000 characters each. It is not persisted, sent to a
provider, or written into conversation history; no dependency or third-party
code was added.

Rollback: remove `StatusMessageHistory`, the history button/dialog, and its
audit contracts; the existing banner and dismissal behavior remain available.

Not yet verified: live Windows keyboard/screen-reader activation of the new
history dialog, cross-platform accessibility behavior, persistent export, and
full NotificationCenter severity/action semantics.

## Slice 89 — explicit bounded memory injection

Status: `implemented_verified_core_and_native_build`

The existing local memory editor now has a separate global consent switch for
including enabled memory in future model requests. The switch is off by
default and is saved only when the user chooses Done in the Memory dialog.
When enabled, prompt assembly includes only enabled `global` notes and notes
scoped to the active project, labels them as untrusted reference material, and
caps the complete context section at 6,000 characters. Disabled, unrelated,
or unavailable memory is not injected; malformed memory fails closed for
context while the prompt continues without it and exposes a status message.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Services/MemoryPromptContext.cs`
- `native/SkaiDesktop.Core.Tests/MemoryPromptContextTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including explicit opt-in, scope filtering, disabled-entry filtering, reference labeling, and the 6,000-character bound.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (141 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: injection is disabled by default and is not
learned from chats. Enabling it can send approved local notes to the selected
endpoint, including a remote endpoint; the Memory dialog discloses this before
the switch is used. Context is bounded and marked as untrusted reference
material. No dependency or third-party code was added.

Rollback: remove the global setting, `MemoryPromptContext`, prompt assembly
call, and consent control; local memory editing and backup remain available.

Not yet verified: live Windows toggle/save/send acceptance, remote-endpoint
consent wording during an actual injected request, cross-platform UI behavior,
semantic retrieval, and larger-context support.

## Slice 90 — reviewed scheduled-task run-now control

Status: `implemented_verified_core_and_native_build`

Activity & tasks now exposes a Run now action for scheduled tasks. The action
only moves the durable task's due time to the current instant, claims it through
the existing scheduler boundary, and surfaces the prompt in the composer for
human review. It does not send a provider request, bypass model availability,
remote-data consent, or the normal Send control. Paused, completed, failed, and
already-running tasks remain ineligible for this action.

Changed files:

- `native/SkaiDesktop.Core/Services/ScheduledTaskStore.cs`
- `native/SkaiDesktop.Core.Tests/ScheduledTaskTests.cs`
- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the durable run-now and claim path.
- native build — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (142 contracts)`, including the accessible run-now control.
- `git diff --check` — passed after the final patch.

Privacy/security/licensing: the action is local-only state manipulation and
does not dispatch network, model, shell, file, or Git work. The existing review
and send boundaries remain in force. No dependency or third-party code was
added.

Rollback: remove `ScheduledTaskStore.RunNow`, the activity control, and its
tests/audit contract; scheduled creation, pause/resume, and automatic due
processing remain available.

Not yet verified: live Windows activation of the new control, cross-platform UI
behavior, OS-level background execution, and durable task synchronization across
multiple app instances.

## Slice 91 — bounded workspace navigation history

Status: `implemented_verified_core_and_native_build`

The native shell now has a bounded, branch-aware navigation history for the Chat
and Coding Agent workspace routes. Back and Forward controls are visible in the
header, expose accessible names/help text, disable when no route is available,
and restore the existing workspace switching path so temporary-chat safeguards,
draft isolation, and project context rules still apply. A new route after going
back truncates forward history; history is capped at 32 entries and is session
state only, so it does not alter conversations or user files.

Changed files:

- `native/SkaiDesktop.Core/Services/NavigationHistory.cs`
- `native/SkaiDesktop.Core.Tests/NavigationHistoryTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including bounded history, back/forward movement, and forward-branch truncation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed after adding the Back/Forward contracts.
- `git diff --check` — passed.

Privacy/security/licensing: route history contains only bounded local route
identifiers; it does not persist prompts, model data, credentials, or files. No
dependency or third-party code was added.

Rollback: remove `NavigationHistory`, the header controls, route hooks, tests,
and audit contracts; existing direct workspace switching remains available.

Not yet verified: live Windows activation of both buttons, persistent tabs or
layout state, dialog/overlay route history, Linux/Skia interaction, and narrow
viewport focus acceptance.

## Slice 92 — durable workspace-route history

Status: `implemented_verified_core_and_native_build`

The bounded Chat/Coding Agent route history from Slice 91 now survives settings
save/reload. Restore is defensive: invalid or control-character route values
are skipped, the list remains bounded to 32 entries, and an empty/corrupt list
falls back to the current Chat route. Back/Forward still operate only on the
existing workspace-switch path, so temporary-chat confirmation, draft isolation,
and project context safeguards remain active. No prompt, conversation, file,
credential, endpoint, or model data is stored in this route history.

Changed files:

- `native/SkaiDesktop.Core/Services/NavigationHistory.cs`
- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core.Tests/NavigationHistoryTests.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including route restoration, invalid-route recovery, settings round-trip, and bounded history.
- native build — passed with 0 warnings/errors.
- UI audit — passed with the existing 144 native contracts.
- `git diff --check` — passed.

Privacy/security/licensing: only bounded route identifiers are persisted in the
existing local settings file. Invalid persisted values fail closed. No
dependency or third-party code was added.

Rollback: remove `WorkspaceRouteHistory` persistence and restore calls; Slice 91
can continue using session-only navigation history.

Not yet verified: persistent tabs/layout geometry, dialog/overlay routes, live
restart Back/Forward activation, Linux/Skia interaction, and narrow viewport
focus acceptance.

## Slice 93 — bounded side-by-side pinned-model comparison

Status: `implemented_verified_core_and_native_build`

The model quick-switch menu now offers a comparison journey when at least two
pinned compatible models exist. The user selects two to four models and enters
one plain-text prompt. The policy deduplicates selections, bounds the prompt,
honors local-only endpoint policy, and rejects invalid model connections before
dispatch. Comparison requests exclude attachments, saved memory, project files,
and web results. Requests run in parallel, remote endpoints require explicit
consent, and each model renders its own response or classified failure so one
failure does not erase partial results. API keys remain session-only.

Changed files:

- `native/SkaiDesktop.Core/Services/ModelComparisonPolicy.cs`
- `native/SkaiDesktop.Core.Tests/ModelComparisonPolicyTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including deduplication, minimum selection, local-only routing, and prompt bounds.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts)`.
- `git diff --check` — passed.

Privacy/security/licensing: comparison is opt-in, plain-text only, and keeps
remote routing visible. No cloud request is made silently; no credentials are
written to settings. Output is bounded before rendering. No dependency or
third-party code was added.

Rollback: remove the comparison policy, menu action, consent set, result dialog,
tests, and audit contracts; existing model switching remains unchanged.

Not yet verified: live Windows comparison with two healthy pinned models, live
remote-consent acceptance, provider-specific model capabilities, durable result
history, and Linux/packaged accessibility behavior.

## Slice 94 — exact Windows Release artifact smoke correction

Status: `implemented_verified_windows_artifact_smoke`

The exact current Release `win-x64` self-contained artifact was published and
smoke-tested. The artifact contains 496 files and 320,042,070 bytes. The bounded
startup smoke passed for eight seconds, and the strict startup-plus-top-level-
window smoke passed for eight seconds. During verification, the window check
initially failed because the smoke harness always launched the child with
`windowsHide: true`; the harness now keeps the child visible when
`NATIVE_SMOKE_REQUIRE_WINDOW=1`, then the strict check passed against the same
published executable.

Changed files:

- `scripts/smoke-native-artifact.cjs`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet publish native/ServerRock.Native/ServerRock.Native.csproj -c Release -f net10.0-desktop -r win-x64 -p:SelfContained=true --output <temporary artifact>` — passed; 496 files, 320,042,070 bytes.
- `npm run smoke:native:artifact -- <exact ServerRock.Native.exe>` with `NATIVE_SMOKE_SECONDS=8` — passed.
- The same command with `NATIVE_SMOKE_REQUIRE_WINDOW=1` — passed after the harness fix.
- `node --check scripts/smoke-native-artifact.cjs` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the smoke ran the published app with a disposable
temporary output directory and did not claim installer, signing, cloud, or
Linux acceptance. No user profile data was targeted and no dependency was added.

Rollback: revert the `windowsHide` conditional and the evidence-only document
updates; the ordinary startup smoke remains available.

Not yet verified: signed release publication, clean-machine install/upgrade/
uninstall/rollback, exact Linux artifact interaction, and full accessibility or
workflow acceptance from the packaged artifact.

## Slice 95 — disposable installer journey and MainPage infrastructure split

Status: `implemented_verified_windows_scripts_and_native_build`

The versioned Windows packaging scripts were exercised against a disposable
Release artifact and disposable application-data roots. `install.ps1 -WhatIf`
showed the expected target without mutation; version `1.0.0` installed, version
`1.0.1` upgraded it into `current` while retaining `previous`, rollback restored
`1.0.0`, and uninstall removed only the install root. A profile file under the
disposable `%LOCALAPPDATA%\\ServerRock\\AI` retained an identical hash after
upgrade, rollback, and uninstall. This does not claim clean-machine or signed
installer acceptance.

As part of the same release-readiness slice, the cross-cutting status history,
guarded UI action wrapper, and settings persistence were moved from the large
`MainPage.xaml.cs` state/lifecycle file into `MainPage.Infrastructure.cs`.
This is a partial-class decomposition only: event names, persistence behavior,
status semantics, and feature APIs are unchanged. The pets partial remains
untouched.

Changed files:

- `packaging/native/install.ps1` — exercised, no source change
- `packaging/native/rollback.ps1` — exercised, no source change
- `packaging/native/uninstall.ps1` — exercised, no source change
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `docs/ARCHITECTURE.md`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- Exact Release artifact: 496 files, 320,042,070 bytes; startup and strict
  startup/window smoke passed for 8 seconds.
- Disposable installer journey: WhatIf preview, install, versioned upgrade,
  rollback, profile hash retention, and uninstall all passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts)`.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: all installer paths were disposable and did not
target the real profile. Uninstall was run without `-RemoveUserData`; explicit
data deletion remains confirmation-gated. The MainPage split adds no dependency
or runtime behavior and does not move secrets or user data.

Rollback: restore the moved methods to `MainPage.xaml.cs` and remove
`MainPage.Infrastructure.cs`; packaging scripts remain unchanged. The temporary
test roots were separate from the repository and user profile.

Not yet verified: clean-machine installer behavior, shortcut launch acceptance,
signing, interrupted replacement recovery, exact Linux packaging, and live
keyboard/screen-reader interaction for the decomposed page.

## Slice 76 — live queued-follow-up journey

Status: `implemented_verified_windows_failure_path`

The existing native queue was exercised end to end on Windows without changing
the queue implementation. A second prompt was entered while a local response
was active; the composer accepted it, the queue manager became accessible, and
the bounded queue status reported one waiting item. The first local provider
request then failed, after which the queued follow-up appeared in the
conversation and the automatic dispatch path produced its own provider failure.
This verifies ordering and failure-path dispatch, not successful model output.

Evidence:

- Live Windows accessibility inspection exposed `Stop generating response`
  while the first request was active.
- A second prompt was accepted while the first request was active; the tree
  exposed `Manage queued messages` with the review/remove/clear help text and
  `Queued message 1 of 8 · it will send when the current response finishes.`
- After the first local request failed, the tree exposed the queued follow-up
  text followed by a second `Connection error`; the queue control was gone,
  showing the queued item had been removed before dispatch.
- No remote endpoint, credential, file attachment, shell, Git action, or pet
  functionality was used or changed.

Privacy/security/licensing: test prompts stayed in the local conversation and
queue store. The journey used the already configured local endpoint and did not
introduce network routing or third-party code.

Rollback: no production code change was made in this evidence slice; retain
the existing bounded queue behavior and its failure handling.

Not yet verified: successful live provider response, live queue reorder/remove/
clear button activation, cross-device synchronization, durable context
continuity, packaged Linux parity, and approval-inbox live acceptance. The
next dependency remains a deterministic local test endpoint or healthy local
model for successful dispatch evidence.

## Slice 77 — persist live approval expiry

Status: `implemented_verified_core`

The Activity approval countdown now persists expired approvals on the live
timer tick instead of waiting for the Activity surface to be reopened. Expired
buttons remain disabled, the status reports that no dispatch occurred, and the
existing service idempotence prevents repeated timer ticks from appending
duplicate expiry events.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the regression that resolves an unrelated pending fixture approval, expires the target approval, invokes expiry again, and verifies one durable `approval.expired` event.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: expiry is local durable workflow state. It does
not dispatch a provider, tool, shell, file, Git, or network action and adds no
credentials, dependency, or third-party code.

Rollback: remove the timer call and status update; the existing open-time
expiry behavior and approval controls remain as the weaker fallback.

Not yet verified: live Windows seeded approval expiry/countdown acceptance,
successful workflow execution, richer branching, parallel limits, OS session
hosting, Linux parity, and packaged acceptance.

## Slice 78 — enforce workflow dependency readiness

Status: `implemented_verified_core`

`WorkflowRunService.BeginStep` now refuses to start a pending step until every
declared dependency is complete or skipped. This closes a state-machine hole
that could otherwise bypass the persisted DAG order, and it provides the safe
readiness boundary needed before parallel branches or richer adapters are
introduced. Unsupported node types remain refused by their existing executor
gates.

Changed files:

- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including refusal to begin the approval step before its `test` dependency and the complete existing workflow, queue, MCP, memory, skill, and release checks.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this is a local state-machine guard. It prevents
out-of-order execution and adds no provider, network, shell, file, Git,
credential, dependency, or third-party path.

Rollback: remove the dependency check and regression assertion; the existing
topological plan and conservative unsupported-node refusal remain available.

Not yet verified: live workflow run acceptance, richer branching semantics,
parallel resource limits, automatic scheduling, side-effecting adapters,
Linux parity, and packaged acceptance.

## Slice 79 — searchable workflow-event evidence

Status: `implemented_verified_with_live_surface_boundary`

Activity search now searches both the durable operation journal and the
workflow-run event journal. Workflow results are bounded to the latest 100
events, match event type/actor/run/workflow identity or bounded event metadata,
and render through the existing allowlisted event-summary mapping rather than
exposing raw payloads. Operation and workflow results have separate accessible
list labels and the search remains non-mutating.

Changed files:

- `native/SkaiDesktop.Core/Models/WorkflowEventSearchHit.cs`
- `native/SkaiDesktop.Core/Services/WorkflowRunService.cs`
- `native/SkaiDesktop.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including local approval-event search and the full existing Core suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (129 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Activity opened `Search
  activity`; the dialog exposed the search field, `Operation activity search
  results`, separate `Workflow event search results`, and the local-only
  `nothing was changed` status. This run had no seeded workflow event, so no
  live event-result match was claimed.

Privacy/security/licensing: search stays on local journals and does not send
queries or event data to a provider. The UI shows only bounded allowlisted
workflow summaries; no dependency or third-party code was added.

Rollback: remove `WorkflowEventSearchHit`, `SearchEvents`, the workflow result
list, its test, and the two UI contracts; operation search remains available.

Not yet verified: seeded live Windows workflow-event result matching, OS session hosting,
parallel agent artifacts, richer workflow branching, Linux parity, and
packaged acceptance.

## Slice 80 — enforce local-only policy on pinned route selection

Status: `implemented_verified_with_live_surface_boundary`

Selecting a pinned model route from the composer hotswapper now reuses the same
endpoint/model validator used by Settings. When local-only mode is enabled, a
pinned remote endpoint is rejected before model loading, settings mutation, or
connection refresh; the user receives an explicit status explaining the
blocked switch. Local route selection remains available, and no automatic
fallback or cloud route is introduced.

Changed files:

- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including local-only route validation and the full existing Core suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (130 contracts).`
- `git diff --check` — passed.
- Live Windows boundary check — local-only mode was enabled and then restored
  through Host configuration. The composer hotswapper exposed `No pinned models
  yet`, so there was no pinned remote route available to exercise the rejection
  message without adding or changing a provider configuration. No model,
  endpoint, credential, pet setting, or cloud route was changed.

Privacy/security/licensing: the guard prevents an explicit local-only setting
from being bypassed through a previously pinned remote route. It adds no
network request, credential handling, provider fallback, dependency, or
third-party code.

Rollback: remove the selection validation and its audit/test assertion; the
Settings save guard remains, but pinned-route selection would lose the same
local-only boundary.

Not yet verified: the live Windows pinned-route rejection message itself (no
pinned route was available), capability-aware
multi-route selection, durable provider identity across fallback attempts,
Linux parity, and packaged acceptance.

## Slice 49 — serialize reviewed workspace writes

Status: `implemented_verified_core`

`WorkspaceFileEditor.Apply` now gates the complete reviewed-write transaction
by normalized target path. The hash comparison, previous-content capture,
temporary write, replacement, and receipt creation occur under the same
per-path gate, so concurrent Coding Agent/editor writes in this process cannot
both pass the same review hash. A regression harness races two reviewed writes
and verifies exactly one succeeds while the other is rejected as stale.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/WorkspaceFileEditor.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including `workspace edits serialize reviewed writes and reject the losing concurrent hash`.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (108 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this prevents competing in-process reviewed
writes from silently overwriting one another; it does not claim OS-level file
locking against an external editor that ignores the app's transaction gate.
No file contents, credentials, network route, dependency, or third-party code
were added.

Rollback: remove the per-target gate and restore the prior `Apply` body; the
existing expected-hash check and undo receipts remain available.

Not yet verified: cross-process/external-editor race behavior, multi-tab
editing, Linux parity, packaged-release acceptance, and full live editor
accessibility.

## Slice 50 — durable one-shot scheduler state

Status: `implemented_verified_core`

Added a bounded `ScheduledTaskStore` and `ScheduledTask` model for one-shot
tasks. Scheduling persists a future UTC due time and workspace mode; claiming
atomically records `running` before any future executor is allowed to act;
completed and failed outcomes are durable; paused tasks do not claim and can
only resume explicitly. A claimed task is never replayed after restart, which
is the safe default until a reviewed execution adapter is wired into the
Activity & tasks UI.

Changed files:

- `native/SkaiDesktop.Core/Models/ScheduledTask.cs`
- `native/SkaiDesktop.Core/Services/ScheduledTaskStore.cs`
- `native/SkaiDesktop.Core.Tests/ScheduledTaskTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including `scheduled task persistence, claim, no-replay, outcome, pause, and resume`.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (108 contracts).` No UI files changed.
- `git diff --check` — passed.

Privacy/security/licensing: the store persists only the user-authored prompt,
workspace mode, timestamps, bounded status, and bounded outcome. It does not
execute prompts, contact a provider, grant tools, or persist credentials. No
dependency or third-party code was added.

Rollback: remove the scheduler model/store/test registration and leave the
existing Activity & tasks placeholder in place; conversation, settings, and
operation stores are unaffected.

Not yet verified: Activity UI creation/list controls, OS session process host,
reviewed prompt execution, timezone selection, recurring tasks, backoff,
cross-process locking, Linux parity, and packaged-release acceptance.

## Slice 51 — Activity scheduling controls and reviewed prompt surfacing

Status: `implemented_verified_windows`

Activity & tasks now exposes a one-time scheduling action with accessible
prompt/date/time/workspace controls, persisted task listing, and explicit
pause/resume actions. A dispatcher timer claims due tasks exactly once. If a
response is active, the prompt is persisted into the existing bounded queue;
otherwise it is placed in the composer for review and focus is restored there.
No scheduled path bypasses model availability, remote-data consent, or the
normal Send action.

Changed files:

- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including scheduler persistence/no-replay tests.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (111 contracts).`
- `git diff --check` — passed.
- Live Windows native run — passed for opening Activity & tasks, scheduling a
  prompt with date/time/workspace controls, seeing the persisted `scheduled`
  record, and changing it to `paused` then `scheduled` through the accessible
  Pause/Resume controls. The short run did not wait long enough to prove the
  due-timer composer surfacing path.

Privacy/security/licensing: scheduling stores only the authored prompt,
workspace choice, timestamps, status, and bounded outcome. Due work is
surfaced for review or queued behind an active response; it does not silently
send a cloud request, persist credentials, or grant tools. No dependency or
third-party code was added.

Rollback: remove `MainPage.ScheduledTasks.cs`, the Activity scheduling controls,
timer, and audit contracts; the durable scheduler store remains harmless and
can be removed separately without touching chat or operation history.

Not yet verified: live Windows due-surface interaction, OS
session process hosting, automatic provider execution/result capture, timezone
and recurring schedules, backoff, cross-process locking, Linux parity, and
packaged-release acceptance.

## Slice 52 — staged, provenance-aware Markdown skill packages

Status: `implemented_verified_core`

Added `SkillPackageInstaller` as a fail-closed package boundary. A package
must provide a validated manifest and root `SKILL.md`; only bounded UTF-8
Markdown files are accepted, while scripts, binaries, symlinks, reparse
points, traversal paths, oversized files, and oversized packages are rejected.
The installer records the pinned source reference and SHA-256 package digest,
activates through a staging directory, preserves the prior version, and can
roll back to the previous activation. Package content is treated as
instructions only and is never executed by this installer.

Changed files:

- `native/SkaiDesktop.Core/Models/InstalledSkill.cs`
- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `native/SkaiDesktop.Core.Tests/SkillPackageInstallerTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including pinned provenance, digest, atomic replacement, rollback, and executable-content rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after stopping the validation app that held the output DLL.
- `npm run audit:native-ui` — passed: `Native UI audit passed (111 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: no network fetch, script execution, provider call,
credential storage, or tool grant is performed. The source reference is
recorded as provenance, while package content remains local and bounded. No
dependency or third-party code was added.

Rollback: remove the installer and test registration; the existing built-in
skill catalog remains unchanged. Installed package directories are not touched
by unrelated conversation, settings, or project stores.

Not yet verified: GitHub download/ref pinning, staged-install UI, explicit
permission review and enablement, publisher/signature trust, update diff,
cross-process activation locking, Linux parity, and packaged-release
acceptance.

## Slice 53 — installed skills in the reviewed Skills journey

Status: `implemented_verified_core`

The Skills dialog now lists installed local packages separately from built-in
read-only templates. Users can choose a local package directory, review an
explicit non-execution warning, inspect publisher/source/digest/permissions,
load bounded Markdown instructions into the composer for review, and roll back
the selected installed package. The installer remains the only activation
boundary; no package script or binary can be executed through this journey.

Changed files:

- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including package digest, activation, rollback, and executable rejection tests.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (114 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: package content remains local and bounded; only
Markdown is loaded into the composer, where existing review, model, and remote
consent gates still apply. No network fetch, credential persistence, tool
grant, script execution, dependency, or third-party code was added.

Rollback: remove the installed-skill controls and `ReadInstructions` bridge;
the built-in skill catalog and safe Core installer remain independently
reversible.

Not yet verified: live Windows package-picker/install/rollback acceptance,
GitHub transport and cryptographic ref verification, explicit permission
enablement, publisher trust, cross-process activation locking, Linux parity,
and packaged-release acceptance.

## Slice 54 — throttled streaming render and scroll preservation

Status: `implemented_verified_core`

Assistant streaming now accumulates provider chunks under a small response
lock and renders through a 50 ms dispatcher timer instead of rebuilding the
native text surface for every chunk. The stream captures whether the user was
already at the bottom; only that case follows the response, so a user who has
scrolled upward is not repeatedly pulled back down. A final render is performed
and the timer is stopped on both success and provider failure/cancellation.

Changed files:

- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (116 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this changes only local rendering cadence and
scroll behavior; provider payloads, persistence, credentials, and routing are
unchanged. No dependency or third-party code was added.

Rollback: restore the direct per-chunk output update and unconditional bottom
scroll in `StreamChatAsync`; provider transport and conversation persistence
remain unchanged.

Not yet verified: a packaged long-stream performance run, Linux/Skia timing,
screen-reader streaming announcements, and full live keyboard/accessibility
acceptance.

## Slice 55 — explicit MCP tool permission gate

Status: `implemented_verified_core`

The Core layer now treats MCP server manifests as untrusted metadata and
requires a separate local grant matching the server identity, tool name,
connection, and approval identity. Grants are limited to an explicit read or
write capability; read-only test tools can be authorized, while a mutating
tool is denied unless write approval is present. Input schemas are bounded and
must parse as JSON objects before any future executor is allowed to proceed.
This slice deliberately does not claim an MCP transport, server lifecycle, or
UI integration.

Changed files:

- `native/SkaiDesktop.Core/Models/McpTool.cs`
- `native/SkaiDesktop.Core/Services/McpToolPermissionGate.cs`
- `native/SkaiDesktop.Core.Tests/McpToolPermissionGateTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including explicit read-only authorization, denial of an unapproved mutating tool, and invalid-schema rejection.
- `git diff --check` — passed.

Privacy/security/licensing: manifest metadata never creates authority; the
grant must come from the local application. Validation is bounded and local,
with no network calls, credentials, prompts, files, or new dependencies.

Rollback: remove `McpTool.cs`, `McpToolPermissionGate.cs`, its focused tests,
and the test-runner entry. No transport or existing integration behavior is
changed.

Not yet verified: Streamable HTTP or stdio transport, server discovery and
health, cancellation/revocation, UI permission review, real read-only MCP
server invocation, write-tool confirmation, MCP audit records, Linux parity,
and packaged-release acceptance.

## Slice 56 — bounded MCP stdio JSON-RPC client

Status: `implemented_verified_core`

The Core layer now has a supervised stdio MCP JSON-RPC client for the
initialize, initialized notification, tools/list, and tools/call exchange.
Tool manifests are parsed conservatively: only an explicit `readOnlyHint`
produces a read-only descriptor; all other tools are treated as mutating.
Every call passes through the explicit local permission gate. Requests,
arguments, response lines, result text, and request duration are bounded, and
caller cancellation is combined with a 30-second default timeout. The client
does not launch processes, store credentials, or expose a UI connection yet.

Changed files:

- `native/SkaiDesktop.Core/Models/McpTool.cs`
- `native/SkaiDesktop.Core/Services/McpStdioClient.cs`
- `native/SkaiDesktop.Core.Tests/McpStdioClientTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including a scripted MCP exchange covering initialization, conservative tool classification, approved read-only invocation, denied mutating invocation, and emitted JSON-RPC messages.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (116 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the transport is stream-only and makes no
network or credential decisions. Server responses remain untrusted text;
bounded parsing, conservative mutability classification, explicit grants,
request serialization, cancellation, and timeout handling limit exposure. No
dependency or third-party code was added.

Rollback: remove `McpStdioClient`, its focused test, and the result record;
the existing standalone permission gate remains available and no current UI
or provider route changes.

Not yet verified: supervised process creation/cleanup, Streamable HTTP,
protocol negotiation beyond the bounded initialize response, schema-change
re-consent, connection health/revocation UI, audit journal events, a live
external MCP server, Linux/Windows packaged acceptance, and ACP adapters.

## Slice 57 — authenticated release manifest verifier

Status: `implemented_verified_core`

The Core layer now verifies a signed release manifest before a staged artifact
can be considered eligible for update handling. The verifier authenticates a
canonical version/channel/artifact/digest/key-id payload with a supplied RSA
public key, rejects channel mismatches and non-monotonic versions, and checks
the staged artifact SHA-256 before use. This is verification infrastructure;
the existing manual GitHub release screen remains non-installing and no
automatic download, replacement, or rollback behavior was enabled by this
slice.

Changed files:

- `native/SkaiDesktop.Core/Models/ReleaseManifest.cs`
- `native/SkaiDesktop.Core/Services/ReleaseManifestVerifier.cs`
- `native/SkaiDesktop.Core.Tests/ReleaseManifestVerifierTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including valid RSA authentication and artifact hashing plus tampered-manifest, tampered-artifact, and channel-mismatch rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (116 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: verification is local and bounded; it does not
contact a release host, execute a package, access user data, or persist keys.
The artifact is rejected on signature, channel, version, or digest failure.
No dependency or third-party code was added.

Rollback: remove the verifier, manifest model, focused tests, and test-runner
entry. Existing manual update discovery remains unchanged.

Not yet verified: trusted public-key provisioning and rotation, signed feed
fetch integration, package signature/Authenticode/GPG verification, staged
install transaction, restart/rollback orchestration, UI diagnostics, clean
machine acceptance, Windows/Linux packaged evidence, and release publication.

## Slice 58 — MCP connection lifecycle and revocation boundary

Status: `implemented_verified_core`

The MCP Core path now has a process-local connection registry with configured,
healthy, degraded, disconnected, and revoked states. Tool authorization checks
the registered server and connection identity and requires a healthy state;
revocation is terminal for that connection and blocks subsequent stdio calls.
The registry is connected to `McpStdioClient`, so revocation is enforced
outside the UI and before a request is written to the transport. This slice is
deliberately not a durable connection store or a UI lifecycle journey yet.

Changed files:

- `native/SkaiDesktop.Core/Models/McpTool.cs`
- `native/SkaiDesktop.Core/Services/McpConnectionRegistry.cs`
- `native/SkaiDesktop.Core/Services/McpStdioClient.cs`
- `native/SkaiDesktop.Core.Tests/McpConnectionRegistryTests.cs`
- `native/SkaiDesktop.Core.Tests/McpStdioClientTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including configured-to-healthy authorization, revocation denial, terminal reactivation refusal, and client-level revocation before wire output.
- `git diff --check` — passed.

Privacy/security/licensing: connection state and revocation remain process-local
and contain no credentials or prompt content. Exact server/connection matching,
healthy-state checks, terminal revocation, and existing bounded grants prevent
stale UI state from authorizing a new tool call. No dependency or third-party
code was added.

Rollback: remove `McpConnectionRegistry`, the lifecycle model, its tests, and
the optional client registry dependency. The standalone MCP permission gate
and transport remain usable.

Not yet verified: durable connection persistence, process supervision and
cleanup, health probes, disconnect recovery, Streamable HTTP, UI permission
review/revocation, audit journal events, live external MCP server acceptance,
Linux/Windows packaged evidence, and ACP adapters.

## Slice 59 — durable MCP connection metadata

Status: `implemented_verified_core`

MCP connection metadata now persists atomically in the application-data area.
The store is bounded to 64 connections, validates identities, transport and
lifecycle states, rejects duplicates, and stores no endpoint credentials or
environment values. The registry reloads this metadata on startup, persists
configuration and state changes, and retains terminal revocation across a
reload. This slice does not launch or supervise a server process and does not
claim a UI connector journey.

Changed files:

- `native/SkaiDesktop.Core/Services/McpConnectionStore.cs`
- `native/SkaiDesktop.Core/Services/McpConnectionRegistry.cs`
- `native/SkaiDesktop.Core.Tests/McpConnectionRegistryTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including atomic store reload and persisted revocation.
- `git diff --check` — passed.

Privacy/security/licensing: only reviewed connection identity, transport,
manifest digest, state, and bounded error metadata are persisted. Writes use
the existing temp-file-then-rename store primitive; malformed data is rejected
without replacing the file. No credentials, prompts, executable paths, or
third-party code were added.

Rollback: remove `McpConnectionStore` and pass no store to
`McpConnectionRegistry`; the process-local lifecycle and permission gate can
continue without persistence.

Not yet verified: encrypted credential references, process supervision and
cleanup, health probes, disconnect recovery, Streamable HTTP, UI lifecycle and
revocation review, audit journal events, live external MCP server acceptance,
Linux/Windows packaged evidence, and ACP adapters.

## Slice 60 — MCP Streamable HTTP transport

Status: `implemented_verified_core`

The Core layer now supports the JSON-response path of MCP Streamable HTTP.
It validates HTTPS endpoints (or loopback-only HTTP), rejects credentials,
query/fragment data, and redirects, applies the same bounded JSON-RPC request
and response limits as stdio, and routes tool calls through the shared
permission and connection-revocation boundary. Cancellation and a bounded
request timeout are applied to the HTTP operation. SSE response framing,
session negotiation, credential references, and UI configuration are
deliberately not claimed yet.

Changed files:

- `native/SkaiDesktop.Core/Services/McpStreamableHttpClient.cs`
- `native/SkaiDesktop.Core.Tests/McpStreamableHttpClientTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed with a scripted HTTP endpoint covering initialize, tool discovery, approved read-only invocation, revocation denial before request, and redirect refusal.
- `git diff --check` — passed.

Privacy/security/licensing: no credentials are accepted or persisted, public
HTTP is limited to loopback, redirects are refused to prevent host substitution,
and payloads/results are bounded. The injected HTTP client remains under the
caller’s ownership; no dependency or third-party code was added.

Rollback: remove `McpStreamableHttpClient`, its focused test, and the test-runner
entry. Stdio transport, connection storage, and permission policy remain
independent.

Not yet verified: Streamable HTTP SSE/session support, live endpoint health and
reconnect, process supervision, UI lifecycle/revocation review, audit journal
events, live external MCP server acceptance, Linux/Windows packaged evidence,
and ACP adapters.

## Slice 61 — bounded MCP Streamable HTTP SSE responses

Status: `implemented_verified_core`

The Streamable HTTP client now accepts both JSON responses and
`text/event-stream` responses. SSE parsing ignores event metadata, bounds each
line, skips empty and `[DONE]` frames, parses the first JSON-RPC data frame,
and applies the same response-id, error, output, timeout, cancellation, and
connection-revocation checks as the JSON path. The client still does not claim
MCP session negotiation or long-lived event subscriptions.

Changed files:

- `native/SkaiDesktop.Core/Services/McpStreamableHttpClient.cs`
- `native/SkaiDesktop.Core.Tests/McpStreamableHttpClientTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed with a scripted endpoint returning an SSE `tools/call` result, plus revocation and redirect negative cases.
- `git diff --check` — passed.

Privacy/security/licensing: SSE data is treated as untrusted bounded text;
event metadata does not gain authority, and no credentials or persistent
subscriptions are created. No dependency or third-party code was added.

Rollback: restore JSON-only response handling and the SSE assertion in the
focused test; stdio, JSON HTTP, permission, and persistence paths remain
independent.

Not yet verified: MCP session IDs and protocol negotiation, long-lived
notifications/resources, health/reconnect, process supervision, UI lifecycle
and revocation review, audit journal events, live external MCP server
acceptance, Linux/Windows packaged evidence, and ACP adapters.

## Slice 62 — MCP Streamable HTTP session negotiation

Status: `implemented_verified_core`

The Streamable HTTP client now captures a bounded `Mcp-Session-Id` from the
initialize response and sends it, together with the negotiated
`MCP-Protocol-Version`, on subsequent requests. Invalid or oversized session
identifiers fail closed. The session is scoped to the client instance and is
not persisted or treated as a credential; long-lived notification/resource
subscriptions remain outside this slice.

Changed files:

- `native/SkaiDesktop.Core/Services/McpStreamableHttpClient.cs`
- `native/SkaiDesktop.Core.Tests/McpStreamableHttpClientTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including scripted initialize/session response, subsequent session/protocol headers, SSE tool invocation, revocation denial, and redirect refusal.
- `git diff --check` — passed.

Privacy/security/licensing: session identifiers remain process-local, bounded,
and never enter persistence, prompts, logs, or credentials. No endpoint
redirects or secret-bearing URLs are accepted. No dependency or third-party
code was added.

Rollback: remove the session fields/header handling and the focused header
assertion; JSON/SSE parsing and permission/revocation checks remain intact.

Not yet verified: long-lived notifications/resources, session expiry and
reconnect, health probes, process supervision, UI lifecycle/revocation review,
audit journal events, live external MCP server acceptance, Linux/Windows
packaged evidence, and ACP adapters.

## Slice 63 — deterministic conversation context-window planner

Status: `implemented_verified_core`

Core now includes a bounded context planner used by the OpenAI-compatible
provider when the serialized request exceeds its existing 8 MB ceiling. It
preserves system messages that fit, selects the newest turns that fit the
requested character budget, inserts an explicit untrusted omission marker when
older history is dropped, and never mutates the source history or invents a
summary. Normal-sized requests are unchanged. This is a deterministic
compaction boundary, not semantic model summarization, persistent memory, or a
claim of 1M+ token support; the budget remains an internal provider safety
limit.

Changed files:

- `native/SkaiDesktop.Core/Services/ConversationContextPlanner.cs`
- `native/SkaiDesktop.Core/Services/OpenAiCompatibleProvider.cs`
- `native/SkaiDesktop.Core.Tests/ConversationContextPlannerTests.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including newest-turn/system preservation, omitted-history marking, source immutability, empty-history handling, oversized multi-turn provider compaction before network access, and preservation of single-message oversize rejection.
- `git diff --check` — passed.

Privacy/security/licensing: planning is local and deterministic; omitted text
is not sent, persisted, or summarized by another service. The marker instructs
the model not to assume missing details. No dependency or third-party code was
added.

Rollback: remove the planner call and focused overflow test, then restore the
provider’s direct 8 MB rejection. Existing normal-sized provider message
assembly remains unchanged.

Not yet verified: user-visible budget controls, exact provider-token accounting,
semantic summarization with consent,
persistent memory, restart continuity, provider-specific context limits,
1M+ token inference, Linux parity, and packaged acceptance.

## Slice 64 — explicit local memory store

Status: `implemented_verified_core`

Core now provides a bounded, atomic memory store for user-approved facts or
project notes. Every entry requires an approval identity and records scope,
source, tags, enabled state, and timestamps. Search is local and bounded;
entries can be disabled, edited, or explicitly deleted, and changes survive
reload. The store does not automatically extract memories from prompts or
responses, inject entries into model requests, sync across devices, or store
credentials.

Changed files:

- `native/SkaiDesktop.Core/Models/MemoryEntry.cs`
- `native/SkaiDesktop.Core/Services/MemoryStore.cs`
- `native/SkaiDesktop.Core.Tests/MemoryStoreTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including approved project search, enable/disable retrieval, edit/reload, explicit durable deletion, and invalid-content rejection.
- `git diff --check` — passed.

Privacy/security/licensing: memory is local-only, explicitly approved, bounded,
atomically persisted, and never sent to a provider by this slice. Multiline
notes are allowed, but other control characters and oversized entries are
rejected. No dependency or third-party code was added.

Rollback: remove `MemoryEntry`, `MemoryStore`, and its focused test/runner
entry. Existing conversations and settings remain independent; any stored
memory file is left for explicit user handling rather than silently deleted.

Not yet verified: native memory management UI, review before injection,
project/global retention controls, encrypted storage, backup/export, automatic
memory proposals, model-context injection, cross-device sync, Linux parity,
and packaged acceptance.

## Slice 65 — explicit native memory management journey

Status: `implemented_verified_windows`

Settings → Chats & history now exposes a Memory manager backed by the tested
local store. The surface searches all saved entries, displays scope/source/
approval metadata, creates explicitly approved notes, edits content and tags,
enables or disables retrieval eligibility, and deletes selected entries. The
screen clearly states that memory is local, not automatically extracted, and
not injected into model requests by this slice. It does not touch companion
settings or code.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the memory store persistence and mutation suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (120 contracts).`
- `git diff --check` — passed.
- Live Windows UI inspection — passed by launching the exact `net10.0-desktop`
  executable and opening Navigation → More → Connection & settings → Chats &
  history → Manage memory. The dialog exposed the local-only disclosure,
  Search saved memory, Saved memory entries, scope/source/content/tags fields,
  enable checkbox, and New/Save/Disable/Delete controls, with `0 saved entries
  · local only`. No memory entry was created or deleted during inspection.

Privacy/security/licensing: the UI writes only when the user clicks Save,
generates a local approval ID for that explicit action, and uses the existing
atomic store. No memory content is sent to a provider, logged, or synced by
this journey. No dependency or third-party code was added.

Rollback: remove the Memory manager section, its field, and the four static UI
contracts; the Core memory file and store remain independent for explicit
future recovery or removal.

Not yet verified: keyboard/screen-reader CRUD journey, memory export or backup,
encrypted storage, retention controls, review-before-injection,
automatic memory proposals, model-context injection, cross-device sync, Linux
parity, and packaged acceptance.

## Slice 66 — explicit local memory backup export

Status: `implemented_verified_core`

The Memory manager now offers an explicit JSON backup export. The export is
generated from the validated local store, includes enabled and disabled entries
with approval/provenance metadata, uses a versioned format marker, and writes
only after the user chooses a destination in the native save picker. There is
no automatic import, provider upload, or model injection.

Changed files:

- `native/SkaiDesktop.Core/Services/MemoryStore.cs`
- `native/SkaiDesktop.Core.Tests/MemoryStoreTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the versioned backup marker and disabled-entry preservation assertion.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after closing the development app process that held its output DLL.
- `npm run audit:native-ui` — passed: `Native UI audit passed (121 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: backup content stays local until the user chooses
the destination and is written through the native file picker. The export does
not send memory to a provider, add credentials, or alter the source store. No
dependency or third-party code was added.

Rollback: remove `MemoryStore.ExportJson`, the export button/picker, its test,
and its static UI contract; saved memory management remains available.

Not yet verified: live Windows export-picker acceptance, memory restore/import,
encrypted storage, retention controls, review-before-injection, automatic
memory proposals, model-context injection, cross-device sync, Linux parity,
and packaged acceptance.

## Slice 70 — safe local memory backup restore

Status: `implemented_verified_core`

The Memory manager now supports importing its versioned JSON backup format
through a bounded native file picker. Import first creates a validated plan,
reports new versus duplicate identities, and requires explicit confirmation;
existing entries are never overwritten. Imported entries retain their original
approval, provenance, tags, timestamps, and enabled state.

Changed files:

- `native/SkaiDesktop.Core/Services/MemoryStore.cs`
- `native/SkaiDesktop.Core.Tests/MemoryStoreTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including import planning, merge, and duplicate-identity protection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (124 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed by launching the exact
  `net10.0-desktop` executable, opening Settings → Chats & history → Manage
  memory, and observing the local-only disclosure plus Import saved memory
  backup and Export saved memory backup controls. The empty state reported
  `0 saved entries · local only`; no memory data was changed.

Privacy/security/licensing: parsing is local and bounded to 12 MB; malformed,
unsupported, or over-capacity backups are rejected before persistence. Import
does not contact a provider or silently replace user data. No dependency or
third-party code was added.

Rollback: remove `MemoryStore.PlanImport`/`ApplyImport`, the import picker and
confirmation flow, its tests, and the static UI contract; existing memory
editing and export remain available.

Not yet verified: live Windows picker selection and confirmation acceptance,
encrypted storage, retention controls, review-before-injection, automatic
memory proposals, model-context injection, cross-device sync, Linux parity,
and packaged acceptance.

## Slice 71 — locale-independent conversation search

Status: `implemented_verified_core`

Conversation search now uses ordinal case-insensitive matching for message,
tag, attachment, and source text. This keeps search behavior deterministic
across machine cultures and adds a regression assertion for uppercase
attachment queries.

Changed files:

- `native/SkaiDesktop.Core/Services/ConversationSearchService.cs`
- `native/SkaiDesktop.Core.Tests/ConversationSearchTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the culture-independent uppercase attachment query.
- `git diff --check` — passed in commit `f10f09c`.

Privacy/security/licensing: matching remains local-only, adds no data flow or
dependency, and does not change stored conversation content.

Rollback: restore the prior culture-sensitive comparison and remove the
uppercase-search regression assertion.

Not yet verified: full cross-platform locale matrix and live accessibility
acceptance for every search result state.

## Slice 72 — bounded recurring scheduled tasks

Status: `implemented_verified_core`

The durable scheduler now supports one-hour, two-hour, daily, and weekly
recurrence options while preserving one-shot scheduling as the default. A
successful surfaced run is durably rescheduled with its run count incremented;
failed runs remain failed for review and are not automatically retried. Existing
pause/resume and no-replay behavior remain explicit and durable.

Changed files:

- `native/SkaiDesktop.Core/Models/ScheduledTask.cs`
- `native/SkaiDesktop.Core/Services/ScheduledTaskStore.cs`
- `native/SkaiDesktop.Core.Tests/ScheduledTaskTests.cs`
- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including recurring claim, durable rescheduling, run-count, and early-replay protection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (125 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: recurrence stores only bounded local task metadata;
it does not execute a model request automatically. Each surfaced prompt still
passes through the existing review, availability, consent, and send controls.
No dependency or third-party code was added.

Rollback: remove `RepeatEveryMinutes`/`RunCount`, the recurrence branch in
completion, the recurrence picker/display, its tests, and the static UI
contract; one-shot task behavior remains available.

Not yet verified: live Windows recurring-task creation and pause/resume,
timezone/locale matrix, backoff policies, autonomous background execution,
parallel agents, Linux parity, and packaged acceptance.

## Slice 73 — explicit installed-skill enablement

Status: `implemented_verified_with_live_surface_boundary`

Installed local skills are now disabled by default and must be explicitly
enabled before their Markdown instructions can be loaded into the composer.
Enablement is stored atomically in the active manifest, displayed with the
publisher/source/digest details, and can be revoked without uninstalling the
package. Updates remain disabled until the new package is reviewed; rollback
restores the prior package and its prior enablement state.

Changed files:

- `native/SkaiDesktop.Core/Models/InstalledSkill.cs`
- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `native/SkaiDesktop.Core.Tests/SkillPackageInstallerTests.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including disabled-by-default, durable enablement, update, rollback, and executable-rejection checks.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (126 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: the Skills dialog exposed the
  read-only built-in catalog, installed-skills list, disabled `Use skill`, local
  package install, explicit enable/disable, rollback, and Done/Close controls;
  no package was installed, enabled, or changed.

Privacy/security/licensing: skill content remains local Markdown and is never
executed. The explicit grant state prevents a newly installed or updated
package from entering a prompt until the user reviews and enables it. No
dependency or third-party code was added.

Rollback: remove `Enabled`, `SetEnabled`, the UI control, test assertions, and
static UI contract; the existing package validation and rollback paths remain.

Not yet verified: GitHub transport/ref verification, publisher trust, live
Windows permission-control execution, cross-platform packaging, and update
diff review.

## Slice 74 — skill package update preview

Status: `implemented_verified_with_live_surface_boundary`

Local skill installation now performs a read-only package preview before the
explicit install confirmation. The preview validates the Markdown-only package,
shows version and publisher, computes the package digest, lists requested
permissions, and identifies whether the package replaces an existing version.
The install confirmation states that nothing executes automatically and that
the package remains disabled until explicitly enabled.

Changed files:

- `native/SkaiDesktop.Core/Models/InstalledSkill.cs`
- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `native/SkaiDesktop.Core.Tests/SkillPackageInstallerTests.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including pre-install digest/permission preview and update detection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: the Skills dialog exposed the
  installed-skills area and local package-install entry point; no package was
  selected or installed, so the preview confirmation was not altered.

Privacy/security/licensing: preview reads only the user-selected local folder,
does not activate or execute it, and does not perform network access. The
digest and permissions are shown before confirmation. No dependency or
third-party code was added.

Rollback: remove `SkillPackagePreview`, the preview method/dialog content, its
tests, and the static UI contract; existing package validation and explicit
enablement remain available.

Not yet verified: GitHub transport/ref verification, publisher trust, live
Windows preview confirmation, cross-platform packaging, and update diff review.

## Slice 75 — pinned GitHub skill-reference validation

Status: `implemented_verified_core`

Skill installation now validates source provenance before reading or activating
a package. Local-folder references remain supported, while GitHub references
must use an HTTPS `github.com/owner/repository@<hex commit ref>` form with no
credentials, query, fragment, or floating branch/tag. This is a format guard,
not a claim that the remote commit exists; transport and remote ref resolution
remain separate future work.

Changed files:

- `native/SkaiDesktop.Core/Models/SkillSourceReference.cs`
- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `native/SkaiDesktop.Core.Tests/SkillPackageInstallerTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including rejection of a floating `@main` reference and preservation of pinned test references.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: validation is local-only and happens before package
file processing; it adds no network request, credential handling, dependency,
or remote trust decision.

Rollback: remove `SkillSourceReference`, the installer validation call, and the
invalid-reference test; existing local package safety checks remain.

Not yet verified: GitHub download/authentication, remote commit existence,
publisher trust, live Windows source-reference controls, cross-platform
packaging, and update diff review.

## Slice 67 — explicit local activity evidence export

Status: `implemented_verified_core`

Activity & tasks now offers an explicit JSON export of the durable operation
journal. The export is validated before serialization, carries a versioned
format marker, and includes run/event provenance so users can preserve local
evidence for review without changing operation state or replaying any side
effect.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/OperationJournal.cs`
- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/SkaiDesktop.Core.Tests/OperationJournalTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the versioned export and reconciliation-event assertion.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (122 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the evidence bundle is written only after an
explicit native save-picker choice, remains local, and is clearly reported as
not sent to a provider. It can contain the user's operation instructions and
must be treated as sensitive when shared. No dependency or third-party code
was added.

Rollback: remove the journal/service export methods, Activity button/picker,
test assertion, and static UI contract; the existing Activity surface and
durable journal remain available.

Not yet verified: live Windows export-picker acceptance, searchable/exported
operation evidence beyond the full journal, OS session hosting, parallel agent
artifacts, Linux parity, and packaged acceptance.

## Slice 68 — bounded local activity search

Status: `implemented_verified_core`

Activity & tasks now includes a non-mutating search dialog for durable
operations. Queries are bounded and match local instruction, status, model,
provider, or workspace provenance; results are capped and ordered by recent
activity. The surface explicitly reports that searching does not change an
operation.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/OperationJournal.cs`
- `native/SkaiDesktop.Core/Services/OperationService.cs`
- `native/SkaiDesktop.Core.Tests/OperationJournalTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including local provenance matching and no-result behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (123 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: search stays on the local operation journal and
does not send queries or instructions to a provider. Query length and control
characters are rejected before processing, and no dependency was added.

Rollback: remove the journal/service search methods, Activity search dialog,
test assertion, and static UI contract; activity export and operation history
remain available.

Not yet verified: live Windows search accessibility acceptance, searchable
workflow-event evidence, OS session hosting, parallel agent artifacts, Linux
parity, and packaged acceptance.

## Slice 69 — centralized rebrand-safe product identity

Status: `implemented_verified_core`

User-facing native labels now read from the centralized `ProductIdentity` and
build `ProductDisplayName` settings instead of repeating the current product
name throughout sidebar, messages, update, archive, backup, terminal, GitHub,
and MCP client surfaces. Existing technical storage paths, namespaces,
credential entropy, package identifiers, and protocol formats remain unchanged
so a future rebrand does not migrate or invalidate user data.

Changed files:

- `native/SkaiDesktop.Core/Models/ProductIdentity.cs`
- `native/Directory.Build.props`
- `native/ServerRock.Native/ServerRock.Native.csproj`
- `native/ServerRock.Native/SidebarView.xaml`
- `native/ServerRock.Native/SidebarView.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.Messages.cs`
- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.ImportExport.cs`
- `native/ServerRock.Native/MainPage.GitHub.cs`
- `native/SkaiDesktop.Core/Infrastructure/WorkspaceTerminalSession.cs`
- `native/SkaiDesktop.Core/Services/McpStdioClient.cs`
- `native/SkaiDesktop.Core/Services/McpStreamableHttpClient.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (123 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this is a local label/configuration change only;
it adds no network traffic, credential changes, data migration, or dependency.

Rollback: restore the prior display literals and application title property;
technical identifiers remain unaffected either way.

Not yet verified: packaged installer title/name propagation, Linux display
acceptance, and the final selected rebrand name.

## Slice 48 — session-only BYOK for compatible providers

Status: `implemented_verified_core`

The native connection dialog now accepts one optional bearer token for any
OpenAI-compatible endpoint. The same session-only value is used for model
discovery, chat streaming, prompt rewriting, and local benchmark requests;
the value is cleared or replaced when the user saves connection settings and
is never added to `AppSettings` or ordinary persistence. The OpenRouter
shortcut continues to prefill its endpoint and free-model route, but the
credential path is no longer provider-specific.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (108 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the token remains in process memory only, is
passed as a bearer header by the existing bounded provider transport, and is
not written to settings, logs, prompts, diagnostics, or URLs. No dependency
or third-party code was added.

Rollback: restore the OpenRouter-only key field and `GetProviderApiKey` branch;
the existing local endpoint flow remains unchanged.

Not yet verified: live hosted-provider discovery/chat with a real user key,
OS keyring-backed hosted credentials, provider-specific scopes/quotas, Linux
parity, and packaged-release acceptance.

## Slice 46 — block sends for definitively unavailable discovered models

Status: `implemented_verified_windows`

The native connection refresh now retains the provider-advertised model
inventory, and the send path performs a local selection check before smart
rewriting, web search, consent prompts, or provider streaming. When the
inventory is non-empty and excludes the selected model, Send leaves the draft
untouched and announces that the user must choose a model in Model Library.
If discovery fails and no inventory is known, the guard stays permissive so the
user can retry the connection instead of receiving a false incompatibility
claim. No automatic fallback or cloud route is introduced.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Connection.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (106 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: after temporarily selecting
  `missing-model`, sending `guard must block this request` kept the draft in
  the composer and announced `Selected model missing-model is unavailable ·
  choose a model in Model Library before sending`; no turn was created. The
  original model was restored afterward.

Privacy/security/licensing: the check uses only model IDs already returned by
the configured endpoint. It prevents an unintended request and does not send
prompt, attachment, project, credential, or web-search data.

Rollback: remove `_advertisedModels`, the connection inventory assignment, the
pre-send selection guard, and the two audit contracts; the existing connection
status and provider failure recovery remain available.

Not yet verified: proactive checks for richer advertised capability mismatches,
multi-route provider selection, durable provider identity across fallback
attempts, live Git dialog acceptance, remote fetch/reconciliation before push,
worktree isolation, Linux parity, and packaged-release acceptance.

## Slice 47 — refresh remote Git state before push

Status: `implemented_verified_core`

`PushAsync` now performs a bounded `git fetch --prune --no-tags origin` before
the final ahead/behind decision, then re-reads repository status. A fetch
failure leaves local commits untouched and reports that remote state could not
be refreshed. If the refreshed branch is behind, the existing no-force refusal
still stops the push and directs the user to review the remote changes. The
push path remains explicit and never uses force.

Changed files:

- `native/SkaiDesktop.Core/Services/GitHubWorkspaceService.cs`
- `native/SkaiDesktop.Core.Tests/GitHubTests.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the ahead/behind parser regression used by push preflight.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (106 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: fetch uses the already-authorized GitHub access
token through the existing process environment credential path, never the
command line or remote URL. It sends only Git protocol metadata and does not
send prompts, workspace file contents, or conversation data.

Rollback: remove the fetch and second status read in `PushAsync`; the existing
behind-count refusal and non-force push remain as a weaker safety boundary.

Not yet verified: live Git dialog push with a user-selected repository and
credential, remote-update race against an actual GitHub branch, worktree
isolation, proactive model capability mismatch checks, multi-route provider
selection, durable provider identity across fallback attempts, Linux parity,
and packaged-release acceptance.

## Slice 45 — isolate selected Git commits from the user index

Status: `implemented_verified_windows_core`

Selected Git commits now use a temporary `GIT_INDEX_FILE` initialized from
`HEAD`. The reviewed paths are staged and committed through that isolated index,
so unrelated changes the user had already staged remain untouched and cannot
accidentally enter the selected commit. The temporary index is removed after
success or failure; the user's working tree and real index are never reset.

Changed files:

- `native/SkaiDesktop.Core/Services/GitHubWorkspaceService.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/SkaiDesktop.Core.Tests/TestSupport.cs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the regression that pre-stages `pre-staged-user-work.md`, commits only the selected safe review changes, and verifies the unrelated file remains staged and absent from `HEAD`.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (104 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the isolated index prevents accidental disclosure
or publication of unrelated user changes. It uses local Git state only, never
places credentials in arguments or remotes, and adds no dependency.

Rollback: restore the prior `CommitSelectedCoreAsync` staging/commit calls and
remove the isolated-index fixture assertion; the existing path and secret
checks remain available.

Not yet verified: live Git dialog acceptance with a user-selected repository,
remote fetch/reconciliation before push, worktree isolation, proactive model
compatibility detection, multi-route provider selection, durable provider
identity across fallback attempts, Linux parity, and packaged-release
acceptance.

## Slice 43 — inline settings validation and draft cancellation

Status: `implemented_verified_windows`

Settings now validates the endpoint, model ID, and local-only policy through a
pure Core validator before any settings snapshot is committed. Invalid Save
attempts cancel the dialog close, show an assertive accessible inline message,
return to the General section, and focus the relevant draft field. Cancel still
discards the entire draft, so malformed or incomplete edits cannot replace the
last working connection.

Changed files:

- `native/SkaiDesktop.Core/Services/ConnectionSettingsValidation.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including valid local, malformed endpoint, missing model, and local-only remote rejection cases.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (101 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Settings opened through the
  command palette; changing the endpoint to `not an endpoint` and choosing Save
  kept Settings open and announced `Enter a valid http:// or https:// model
  endpoint.`; choosing Cancel returned to chat with the original endpoint/model
  still selected.

Privacy/security/licensing: validation is local-only and uses no provider call,
credential, prompt, or workspace data. It reduces accidental endpoint changes
and preserves the user's last valid connection on cancellation.

Rollback: remove `ConnectionSettingsValidator`, the primary-click validation
handler, and its tests; the existing post-dialog checks remain as a weaker
fallback.

Not yet verified: broader settings validation matrix, platform-specific
diagnostics, proactive model compatibility detection, multi-route provider
selection, durable provider identity across fallback attempts, Linux parity, and
packaged-release acceptance.

## Slice 44 — preserve composer drafts across workspaces

Status: `implemented_verified_windows`

The Chat and Coding Agent workspaces now keep independent in-memory composer
drafts while the user switches between them. Text changes update the active
workspace draft, mode switching restores the destination draft, and starting a
new chat clears only the active workspace draft. Drafts are bounded to 100,000
characters and are intentionally not persisted as conversation history or
settings.

Changed files:

- `native/SkaiDesktop.Core/Services/WorkspaceDraftStore.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.SlashCommands.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including isolated Chat/Coding Agent draft state.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (104 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Chat draft
  `chat draft survives workspace switch` and Coding Agent draft
  `coding draft survives workspace switch` were entered separately; switching
  to Coding Agent and back restored the exact Chat draft and retained the same
  selected model.

Privacy/security/licensing: drafts remain in process memory only, never enter
conversation persistence or provider requests until the user sends them, and
no dependency or third-party code was added.

Rollback: remove `WorkspaceDraftStore`, the draft field/restore calls, and the
three UI audit contracts; existing mode switching continues with its prior
composer behavior.

Not yet verified: draft persistence across app restart, attachment-draft
continuity, typed navigation history, persistent tabs/layout, proactive model
compatibility detection, multi-route provider selection, durable provider
identity across fallback attempts, Linux parity, and packaged-release
acceptance.

## Slice 42 — preserve unavailable selected models until explicit choice

Status: `implemented_verified_windows`

The connection refresh path now evaluates the saved model against the provider's
advertised model list without silently replacing it or persisting the first
available model. When the saved model is unavailable, the runtime status tells
the user to choose a model in Model Library while the selected model identity
remains visible. Model changes continue to happen only through an explicit user
selection path.

Changed files:

- `native/SkaiDesktop.Core/Services/ModelSelectionPolicy.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.Connection.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (98 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Settings temporarily saved
  `missing-model`; runtime status announced that the selected model was
  unavailable and directed the user to Model Library, while ModelButton still
  exposed `missing-model`. The original model was restored afterward.

Privacy/security/licensing: preserving the explicit selection prevents an
unexpected local model change and avoids silently changing provider behavior.
The policy uses only the locally advertised model list and adds no credentials,
network route, prompt data, or dependency.

Rollback: restore the prior unavailable-model branch in
`MainPage.Connection.cs` and remove `ModelSelectionPolicy`; the existing
explicit Model Library selection path remains available.

Not yet verified: proactive compatibility detection before a request,
multi-route provider selection, durable provider identity across fallback
attempts, Linux parity, and packaged-release acceptance.

## Slice 139 — live Back/Forward workspace navigation

Status: `verified_windows_disposable_navigation`

The existing bounded `NavigationHistory` implementation now has live native
acceptance evidence. In a disposable settings profile, the Windows app moved
from Chat to Coding Agent, Back returned to Chat and enabled Forward, and
Forward returned to Coding Agent while the accessibility tree exposed the
correct route names and enabled/disabled states. The same disposable settings
file persisted the route history as `ChatBot` and `Coding Agent`.

Changed files:

- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — the existing bounded branch-aware navigation and corrupt-route recovery tests remain the Core contract.
- Live Windows accessibility inspection — passed: Chat and Coding Agent were
  exposed as named workspace controls; Back was enabled after moving to Coding
  Agent; Back returned to Chat and enabled Forward; Forward returned to
  Coding Agent and then disabled itself.
- Disposable `settings.json` inspection — passed: route history persisted the
  two visited routes; the real user profile was not opened or modified.
- `npm run audit:native-ui` and `git diff --check` — passed.

Privacy/security/licensing: the check used only route names and a disposable
local settings file. No prompt, provider request, credentials, project data,
or pet state was changed.

Rollback: revert this documentation entry and the capability-matrix wording;
the runtime implementation and existing Core history contract remain intact.

Not yet verified: persistent tabs/layout geometry, inspector behavior, typed
dialog routes, live focus/resize acceptance, Linux parity, and packaged-release
acceptance.

## Slice 138 — approved-memory injection after app restart

Status: `verified_windows_disposable_memory_restart`

The approved-memory injection path now has a bounded live restart check. A
disposable profile supplied through `SRAI_SETTINGS_PATH`,
`SRAI_CONVERSATIONS_PATH`, and `SRAI_MEMORY_PATH` was launched with the local
Ollama model `wtwk-qwen35-4b:latest` and one synthetic approved global note.
The first session returned the exact marker `MEMORY_RESTART_OK`; the app was
closed and relaunched against the same files; the second fresh request
returned `MEMORY_RESTART_OK_2`. The persisted settings still contained the
local endpoint, selected model, `memoryInjectionEnabled: true`, and
`requireLocalModelEndpoints: true`, while the conversation file contained
both completed turns and the memory file retained the approved note.

Changed files:

- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- Live Windows session 1 — passed: the disposable profile injected the
  approved synthetic note and the healthy local model returned exactly
  `MEMORY_RESTART_OK`.
- Live Windows session 2 after closing and relaunching the app — passed: the
  same profile injected the retained note and returned exactly
  `MEMORY_RESTART_OK_2`.
- Terminal inspection — passed: `settings.json` retained the memory-enabled
  local-only configuration; `memory.json` retained the approved note; and
  `conversations.json` contained both request/response pairs.
- The disposable files were removed after verification; the real profile was
  not opened or modified.

Privacy/security/licensing: the test used synthetic content and a disposable
local profile only. The memory note remained marked approved but was still
used as untrusted reference context; no credentials, remote endpoint, or
user-profile data were involved.

Rollback: revert this documentation entry and the corresponding capability
matrix wording. The runtime behavior is unchanged by this evidence slice.

Not yet verified: durable semantic conversation-context continuity and profile
migration beyond approved-memory injection, semantic compaction under broad
real-world histories, 1M+ token context, Linux parity, and packaged-release
acceptance.

## Slice 140 — live follow-up queue dispatch during streaming

Status: `verified_windows_disposable_queue_dispatch`

The bounded follow-up queue now has live native evidence for its primary user
journey. Against a disposable local profile and healthy Ollama model, a long
first request was sent; a second prompt was entered while the first response
was still streaming; the UI announced `Queued message 1 of 8` and exposed the
accessible `Manage queued messages` control. After the first response
finished, the queued prompt dispatched automatically and the fresh local
response returned exactly `QUEUE_FOLLOWUP_OK`.

Changed files:

- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- Live Windows accessibility inspection — passed: the in-flight composer
  accepted the second prompt without replacing the active response, exposed
  the queued-count status, and retained the stop control for the active
  response.
- Live Windows response inspection — passed: the queued prompt appeared after
  the first turn and its assistant response was exactly `QUEUE_FOLLOWUP_OK`;
  the queue status and stop control cleared afterward.
- Disposable `conversations.json` inspection — passed: both prompts and the
  completed queued response were persisted; the disposable settings and
  conversation files were removed afterward.
- Existing `QueueService` Core tests, `npm run audit:native-ui`, and
  `git diff --check` remain passing.

Privacy/security/licensing: the journey used synthetic prompts, a local-only
Ollama endpoint, and a disposable profile. Queueing did not add a provider,
credential, remote route, dependency, or pet-state mutation.

Rollback: revert this documentation entry and capability-matrix wording; the
existing bounded queue implementation and persistence contract remain intact.

Not yet verified: live queue removal/reorder/clear accessibility, richer
cross-device queue synchronization, durable semantic conversation-context
continuity, 1M+ token context, Linux parity, and packaged-release acceptance.

## Slice 141 — repair and verify queue management controls

Status: `implemented_verified_windows_disposable_queue_management`

The live acceptance of Slice 140 exposed that `ComposerInputView` raised
`ManageQueueRequested` without `MainPage` subscribing to it. The repaired
wiring now routes the accessible Queue control into the existing bounded
management dialog. The queue store also honors `SRAI_QUEUE_PATH`, allowing
disposable acceptance without reading or writing the real user queue.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/ChatMessageQueueStore.cs`
- `native/SkaiDesktop.Core.Tests/ChatMessageQueueStoreTests.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/ComposerInputView.xaml.cs`
- `scripts/audit-native-ui.cjs`
- `docs/ARCHITECTURE.md`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including the explicit disposable `SRAI_QUEUE_PATH` store override.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: 183 contracts, including queue event
  wiring and the management control’s accessible label.
- Live Windows disposable profile — passed: the Queue control opened the
  `Queued messages` dialog; moving the first message down changed the visible
  order to `Queued two`, `Queued one`, `Queued three`; removing the middle
  message left two entries; Clear queue closed the dialog, announced
  `Queued messages cleared. The active response was not changed.`, and wrote
  `[]` to the disposable queue file.
- `npm run audit:future-features` and `git diff --check` — passed.

Privacy/security/licensing: the new environment override is opt-in and does
not change the normal `%APPDATA%` queue location. The live journey used only
synthetic prompts and local disposable files; no credentials, remote route,
project data, or pet state was changed.

Rollback: remove the event subscription, the explicit queue-path override and
its test, the audit contracts, and the documentation updates. The existing
queue implementation would remain available but its management activation
would regress, so rollback is only appropriate with an owner decision.

Not yet verified: richer cross-device queue synchronization, durable semantic
conversation-context continuity, 1M+ token context, Linux parity, and
packaged-release acceptance.

## Slice 142 — live dynamic variables in reviewed skill prompts

Status: `implemented_verified_windows_dynamic_prompt_variables`

The reviewed built-in skill path now visibly exercises the supported dynamic
variables. The Release readiness skill prompt includes the explicit opt-in
`{{USER_NAME}}` and automatic `{{DATE}}` variables. With the disposable local
settings profile naming the reviewer `Ada`, choosing `/skills`, selecting
`Review release readiness`, and activating `Use skill` populated the composer
with `Prepare this release-readiness review for Ada on 2026-11-12...`; no
placeholder remained unresolved and the prompt was left reviewable rather than
sent automatically.

Changed files:

- `native/SkaiDesktop.Core/Services/SkillCatalogService.cs`
- `native/SkaiDesktop.Core.Tests/CatalogTests.cs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj` — passed, including deterministic expansion of the Release readiness skill prompt with an explicit name and fixed date.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- Live Windows disposable profile — passed: the Skills dialog exposed the
  built-in skill; selecting and activating it populated the focused composer
  with `Ada` and `2026-11-12`, while the dialog announced that the prompt was
  loaded for review.
- `npm run audit:native-ui` — passed: 183 contracts, including skill prompt
  expansion and explicit user-name setting labels.
- `npm run audit:future-features` and `git diff --check` — passed; the dynamic
  variables item is now checked with this evidence.

Privacy/security/licensing: the name is explicitly entered and stored locally;
it is never read from the Windows account automatically. Expansion happens
before review, no provider request is made by Use skill, and no project files,
credentials, pet state, or third-party dependency are involved.

Rollback: remove the two placeholders from the built-in prompt, its catalog
assertion, the future-feature check, and this evidence entry. The generic
expander remains available for reviewed local skill packages.

Not yet verified: dynamic-variable use in installed third-party skills,
cross-platform and packaged acceptance, and the remaining marketplace trust
and remote-install gaps.

## Slice 143 — live persistent-memory management CRUD

Status: `implemented_verified_windows_memory_crud`

The persistent-memory management backlog item is now verified through the live
native Windows UI. A disposable profile seeded one synthetic approved note;
the Memory manager opened it, displayed its provenance and enabled state,
edited its content and tags, saved the change, reselected the updated entry,
and deleted it. The manager ended at `0 saved entries · local only`, and the
disposable memory file contained `[]` after the journey.

Changed files:

- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Live Windows disposable profile — passed: Settings → Chats & history →
  Manage memory exposed the seeded note and accessible view/edit/delete
  controls; edit/save/reselect/delete completed successfully without a model
  request.
- Disposable `SRAI_MEMORY_PATH` — ended as `[]` after deletion; no normal
  profile data was used.
- `npm run audit:future-features` — passed with the persistent-memory item
  checked.
- `git diff --check` — passed.

Privacy/security/licensing: the journey used synthetic local data only. The
memory manager showed the local-only boundary and explicit approval metadata;
no credentials, remote endpoint, project files, or pet state were changed.

Rollback: revert the three documentation changes to return the backlog item
and capability evidence to their prior state. Runtime behavior is unchanged.

Not yet verified: live memory backup picker import/export, enabled memory
injection review after this CRUD journey, semantic conversation continuity,
cross-platform/package acceptance, and 1M+ token context.

## Slice 144 — live web search with citations

Status: `implemented_verified_windows_web_search_citations`

The opt-in web-search capability is now verified through the native Windows
journey. In a disposable local-only profile, the composer exposed the Web
search toggle and its privacy explanation; enabling it reported that the query
would go to DuckDuckGo and the prompt/results to the selected model. Sending a
current-information question produced `Found 6 web sources`, a local-model
answer containing a source table, and six rendered clickable source cards with
titles, domains, URLs, snippets, and the incomplete-snippet warning.

Changed files:

- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Live Windows disposable profile — passed: toggle on, query send, local
  provider response, source table, and six accessible `Open web source` cards
  were observed end to end.
- The app persisted source metadata with the assistant message and exposed
  source links independently from the model-generated prose.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  already passed the parser, URL-safety, and source-metadata coverage used by
  this path.
- `npm run audit:future-features` and `git diff --check` — passed after this
  evidence update.

Privacy/security/licensing: web search is explicit opt-in. The UI discloses
that the query goes to DuckDuckGo and the query plus bounded snippets go to
the selected model endpoint. The journey used a local model, synthetic
question, no credentials, no project files, and no pet state. No third-party
code was added.

Quality limitation: the live model answer reported a stale date even though
the source cards were present. This verifies source retrieval and citation UI,
not factual freshness or citation-grounded answer correctness. Freshness,
answer-grounding evaluation, source ranking, and broader search-provider
coverage remain release gaps.

Rollback: revert the three documentation changes to return the item to its
unchecked/evidence-pending state. Runtime behavior is unchanged.

Not yet verified: web-search freshness and answer-grounding evaluations,
provider-side citation enforcement, cross-platform/package acceptance, and
the remaining multi-provider/RAG backlog.

## Slice 145 — live side-by-side local model comparison

Status: `implemented_verified_windows_local_comparison`

The existing bounded comparison path is now verified in the native Windows
app. A disposable local-only profile exposed two distinct pinned Ollama
models: `wtwk-qwen35-4b:latest` and the installed 1.5B Qwen coder model. The
model picker exposed `Compare pinned models`; its dialog selected both models,
accepted one plain-text prompt, and explicitly stated that attachments, saved
memory, project files, and web results were excluded. The comparison completed
with separate model-labeled responses in the results dialog.

Changed files:

- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Live Windows disposable profile — passed: two pinned local models were
  visible, the comparison prompt was reviewed, both models ran, and the
  results dialog rendered one response column per model.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including minimum-model, local-only, prompt-bound, and bounded
  comparison-policy coverage.
- `npm run audit:future-features` and `git diff --check` — passed after the
  evidence update.

Privacy/security/licensing: both routes were loopback Ollama endpoints and
the settings required local model endpoints. The comparison intentionally
excluded attachments, memory, project context, and web results; no remote
consent, credential, network route, third-party dependency, or pet state was
used.

Rollback: revert the three documentation changes to return the comparison
item to its evidence-pending state. Runtime behavior is unchanged.

Not yet verified: remote comparison consent and provider diversity, resource
scheduling/unloading, comparison cancellation, cross-platform/package
acceptance, and the broader multi-model picker requirement.

## Slice 146 — live generic compatible-provider model picker

Status: `implemented_verified_windows_compatible_picker`

The provider-agnostic model picker is now verified against a disposable local
OpenAI-compatible endpoint. A temporary loopback `/v1/models` service
advertised `mock-chat-small` and `mock-chat-large`. The native app connected
to that endpoint with local-only routing enabled, discovered both models in
Model Library alongside the installed Ollama inventory, selected
`mock-chat-large`, pinned it, and exposed both pinned routes in the composer
model switcher. The live route displayed the selected endpoint and model in
the connection status and model accessibility description.

Changed files:

- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Live Windows disposable profile — passed: generic compatible endpoint
  connection, two-model discovery, model selection, pinning, and hotswitcher
  inspection completed without a cloud endpoint or credential.
- The temporary test service implemented only bounded `/v1/models` and
  `/v1/chat/completions` responses and was stopped after the journey; it was
  not added to the repository.
- `npm run audit:future-features` and `git diff --check` — passed after this
  evidence update.

Privacy/security/licensing: the endpoint was loopback-only and the disposable
settings required local model endpoints. No API key, remote request, project
file, memory, third-party package, or pet state was used.

Quality limitation: the live route proves the generic OpenAI-compatible
discovery/selection path, not every hosted provider or remote-consent branch.
The selected discovered model was labeled `Current local endpoint`, so
durable provider identity normalization across all provider types remains an
open routing-quality item.

Rollback: revert the three documentation changes to return the picker item to
its evidence-pending state. Runtime behavior is unchanged.

Not yet verified: remote compatible-provider discovery and consent, provider
identity normalization, multi-route scheduling, unload/queue behavior,
cross-platform/package acceptance, and model-library download workflows.

## Slice 147 — validated Markdown chat transcript export

Status: `implemented_verified_core_native_picker_pending`

The chat export path now offers both the existing portable JSON archive and a
human-readable Markdown transcript. Markdown export reuses the canonical
conversation archive validator before writing, includes UTC export metadata,
conversation identity/status fields, message roles/content, source links, and
attachment names. Attachment bytes remain in the JSON archive only, keeping
the Markdown transcript readable and avoiding a second unbounded binary
export path.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/ConversationMarkdownExporter.cs`
- `native/SkaiDesktop.Core.Tests/ConversationArchiveTests.cs`
- `native/ServerRock.Native/MainPage.ImportExport.cs`
- `scripts/audit-native-ui.cjs`
- `docs/ARCHITECTURE.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including Markdown metadata/message/attachment-name coverage and
  the existing archive validation suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 185 contracts, including the
  Markdown picker type and serializer wiring.
- `npm run audit:future-features` and `git diff --check` — passed.

Privacy/security/licensing: export remains explicit and user-initiated. The
same bounded archive validator protects the Markdown path; source URLs are
preserved as links, attachment names are disclosed, and attachment bytes are
not duplicated into the Markdown file. No network, credentials, third-party
dependency, or pet state is involved.

Rollback: remove the Markdown exporter, its focused test and audit contracts,
the `.md` picker choice, and the architecture/evidence updates. JSON archive
export/import remains available.

Not yet verified: native Windows file-picker selection of `.md` and resulting
file contents, split-pane chat view, per-conversation Markdown directory
layout, and packaged cross-platform export acceptance. The combined future
item remains unchecked until split view and picker acceptance are complete.

## Slice 148 — live Windows Markdown export picker evidence

Status: `implemented_verified_windows_markdown_picker`

The disposable Windows acceptance journey confirmed that the native Save As
picker exposes both supported export choices: the existing `Skai Desktop chat
archive` JSON type and the new `Skai Desktop Markdown transcript` type. This
closes the picker-choice evidence gap for the Markdown export path while
keeping the combined export-and-split-view backlog item open.

Evidence:

- Launched the built `net10.0-desktop` app with isolated settings and
  conversation paths under `.tmp-live-slice148/`.
- Created one synthetic local-model conversation and opened the accessible
  `Export saved chats` action.
- Inspected the native Save As `Save as type` list and observed both JSON and
  Markdown choices.
- Canceled the disposable picker without writing to the user's Documents
  folder or normal conversation history.
- Slice 147 core tests, native build, and 185-contract native UI audit remain
  passing; this slice adds the live picker observation.

Privacy/security/licensing: the journey used only synthetic prompt content and
the local Ollama endpoint. No user data, credentials, network search, pets
state, or handoff files were accessed or modified.

Rollback: revert this evidence section and the corresponding capability-matrix
wording. Runtime behavior is unchanged.

Not yet verified: selecting `.md` and reading back the written file, split-pane
chat view, per-conversation Markdown directory layout, and packaged
cross-platform export acceptance. The combined future item remains unchecked.

## Slice 149 — live Markdown export write/readback

Status: `implemented_verified_windows_markdown_write_readback`

The native Windows export journey now covers the complete disposable Markdown
write/readback path. A synthetic local-model conversation was exported through
the native picker with `Skai Desktop Markdown transcript` selected and the file
was written to `.tmp-live-slice149/export.md`. The app reported one exported
conversation, and an independent readback assertion confirmed the `# Chat
export` header, `Conversations: 1` metadata, synthetic user message, and
assistant transcript section.

Evidence:

- Launched the built `net10.0-desktop` app with isolated settings and
  conversation paths under `.tmp-live-slice149/`.
- Created one synthetic conversation using the local Ollama endpoint.
- Selected the Markdown type in the native Save As picker and wrote only to
  the disposable workspace path.
- Observed the in-app status: `Exported 1 saved conversation(s) to
  export.md.`
- Read the resulting 7,237-byte file and ran deterministic marker assertions;
  all checks passed.

Privacy/security/licensing: the journey used synthetic content and an
on-device model only. No user Documents data, credentials, network search,
pets state, or handoff files were accessed or modified.

Rollback: revert this evidence section and remove the disposable validation
artifacts; runtime behavior is unchanged.

Not yet verified: split-pane chat view, per-conversation Markdown directory
layout, packaged cross-platform export acceptance, and broader export-content
fidelity evaluation. The combined future item remains unchecked until split
view is complete.

## Slice 150 — bounded live split chat view

Status: `implemented_verified_windows_split_chat`

The native chat surface now supports a reversible split view. The current
conversation remains on the left while a saved conversation selected from an
accessible comparison picker renders on the right. The split view is
read-only by design: sending, editing, retrying, and other mutations remain
owned by the normal composer surface, preventing accidental writes to the
comparison chat. Closing the view restores the regular conversation surface.

Changed files:

- `native/ServerRock.Native/SplitConversationView.xaml`
- `native/ServerRock.Native/SplitConversationView.xaml.cs`
- `native/ServerRock.Native/MainPage.SplitChat.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPageHeader.xaml`
- `native/ServerRock.Native/MainPageHeader.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.Messages.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed with `Native core checks passed.`
- `npm run audit:native-ui` — passed with 192 contracts covering the split
  surface, picker, toggle wiring, and accessibility names.
- Live Windows journey used isolated `.tmp-live-slice150/` settings/history,
  created two synthetic local-Ollama chats, opened the `Close split chat view`
  state, verified `Current split chat title`, `Choose comparison chat`, and
  both transcript panes, then closed the view and observed the normal single
  conversation surface restored.
- `npm run audit:future-features` and `git diff --check` — passed.

Privacy/security/licensing: comparison content stays local and read-only in
the current window. No remote route, new dependency, credential, network
search, pets state, or handoff file was used. The picker excludes archived
and current conversations and does not change saved history.

Rollback: remove the split control, split surface, event wiring, audit
contracts, backlog check, and this evidence entry. Normal chat rendering and
export remain available.

Not yet verified: draggable/resizable split ratios, persisted pane geometry,
cross-platform packaged rendering, and editing or composing independently in
both panes. Those remain separate layout/workspace requirements.

## Slice 151 — persisted split ratio control

Status: `implemented_verified_windows_split_ratio_persistence`

The split view now includes a bounded, keyboard-accessible vertical ratio
slider. Values are constrained to 25–75 percent, apply immediately to the two
chat columns, and persist in the existing local settings store under a key
derived from the workspace mode and active project context. This provides a
safe remembered layout preference without introducing a new dependency or
allowing an unreadable zero-width pane.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/SplitConversationView.xaml`
- `native/ServerRock.Native/SplitConversationView.xaml.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.SplitChat.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including settings round-trip coverage for the split ratio.
- `npm run audit:native-ui` — passed with 195 contracts, including the ratio
  control, accessibility help text, event wiring, and bounded persistence.
- `npm run audit:future-features` and `git diff --check` — passed.
- Live Windows disposable journey: initial saved ratio `62` was visible in
  `SplitRatioSlider`; changing it to `68` updated the live accessibility state
  and disposable settings JSON; after closing and relaunching the app, opening
  split view again reported `Value: 68`.

Privacy/security/licensing: only a numeric local layout preference is stored.
No prompt content, secrets, remote endpoint data, pets state, or handoff files
are involved. The ratio is clamped before both layout application and save.

Rollback: remove the ratio slider, settings field, persistence wiring, focused
test, audit contracts, and this evidence entry. The fixed two-pane split view
continues to work.

Not yet verified: draggable divider semantics, drag-and-drop pane layouts,
multiple independently editable/composing panes, cross-platform packaged
rendering, and full workspace-wide geometry persistence.

## Slice 152 — draggable split divider

Status: `implemented_verified_windows_split_divider`

The split comparison surface now supports direct pointer dragging on its
divider gutter. Dragging maps the pointer position to the same bounded 25–75%
ratio used by the keyboard-accessible slider, applies both pane widths
immediately, and persists through the existing settings-backed ratio event.
Pointer capture and capture-loss handling prevent a partially completed drag
from leaving the surface in a stale drag state. The slider remains available
for keyboard and screen-reader users.

Changed files:

- `native/ServerRock.Native/SplitConversationView.xaml`
- `native/ServerRock.Native/SplitConversationView.xaml.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed; existing settings round-trip coverage remains green.
- `npm run audit:native-ui` — passed with the draggable divider contract and
  pointer-handler wiring.
- `npm run audit:future-features` and `git diff --check` — passed.

Privacy/security/licensing: pointer coordinates are used only for local pane
layout. No message content, credentials, network route, pets state, or
handoff file is accessed. The ratio remains clamped before layout and save.

Rollback: remove the gutter pointer handlers and audit contract; the existing
slider-based ratio control and fixed split view remain available.

Not yet verified: drag-and-drop pane reordering/layouts, multiple independently
editable/composing panes, cross-platform packaged rendering, and full
workspace-wide geometry persistence.

## Slice 153 — per-conversation Markdown directory export

Status: `implemented_verified_core_and_native_build`

Chat export now has a separate folder action that creates a generated export
directory and writes one validated Markdown transcript per saved conversation.
Each filename combines a sanitized title and conversation ID, caps component
length, and falls back safely for empty or invalid names. The existing single
file JSON and Markdown export choices remain unchanged.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/ConversationMarkdownExporter.cs`
- `native/SkaiDesktop.Core.Tests/ConversationArchiveTests.cs`
- `native/ServerRock.Native/ConversationNavigationView.xaml`
- `native/ServerRock.Native/ConversationNavigationView.xaml.cs`
- `native/ServerRock.Native/SidebarView.xaml`
- `native/ServerRock.Native/SidebarView.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.ImportExport.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including safe filename coverage for invalid title characters and
  deterministic one-file-per-conversation directory write/readback coverage.
- `npm run audit:native-ui` — passed with 202 contracts, including the folder
  action, event forwarding, and per-session filename/write path.
- `npm run audit:future-features` and `git diff --check` — passed.

Privacy/security/licensing: folder selection and writes remain local. The
exporter reuses canonical archive validation for every conversation, does not
send content to a provider, and does not copy credentials, pets state, or
handoff files.

Rollback: remove the folder action and `FileNameFor` helper; the existing
single-file exports and import path remain intact.

Not yet verified: live Windows FolderPicker selection and packaged picker
behavior (the current desktop runtime did not surface the picker during the
disposable UI attempt), rich Markdown editing, and attachment binary export
(the Markdown files retain attachment names and the JSON archive remains the
content-preserving format).

## Slice 154 — durable workspace composer drafts

Status: `implemented_verified_windows_restart`

Composer drafts are now stored in the existing local settings JSON by
workspace. The in-memory store accepts and emits bounded snapshots, the UI
debounces writes for 500 ms instead of writing on every keystroke, workspace
switches and new-chat clearing flush immediately, and startup restores the
active workspace draft without triggering a second persistence cycle.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core/Services/WorkspaceDraftStore.cs`
- `native/SkaiDesktop.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.SlashCommands.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including isolated workspace and snapshot-restore coverage.
- `npm run audit:native-ui` — passed with 203 contracts, including the
  composer text-change persistence event.
- `npm run audit:future-features` and `git diff --check` — passed.
- Live Windows disposable journey: typed `draft survives restart`, observed
  `workspaceDrafts.ChatBot` in the isolated settings JSON, closed the app,
  relaunched with the same isolated profile, and observed the exact draft
  restored in the accessible Message composer.

Privacy/security/licensing: drafts remain local, bounded, and workspace-keyed.
They are not sent to a model until the user presses Send, and no credentials,
pets state, or handoff files are included.

Rollback: remove the settings field, debounce timer, startup restoration, and
snapshot APIs; existing in-memory workspace switching still remains available.

Not yet verified: encrypted-at-rest draft storage, cross-device synchronization,
attachment-draft persistence, and cross-platform packaged restart behavior.

## Slice 155 — durable local attachment drafts

Status: `implemented_verified_windows_restart`

Unsent attachments now persist in a separate atomic local store keyed by
workspace, independent of general preferences. The store validates a maximum
of four files and 12 million content characters per workspace, preserves
existing files on malformed input, restores the active workspace at startup,
flushes on selection/removal/workspace changes, and clears after send or new
chat. Text, extracted PDF content, and bounded image data use the same local
attachment model as the existing send path.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/WorkspaceAttachmentDraftStore.cs`
- `native/SkaiDesktop.Core.Tests/AttachmentTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.Attachments.cs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including local round-trip and clear/remove coverage.
- `npm run audit:native-ui`, `npm run audit:future-features`, and
  `git diff --check` — passed.
- Live Windows disposable journey: seeded an isolated attachment-drafts file
  with `notes.md`, launched the native app with isolated settings/history, and
  observed the accessible `Remove attachment notes.md` chip in the composer.

Privacy/security/licensing: pending content remains on the local profile and
is never sent merely by being restored. Size/file bounds, atomic replacement,
and the existing attachment policy limit local exposure; no credentials, pets
state, or handoff files are involved.

Rollback: remove the separate store and restore/flush hooks; existing
attachments still work for the current process and queued messages remain
independently persisted.

Not yet verified: encrypted-at-rest attachment drafts, cross-device sync,
cross-platform packaged restart behavior, and a picker-based add/remove live
journey in the current desktop runtime.

## Slice 156 — Activity search focus handoff

Status: `implemented_verified_windows_keyboard_focus`

Activity search and agent-task search now enqueue their initial query focus
after the ContentDialog opens. This avoids the native dialog's focus request
being overwritten by the underlying composer while preserving the existing
accessible labels and result lists.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed.
- `npm run audit:native-ui` — passed with 205 contracts, including both
  dispatcher-backed search focus paths.
- `npm run audit:future-features` and `git diff --check` — passed.
- Live Windows journey: opened Activity & tasks, activated Search activity,
  observed the query field focused visually, and typed `ratio` through native
  keyboard input; the dialog document text reflected `ratio` while the
  underlying composer remained unchanged.

Privacy/security/licensing: the change only affects local focus routing and
searches already-local activity data. No provider request, file write,
credential access, pets state, or handoff file was involved.

Rollback: revert the two dispatcher-backed focus handlers and their audit
contracts; search remains functional with the previous immediate focus call.

Not yet verified: Narrator-specific announcements, Linux/Skia focus parity,
and full keyboard activation of every Activity action such as export and
schedule.

## Slice 157 — Dialog action focus hardening

Status: `implemented_build_verified_live_acceptance_open`

The Activity & tasks dialog, status-history dialog, and agent-task planning
dialog now enqueue their initial focus after opening. The intent is to keep
focus inside the newly opened dialog when the underlying composer or sidebar
finishes its own activation work. The existing Activity and agent-task search
focus handoff remains covered by Slice 156.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed.
- `npm run audit:native-ui` — passed with 206 contracts.
- `npm run audit:future-features` and `git diff --check` — passed.
- Live Windows journey: opened Activity & tasks from the sidebar and confirmed
  the dialog and action controls were present. Native accessibility still
  reported the underlying Message composer as the focused element after the
  dispatcher handoff, so keyboard/screen-reader focus acceptance remains open.

Privacy/security/licensing: this change only routes local focus and does not
send provider requests, write files, access credentials, or modify pets state.

Rollback: revert the dispatcher-backed initial-focus handlers and their audit
contracts; the dialogs retain their prior opening behavior.

Next: isolate the native Popup/ContentDialog focus race with a targeted
keyboard test for Activity export, status-history dismissal, and agent-task
planning before marking this seam verified on Windows. Narrator announcements,
Linux/Skia parity, and packaged-runtime acceptance also remain open.

## Slice 158 — Loaded-content focus retry

Status: `implemented_verified_dialog_rendering_accessibility_gap_open`

The focus handoff now retries from both the dialog `Opened` event and the
loaded dialog content at low dispatcher priority. This covers the native
Popup/ContentDialog ordering case where the control tree is not yet loaded
when `Opened` fires. Activity export, status history, and agent-task planning
each use a named local focus helper; the existing search dialogs retain their
dispatcher-backed query focus.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after
  closing the exact verification process that held the output assembly.
- `npm run audit:native-ui` — passed with 208 contracts.
- `git diff --check` — passed.
- Live Windows journey: rebuilt and launched the desktop app, opened Activity &
  tasks, confirmed the rendered dialog and all local action controls, then
  closed it without sending a provider request or writing evidence. The native
  accessibility tree continued to report `PromptBox` as focused, so focus is
  not marked fully accepted.

Privacy/security/licensing: the retry only changes local UI focus scheduling;
it does not route data, access credentials, write user files, or modify pets.

Rollback: revert the loaded-content hooks and low-priority focus helpers; the
dialog actions and search behavior remain otherwise unchanged.

Next: add a focused Windows accessibility harness or a supported Popup focus
bridge before claiming keyboard/screen-reader acceptance. Narrator behavior,
Linux/Skia parity, and packaged-runtime acceptance remain open.

## Slice 159 — Scheduled-task dialog journey

Status: `implemented_verified_windows_dialog_cancel`

The existing local scheduler received a fresh Windows user-journey check. The
Activity & tasks surface opened the Schedule task form, exposed the prompt,
date, time, workspace, and recurrence controls with accessible names, and
showed the safety disclosure that scheduled work is surfaced for review rather
than sent automatically. The disposable form was cancelled before submission,
so no scheduled task, conversation, provider request, or file mutation was
created.

Changed files:

- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- Live Windows journey: opened Activity & tasks, activated Schedule task,
  inspected the rendered form and its accessible control tree, then activated
  Cancel and confirmed return to the workspace with no scheduled-task row.
- The native build, Core checks, and 208-contract UI audit remain green from
  Slice 158; this slice changed documentation only.

Privacy/security/licensing: the journey remained local and was cancelled before
any durable scheduler write or model/provider action. No credentials, cloud
route, pets state, or third-party content was involved.

Rollback: remove this evidence entry if the live journey is invalidated; no
product state requires rollback.

Remaining: keyboard/screen-reader focus reporting for Popup dialogs, actual
scheduled-task persistence/run-now outcomes across restart, timezone/locale
coverage, and autonomous/background execution remain open.

## Slice 160 — In-app documentation

Status: `implemented_verified_windows_more_and_command_palette`

Added a bounded, local in-app guide covering getting started, privacy and
routing, review-first actions, local data controls, and navigation to major
workspaces. It is reachable from More workspace options and from the Ctrl+K
command palette, uses the centralized product identity, and renders its
content inside an accessible scroll region.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed.
- `npm run audit:native-ui` — passed with 212 contracts.
- `npm run audit:future-features` — passed with 160 items, 11 sections, and 11
  checked items.
- `git diff --check` — passed.
- Live Windows journey: More workspace options displayed In-app documentation;
  the guide rendered five readable sections in an accessible scroll region.
  Ctrl+K filtered to `documentation`, showed exactly the new command, and
  Run action opened the same guide. Both journeys closed without provider or
  file activity.

Privacy/security/licensing: the guide is static local UI text and does not
inspect conversations, route prompts, access credentials, or add a dependency.

Rollback: remove the documentation command/menu action and revert the checked
backlog item; no persisted user state is created by opening the guide.

Remaining: richer versioned help content, localization, searchable external
handoff references, and full Popup focus/screen-reader acceptance remain open.

## Slice 161 — Agent-task dependency selection journey

Status: `implemented_verified_windows_dependency_plan_cancel`

The Activity & tasks agent-plan workflow was verified live with a disposable
local plan. The form accepted a title and instruction, exposed existing local
tasks as accessible prerequisite checkboxes, and saved the selected dependency
into the task artifact. Activity rendered the resulting relationship as
`after Disposable final dependency task`. The plan was then cancelled before
queueing or execution.

Changed files:

- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- Live Windows journey: opened Activity & tasks, opened Plan agent task,
  entered bounded disposable text, selected one existing prerequisite, saved,
  reopened Activity, inspected the persisted dependency label, and activated
  Cancel plan.
- The app announced `Agent task plan saved locally for review. No agent or
  provider was started.` and then `Agent task plan cancelled locally before
  execution. No agent or provider was started.`
- No model, provider, shell, Git, file, or network action was started.

Privacy/security/licensing: this was local task-artifact state only; the plan
was cancelled before queueing and no external execution occurred. No
credentials, cloud route, pets state, or third-party package was involved.

Rollback: the disposable plan was cancelled through the product UI; no
remaining task mutation requires cleanup.

Remaining: parallel dispatch, worktree/process isolation, background execution,
task-artifact result UI, cross-platform acceptance, and full Popup focus remain
open.

## Slice 162 — Close-only dialog focus contract

Status: `implemented_build_verified_focus_gap_persists`

Close-only Activity and status-history dialogs now explicitly set
`DefaultButton = ContentDialogButton.Close` in addition to their loaded-content
focus retries. This gives the native dialog a declared default focus target and
keeps keyboard dismissal semantics explicit without changing any action or
provider behavior.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 214 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Activity & tasks after the rebuild and
  inspected the rendered accessible dialog tree. The dialog-owned controls and
  explicit close action remained present, but the native accessibility tree
  still reported `PromptBox` as focused. The experiment therefore does not
  claim full keyboard or screen-reader focus acceptance.

Privacy/security/licensing: this is a local dialog-focus contract only. It
does not send prompts, access credentials, write files, or modify task,
conversation, provider, or pets state.

Rollback: remove the two `DefaultButton` assignments and their audit contracts;
the loaded-content retries and dialog actions remain independently reversible.

Remaining: a supported Uno/Win32 Popup focus bridge or dedicated accessibility
harness is still required for authoritative focus acceptance, along with
Narrator, Linux/Skia, and packaged-runtime coverage.

## Slice 163 — Dialog-container focus attempt

Status: `implemented_supported_focus_bridge_verified_gap_persists`

Activity and status-history dialogs now mark the `ContentDialog` itself as a
tab stop, use cycle-local tab navigation, and call the supported WinUI
`FocusManager.TryFocusAsync` bridge for the first actionable content. This
keeps both keyboard traversal and the dialog-level fallback explicit in the
native implementation. It does not alter provider routing, task execution, or
close behavior.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 216 contracts.
- `git diff --check` — passed.
- Live Windows journey: rebuilt and opened Activity & tasks. The dialog tree
  exposed the correct Activity controls after the supported focus bridge was
  added, but native accessibility still reported `PromptBox` as focused.
  Full Popup keyboard/screen-reader acceptance therefore remains unverified.

Privacy/security/licensing: local focus metadata only; no prompt, provider,
credential, file, network, task, or pets state was changed.

Rollback: revert the `TabFocusNavigation` assignments and
`FocusManager.TryFocusAsync` calls; the existing explicit close defaults and
loaded-content retries remain separate.

Remaining: resolve through a supported Uno/Win32 Popup accessibility bridge or
dedicated harness, then verify Narrator, keyboard traversal, Linux/Skia, and
packaged-runtime behavior.

## Slice 164 — Scheduled-task cancellation

Status: `implemented_verified_windows_cancel_before_execution`

Scheduled tasks can now be cancelled from Activity & tasks while they are
scheduled or paused. Cancellation is durable, records the bounded outcome
`Cancelled before execution.`, prevents future claiming, and leaves completed,
failed, or running tasks unchanged. The UI discloses that cancellation occurs
before execution and does not send a prompt automatically.

Changed files:

- `native/SkaiDesktop.Core/Models/ScheduledTask.cs`
- `native/SkaiDesktop.Core/Services/ScheduledTaskStore.cs`
- `native/SkaiDesktop.Core.Tests/ScheduledTaskTests.cs`
- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including durable cancellation and non-claimability assertions.
- `npm run audit:native-ui` — passed with 218 contracts.
- `git diff --check` — passed.
- Live Windows journey: created a disposable future scheduled prompt, opened
  its Activity row, confirmed Run now/Pause/Cancel controls, activated Cancel,
  and observed `Scheduled task cancelled locally before execution. No prompt
  was sent and no provider was called.`

Privacy/security/licensing: cancellation is local durable state only. It does
not call a model, access credentials, send network traffic, modify files, or
alter pets state.

Rollback: restore the prior scheduled-task status model and remove the Cancel
button; the Core store remains bounded and existing pause/run behavior is
unchanged.

Remaining: autonomous/background scheduling, automatic result logging,
timezone/locale coverage, recurrence backoff, cross-platform packaging, and
full Popup focus acceptance remain open. The broader “Scheduled automations”
future feature remains unchecked because the current scheduler is review-first.

## Slice 165 — Custom ambient appearance presets

Status: `implemented_build_and_core_verified_live_visual_acceptance_pending`

Settings → Appearance now offers persisted ambient presets separate from the
light/dark palette: None, Deep Sea, and Deep Space. Deep Sea renders a blue
gradient with translucent bubbles that drift upward. Deep Space renders a dark
gradient with stars, two planets, and drifting UFO silhouettes. All decoration
is procedural, local, and hit-test-free so it does not intercept chat, sidebar,
composer, or pet interactions. A reduced-motion option keeps the selected scene
visible while pausing animation.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/ServerRock.Native/AppearanceBackdropView.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including persisted backdrop and reduced-motion
  settings.
- `git diff --check` — passed.

Privacy/security/licensing: the visuals are generated locally from vector
elements and do not load images, call a provider, access credentials, inspect
files, or send telemetry. No pets code or pet state was changed. Motion is
bounded to a small in-memory element set and the overlay is non-interactive.

Rollback: set the backdrop to None in Settings or remove the appearance overlay;
the existing light/dark palette and other settings remain independent.

Remaining: complete live Windows visual acceptance for both presets, verify
restart persistence and reduced-motion behavior, then extend the preset catalog
only through similarly bounded, accessible visual components. Cross-platform
Skia, packaged-runtime, localization, and full accessibility coverage remain
open.

## Slice 166 — Local prompt snippets with reviewable expansion

Status: `implemented_verified_windows_save_insert_and_expansion`

Added a bounded Prompt snippets surface reachable from the command palette and
More workspace options. Users can save up to 24 local snippets, each capped at
4,000 characters, insert one into the composer, or explicitly ask the selected
model to expand it. Expansion is not automatic: the returned candidate is shown
in an editable review dialog and only the explicit Use expanded text action puts
it into the composer. Saved snippets remain local settings data and are not sent
until the user inserts them or requests expansion.

Changed files:

- `native/SkaiDesktop.Core/Models/PromptSnippet.cs`
- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.Snippets.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including settings round-trip coverage for snippets.
- `npm run audit:native-ui` — passed with 221 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Prompt snippets from Ctrl+K, saved disposable
  `Review checklist`, inserted it into the composer, and observed the saved
  snippet and exact composer text in the accessibility tree.
- Live Windows expansion journey: selected the saved snippet, explicitly
  requested expansion through the compatible local endpoint, observed the
  editable `Review expanded snippet` dialog, and activated `Use expanded text`.
  The resulting expanded text appeared in `PromptBox`; no message was sent.

Privacy/security/licensing: snippets are bounded local JSON preferences. An
expansion is a visible, user-triggered provider request using the selected route;
the review candidate is not auto-sent or silently saved. No files, credentials,
pet state, or background tasks were accessed.

Rollback: delete the saved snippet from the local settings file or use the
existing settings recovery path; removing the command and More entry leaves
normal composer behavior unchanged.

Remaining: verify provider failure and cancellation messaging for expansion.
Full cross-platform accessibility and packaged-runtime coverage remain open.

## Slice 167 — Review-first built-in translator

Status: `implemented_verified_windows_review_and_insert`

Added a bounded translator reachable from Ctrl+K and More workspace options.
Users enter source text, choose a target language, explicitly request a
translation through the selected model route, edit the candidate in a review
dialog, and explicitly insert the reviewed result into the composer. Translation
never sends the composer automatically.

Changed files:

- `native/ServerRock.Native/MainPage.Translator.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed.
- `npm run audit:native-ui` — passed with 224 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Translate text from Ctrl+K, entered disposable
  `Hello handoff`, selected Spanish, observed the editable `Review Spanish
  translation` result, activated `Use translation`, and verified `Hola, cambio`
  in `PromptBox` with the status `Nothing was sent.`

Privacy/security/licensing: translation is an explicit provider request through
the selected route. Source text is disclosed in the translation prompt, the
candidate remains editable and reviewable, and no message is sent automatically.
No files, credentials, pets state, or background tasks were accessed.

Rollback: remove the translator command and More action; normal composer and
provider behavior remain unchanged. Existing settings and conversation data are
not migrated or rewritten.

Remaining: provider failure/cancellation acceptance, richer language detection,
localization, full cross-platform accessibility, and packaged-runtime coverage
remain open.

## Slice 168 — Local quicklinks

Status: `implemented_verified_windows_save_select_remove`

Added bounded local Quicklinks reachable from Ctrl+K and More workspace options.
Users can save up to 32 named `http` or `https` bookmarks, select one, explicitly
open it through the operating system, or remove it. URLs with embedded
credentials, unsupported schemes, duplicates, and oversized values are refused.
The manager does not fetch links or send them to a model while saving or listing.

Changed files:

- `native/SkaiDesktop.Core/Models/Quicklink.cs`
- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.Quicklinks.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including Quicklink settings persistence.
- `npm run audit:native-ui` — passed with 227 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Quicklinks from Ctrl+K, saved disposable
  `Handoff docs` at `https://example.com/handoff`, selected it, observed enabled
  Open selected and Remove selected controls, then removed it and observed the
  local-removal status. The composer and provider state were unchanged.

Privacy/security/licensing: bookmarks are local JSON preferences. Validation
rejects non-web schemes and embedded credentials; no URL is opened or fetched
until the user chooses Open selected. No provider, file, credential, or pets
state was accessed.

Rollback: remove the Quicklinks command and More action or delete saved entries
from local settings; existing settings and conversation data remain intact.

Remaining: folders/sync, richer metadata, browser-panel integration, and full
cross-platform accessibility and packaged-runtime coverage remain open.

## Slice 169 — Local developer mode diagnostics

Status: `implemented_verified_windows_toggle_and_diagnostics`

Added a persisted Advanced → Developer mode toggle and a gated Developer
diagnostics action in Ctrl+K and More workspace options. When enabled, the
surface reports bounded non-secret facts such as runtime platform, endpoint
classification, selected provider/model, local feature toggles, and counts of
saved chats, projects, and quicklinks. It does not grant tools, bypass review
or approval gates, run commands, expose credentials, or send telemetry.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.DeveloperMode.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including DeveloperMode settings persistence.
- `npm run audit:native-ui` — passed with 230 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Connection & settings from Ctrl+K, opened
  Advanced, enabled and saved Enable developer mode, then opened Developer
  diagnostics from Ctrl+K and observed the accessible `Developer diagnostics`
  summary dialog. No message was sent and no provider request was made.

Privacy/security/licensing: the mode is opt-in local preference data. The
diagnostics summary deliberately omits API keys and secret values and keeps
existing provider, command, file, and approval boundaries unchanged. No pets
code or state was changed.

Rollback: disable Developer mode in Advanced or remove the diagnostics command;
normal chat, settings, provider, and approval behavior remain unchanged.

Remaining: richer debugger integrations, trace viewers, cross-platform
diagnostics, and packaged-runtime/accessibility coverage remain open. This
slice does not claim an IDE or unrestricted developer console.

## Slice 170 — Bounded attachment retrieval

Status: `implemented_core_verified_bounded_lexical_retrieval`

Oversized text and PDF attachments now pass through a local lexical retrieval
step before prompt construction. The query terms from the user turn score
bounded 2,000-character chunks; the highest-scoring chunks are restored to
document order and capped at 12,000 characters per attachment. Small files
continue using the prior full-text path, and images remain image parts for the
existing vision-capable provider path. Retrieved content remains labeled
untrusted context and no new provider route is introduced.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/ChatAttachmentPrompt.cs`
- `native/SkaiDesktop.Core.Tests/AttachmentTests.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including oversized attachment query-relevance and
  output-bound assertions.
- `npm run audit:native-ui` — passed with 230 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: retrieval is local deterministic string matching;
it adds no dependency, telemetry, vector store, credential access, or hidden
network request. Attachment content remains explicitly marked untrusted so it
cannot silently become instructions. Existing remote-attachment consent and
local-only endpoint policy remain unchanged.

Rollback: restore `ChatAttachmentPrompt.AppendTo` at the provider message
construction call site; the existing attachment picker, bounds, and image path
remain independently usable.

Remaining: vector database backends, hybrid BM25/vector/rerank search, OCR and
additional extraction engines, corpus management, agentic retrieval, and a
full NotebookLM-style uploaded-document mode remain open.

## Slice 171 — Evidence-backed backlog reconciliation

Status: `verified_existing_context_compaction_and_structured_task_list`

Reconciled two future-feature entries against existing implementation and
evidence rather than adding duplicate behavior. `Context compaction` now
records the deterministic bounded provider-side compaction path that marks
omitted history before an oversized request. `Structured task lists` now
records the persisted local AgentTaskArtifact plan, dependency-aware readiness,
queue/search/cancel controls, and prior Windows planning acceptance. Neither
entry claims semantic summarization, 1M+ context, autonomous execution, or
parallel agent dispatch.

Changed files:

- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Existing Core provider tests pass for bounded oversized-history compaction,
  omitted-history markers, newest-turn preservation, and source immutability.
- Existing AgentTaskArtifact Core tests pass for dependency order, readiness,
  queue/claim/complete/fail/cancel transitions, persistence, and bounded
  artifacts.
- Existing Slice 161 live Windows evidence recorded the review-first planning,
  dependency selection, persisted plan, and cancellation journey.

Privacy/security/licensing: documentation-only reconciliation; no runtime,
provider, credential, file, or pets state changed.

Rollback: restore the two checklist markers and their explanatory qualifiers;
the underlying implementations are unaffected.

Remaining: semantic compaction, larger context windows, autonomous/background
execution, parallel delegation, and cross-platform/package acceptance remain
open and are not claimed complete.

## Slice 172 — Pending attachment previews

Status: `implemented_build_core_and_audit_verified_live_picker_gap_open`

Pending text, PDF, and image attachments now expose a separate Preview action
beside the existing remove action. Text and PDF previews are local read-only
bounded views (12,000 characters with a truncation marker); images are decoded
locally for a bounded image view, with a safe fallback message if rendering
fails. Opening a preview does not send the attachment or call a provider.

Changed files:

- `native/ServerRock.Native/MainPage.Attachments.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed.
- `npm run audit:native-ui` — passed with 232 contracts.
- `git diff --check` — passed.
- Live Windows inspection confirmed the Attach a file control, but repeated
  activation did not surface a native file-picker window in this Uno/Win32
  session, so no attachment was selected and the preview dialog was not
  falsely claimed as live-accepted.

Privacy/security/licensing: preview content stays in the existing local pending
attachment draft. The dialog is read-only, content remains untrusted, image
decoding is bounded, and no new dependency, network call, credential access,
or pets state is involved.

Rollback: remove the Preview buttons and `PreviewPendingAttachmentAsync`; the
existing attach/remove/send flow remains unchanged.

Remaining: resolve or provide a supported native picker harness, then live-test
text, PDF, and image previews; full RAG, OCR, cross-platform, and packaged
acceptance remain open.

## Slice 173 — Desktop picker window-handle bridge

Status: `implemented_build_verified_live_picker_gap_persists`

The native desktop target now defines `SRAI_DESKTOP` and compiles the existing
Win32 `InitializeWithWindow` bridge for desktop picker surfaces, not only the
packaged Windows target. The bridge was applied consistently to attachment,
project, import/export, plugin, settings, companion, and sidebar pickers. This
is a platform plumbing correction only; it does not broaden picker file types
or change attachment safety limits.

Changed files:

- `native/ServerRock.Native/ServerRock.Native.csproj`
- `native/ServerRock.Native/MainPage.Attachments.cs`
- `native/ServerRock.Native/MainPage.Companion.cs`
- `native/ServerRock.Native/MainPage.ImportExport.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `native/ServerRock.Native/MainPage.Projects.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- Live Windows retry: activated the attachment control twice after the bridge
  change; no native picker window appeared and no attachment was selected.
  The preview journey therefore remains unverified rather than being claimed.
- Existing Core tests and the 232-contract native UI audit remain green from
  Slice 172; the bridge changes are picker-target conditionals only.

Privacy/security/licensing: this changes only native window ownership for local
pickers. It adds no dependency, network request, credential access, file read,
provider call, or pets state change.

Rollback: remove `SRAI_DESKTOP` and restore the picker blocks to `#if WINDOWS`;
the packaged Windows path and existing picker validation remain separate.

Remaining: identify the Uno/Win32 picker-hosting requirement or use a supported
desktop picker harness, then live-test text, PDF, and image previews.

## Slice 174 — Explicit local attachment path fallback

Status: `implemented_build_tests_audit_live_verified`

Composer Tools now exposes an `Attach path` action for supported local files.
The user enters a full path in an explicit dialog; the app does not read the
path until the user confirms Attach. The fallback applies the existing local
attachment policy: supported names, secret-like-name blocking, byte limits,
PNG/JPEG signatures, PDF signatures and local extraction, bounded text, image
budgets, and the five-file message cap. Pending content is still a local draft,
with the existing Preview and Remove actions available before Send.

Changed files:

- `native/ServerRock.Native/ComposerToolbarView.xaml`
- `native/ServerRock.Native/ComposerToolbarView.xaml.cs`
- `native/ServerRock.Native/ComposerInputView.xaml.cs`
- `native/ServerRock.Native/MainPage.Attachments.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed.
- `npm run audit:native-ui` — passed with 232 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Composer Tools, opened Attach a local file path,
  attached `ServerRock_AI_FINAL_COMPLETE_HANDOFF/README.md`, verified the local
  read-only Preview dialog and bounded content, then removed the attachment.

Privacy/security/licensing: the fallback performs no network request and no
provider call. It is explicit-path only, read-only until attachment creation,
bounded by the existing policy, and preserves the untrusted-content warning.
No pets code/settings or new dependency is involved.

Rollback: remove the Attach path control, event forwarding, and
`AttachLocalFileAsync`; the existing picker path remains available.

Remaining: full RAG/vector backends/reranking, OCR, image-model acceptance,
cross-platform picker behavior, and app-preview coverage remain open.

## Slice 175 — Pet companion safety and placement hardening

Status: `implemented_build_tests_audit_live_verified_windows`

The companion surface now has explicit safety boundaries for the skipped pet
work. Persisted custom state sprites are accepted only as bounded fully
qualified local paths with supported image extensions. The drawer blocks the
companion overlay while open, decorative speech/name regions are noninteractive,
and the positioner moves the card away from the composer when its visual bounds
would overlap the prompt area. Existing drag and keyboard movement remain
available on the sprite itself.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/CompanionStateStore.cs`
- `native/ServerRock.Native/MainPage.Companion.cs`
- `native/ServerRock.Native/MainPage.Layout.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed.
- `npm run audit:native-ui` — passed with 232 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened the sidebar, exposed the pet controls, selected
  Byte from the accessible companion picker, dismissed the drawer, and
  confirmed the Byte companion rendered at the top-right without covering the
  composer. The drawer visibly suppressed the companion while open.

Privacy/security/licensing: custom sprite persistence remains local-only and
bounded; unsupported or non-local paths are ignored during normalization. The
pet does not send telemetry or provider data, and no pets-unrelated feature,
credential, or handoff file was changed.

Rollback: remove the path normalization, overlay blocking, and composer
avoidance guard to restore the previous companion behavior.

Remaining: packaged cross-platform behavior, full breakpoint/scale collision
matrix, modal focus restoration, and live custom-sprite picker acceptance.

## Slice 176 — Local path fallback for custom pet sprites

Status: `implemented_build_tests_audit_live_verified_windows`

The custom pet-state sprite dialog now supports an explicit `Use local path`
action when the native picker is unavailable. The path is read only after the
user chooses the action and must be fully qualified, existing, local, bounded
to 1,024 characters and 8 MB, and use SVG, PNG, JPEG, or WEBP. Only the
validated path is stored in the existing companion preferences; Clear custom
sprite remains available for rollback to the built-in asset.

Changed files:

- `native/ServerRock.Native/MainPage.Companion.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed.
- `npm run audit:native-ui` — passed with 232 contracts.
- `git diff --check` — passed.
- Live Windows journey: opened Choose pet → Use your own pictures, selected
  the `success` state, entered `assets/pets/byte-success.png`, observed the
  `success: byte-success.png · saved locally` status, confirmed the custom
  sprite rendered, then cleared the temporary sprite and restored the pet to
  hidden.

Privacy/security/licensing: the fallback is local-only and path-only; it makes
no network or provider request and does not copy or upload the image. Extension,
existence, path-length, and file-size checks are bounded before persistence.

Rollback: use Clear custom sprite for existing state, or remove the local-path
controls and `TryValidateCustomAsset` to restore picker-only customization.

Remaining: native picker hosting, packaged cross-platform behavior, full
breakpoint/scale collision matrix, and modal focus restoration.

## Slice 177 — Read-only agent task detail review

Status: `implemented_build_audit_verified`

Activity & tasks agent artifacts now expose an `Inspect task details` action.
The read-only dialog presents the bounded task title, status, role, instruction,
result summary, and artifact labels from the existing local artifact record.
It does not claim, queue, dispatch, execute, or send anything.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including agent-task lifecycle, dependency, result,
  and search coverage.
- `npm run audit:native-ui` — passed with 232 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: the dialog reads existing local task-artifact
metadata only. It does not access credentials, send provider or network
requests, execute instructions, write project files, or change pet state.

Rollback: remove the detail button and its read-only dialog handler; task
planning, queue, cancellation, and persistence remain unchanged.

Remaining: actual parallel dispatch, process/worktree isolation, task execution
results beyond local review, background execution, and cross-platform acceptance.

## Slice 178 — Bounded project text-file browser and preview

Status: `implemented_build_audit_verified`

Project folder management now offers a read-only `Browse text files` action.
The browser uses the existing selected-folder context reader, displays relative
paths across the selected folders, supports a local path filter, and opens a
bounded text preview without writing or injecting content into a provider
request. It inherits the existing 16-file and 8,000-character per-file limits,
secret-name filtering, excluded-directory rules, UTF-8 validation, and symlink
guards.

Changed files:

- `native/ServerRock.Native/MainPage.Projects.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 232 contracts.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including bounded workspace-context and protected-file
  coverage.
- `git diff --check` — passed.

Privacy/security/licensing: the browser reads only explicitly selected local
folders and keeps the result local. It excludes secrets, dependency/build
directories, reparse points, unsupported binary files, and oversized/invalid
text. No provider, network, credential, or telemetry path is involved.

Rollback: remove the Browse text files button and `ShowProjectFileBrowserAsync`;
folder selection and existing Coding Agent context behavior remain unchanged.

Remaining: nested mutation-aware file editing, binary/PDF/image preview, full
app previews, cross-platform packaged acceptance, and richer file connectors.

## Slice 179 — Local workspace session-state bar

Status: `implemented_build_audit_verified`

The Coding Workspace bar now includes a compact local session-state line. It
surfaces the current workspace mode, message count, pending attachment count,
reviewed file-change count, and queued local agent-plan count. The line updates
with project changes and attachment-draft changes and is informational only;
it does not claim that background work is executing.

Changed files:

- `native/ServerRock.Native/CodingWorkspaceBar.xaml`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.Attachments.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 232 contracts.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — the full Core suite previously passed; this UI-only slice
  does not change Core code or persistence schemas.
- `git diff --check` — passed.

Privacy/security/licensing: the bar reads existing in-memory and local task
state only. It performs no provider, network, credential, file-write, or
telemetry action.

Rollback: remove the session-state TextBlock, property, and update calculation;
the existing project bar, attachment behavior, and task stores remain intact.

Remaining: true background-task status, richer context/change inspection,
cross-platform packaged acceptance, and full accessibility/scale validation.

## Slice 180 — Local image and PDF project previews

Status: `implemented_build_tests_audit_verified`

The project file browser now includes bounded PNG/JPEG and PDF entries in
addition to text files. Image previews read only the bounded local bytes,
validate the PNG/JPEG signature, and render through the existing native image
surface. PDF previews read within the existing PDF byte limit and extract
bounded selectable text locally; scanned PDFs report that OCR is unavailable.
The browser continues to skip secrets, dependency/build directories, reparse
points, oversized files, and unsupported types.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/WorkspaceFileContext.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.Projects.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including bounded image/PDF enumeration and protected
  path assertions.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 232 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: previews remain local and read-only. Size limits,
file signatures, secret filtering, excluded directories, and reparse-point
checks apply before bytes are rendered or PDF text is extracted. No provider,
network, credential, or telemetry path is involved.

Rollback: remove `EnumeratePreviewableFiles` and the media/PDF branches from
the browser; the original bounded text preview and project-folder controls
remain available.

Remaining: OCR, app/executable previews, mutation-aware editing, full artifact
previews, cross-platform packaged acceptance, and richer file connectors.

## Slice 181 — OpenAPI local-path review fallback

Status: `implemented_build_audit_verified`

The Plugins OpenAPI review surface now provides an explicit `Use local path`
fallback beside the native JSON picker. It accepts only a bounded fully
qualified `.json` path, rejects control characters, missing files, reparse-point
files, and oversized input, then parses the local OpenAPI 3 document through
the existing untrusted, execution-free discovery flow. The fallback does not
fetch a server, grant permissions, invoke tools, or send schema content to a
provider.

Changed files:

- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 234 contracts.
- Existing Core OpenAPI discovery tests passed in the full Core suite; this
  slice changes only the native review entry point and audit contracts.
- `git diff --check` — passed.

Privacy/security/licensing: the fallback reads only the path explicitly
provided by the user, applies the existing document byte/character bounds, and
keeps all discovered operations untrusted. No network, credential, provider,
or third-party execution path is introduced.

Rollback: remove the local path controls and handler; native picker review and
the existing schema discovery service remain unchanged.

Remaining: authenticated retrieval, external references, tool authorization
and execution, long-lived MCP resources/notifications, process supervision,
cross-platform packaged acceptance, and full live picker/path acceptance.

## Slice 182 — Enforce fully qualified OpenAPI fallback paths

Status: `implemented_build_audit_verified`

The OpenAPI local-path fallback now rejects relative paths before normalization,
matching its documented local-file boundary and the attachment/pet path policy.
The existing extension, existence, reparse-point, and size checks remain in
place.

Changed files:

- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 234 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: this is a local validation-only boundary change;
no provider, network, credential, or execution behavior changes.

Rollback: remove the rooted-path guard; all other OpenAPI review validation
remains unchanged.

## Slice 183 — Reviewable documents-only Chat mode

Status: `implemented_build_core_audit_verified`

Chat now exposes a `Documents only` composer control. When enabled, it turns
web search off, requires at least one current attachment before sending, adds a
bounded grounding instruction to the system context, and excludes project
instructions, project-file excerpts, saved-memory injection, and stale web
context from that request. The attachment content remains untrusted reference
data; the mode does not claim that documents contain an answer when they do
not. Slash-command web enablement and research mode are also blocked while the
toggle is active.

Changed files:

- `native/ServerRock.Native/ComposerToolbarView.xaml`
- `native/ServerRock.Native/ComposerToolbarView.xaml.cs`
- `native/ServerRock.Native/ComposerInputView.xaml.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Composer.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.SlashCommands.cs`
- `native/SkaiDesktop.Core/Infrastructure/ModelInstructions.cs`
- `native/SkaiDesktop.Core.Tests/ChatMessageTests.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/FUTURE_FEATURES.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including the documents-only grounding instruction
  contract and the full existing suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 236 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: this mode is local UI/prompt policy. It does not
call a new service, persist document contents, silently route to a cloud model,
or execute document instructions. Existing explicit remote-attachment consent
still applies when the selected endpoint is not local.

Rollback: turn the control off or remove its prompt guard/instruction; normal
Chat routing and existing attachment behavior remain unchanged.

Remaining: durable uploaded-document corpus management, vector/hybrid retrieval,
OCR, semantic grounding evaluation, cross-platform acceptance, and full
NotebookLM-style project isolation.

## Slice 184 — Reconcile bounded backlog evidence

Status: `implemented_documentation_reconciled`

The future-feature inventory now distinguishes bounded implementations from
their broader unchecked requirements: the nested project file browser, local
text/image/PDF previews, workspace session-state bar, and session-only
documents-only Chat guard are described without marking the full feature
families complete. Full tree/mutation workflows, app previews, background task
status, durable document corpora, and packaged/live acceptance remain open.

Changed files:

- `docs/FUTURE_FEATURES.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Repository inspection confirmed the referenced bounded implementations and
  their existing Slice 178–183 build, test, and audit records.
- `git diff --check` — passed.

Privacy/security/licensing: documentation only; no application behavior,
provider route, credentials, user data, or pet state changed.

Rollback: revert the explanatory parentheticals and this slice record; prior
unchecked backlog labels remain unchanged.

## Slice 185 — Centralize stable GitHub credential identifiers

Status: `implemented_build_tests_audit_verified`

GitHub credential encryption and keyring-label literals now live in
`ProductIdentity` as explicitly stable storage identifiers. The credential
store no longer couples its entropy or Secret Service label directly to the
current display-name source, while the exact existing values remain unchanged
so saved credentials continue to decrypt and keyring entries remain addressable
through a future display rebrand.

Changed files:

- `native/SkaiDesktop.Core/Models/ProductIdentity.cs`
- `native/SkaiDesktop.Core/Infrastructure/GitHubCredentialStore.cs`
- `native/SkaiDesktop.Core.Tests/ProductIdentityTests.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
  --no-restore` — passed, including the centralized stable-identifier contract
  and the existing credential tests.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 236 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: this changes only constant ownership; credential
bytes remain protected by the existing DPAPI/keyring paths and no credentials
are read, migrated, or transmitted by the slice.

Rollback: restore the two existing literals in `GitHubCredentialStore`; the
stable values and credential storage behavior are otherwise unchanged.

Remaining: complete rebrand verification across packaging metadata, installers,
localization, and all release artifacts; full keyring parity and OS isolation
remain open.

## Slice 268 — Preserve Chat reading position while new work arrives

Status: `implemented_live_electron_verified`

Chat now distinguishes a user reading older turns from a user who is following
the latest response. New messages and busy-state changes only auto-scroll when
the message list is already near the bottom. Conversation-specific scroll
positions continue to restore on thread switches, and a compact `Jump to
latest` action appears when older content is being read.

Changed files:

- `apps/desktop/src/features/chat/ChatPage.tsx`
- `apps/desktop/src/styles.css`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Chat remains visually bounded with the composer docked at the bottom.
- The rebuilt Electron Chat surface rendered with the composer docked at the bottom and the message region contained inside the conversation workspace.
- The scroll listener now owns the latest-position decision; source inspection confirms upward reading is preserved across message and busy-state changes.
- The jump control is conditional on overflow and returns to the latest turn through the same bounded scroll region.
- `npm run build`, `npm run typecheck`, `npm run check:syntax`, `npm run audit:local-only`, `npm run audit:electron-runtime`, `npm run check:hygiene`, and `git diff --check` pass.

## Slice 267 — Make Percy recoverable and visibly alive as a desktop companion

Status: `implemented_electron_runtime_verified`

Percy remains a separate transparent, always-on-top Electron window with a
drag gesture that uses screen coordinates, persisted placement, and completion
messages from the local agent bridge. This slice hardens the behavior that
matters outside the main app: saved positions are clamped back onto a connected
display when monitor topology changes, idle and success states use their own
six-frame transparent strips, and reduced motion pauses animation without
turning a completion state into a frozen sprite. The companion remains local;
no cloud service or unrestricted renderer API was added.

Changed files:

- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/components/overlays/PetWindow.tsx`
- `apps/desktop/src/styles.css`
- `docs/GOAL_EXECUTION.md`

Evidence:

- The rebuilt Electron process exposed separate `Skai Desktop · chat` and `Percy · Skai companion` windows.
- Live capture showed the blue wolf over the desktop/app behind it with no rectangular pet background.
- The renderer exposes the drag surface and the main process accepts screen-coordinate drag IPC; the CUA harness could not complete a transparent cross-window drag because its screenshot-region binding rejected the window crop.
- Completion routing and success/bubble state remain wired through the local agent response path; no cloud service or unrestricted renderer API was added.

## Slice 269 — Bound the Marketplace catalog workspace

Status: `implemented_live_electron_verified`

Marketplace now uses the same bounded desktop workspace pattern as the primary
surfaces. The page header and search/filter controls stay stable while the
category results and selected-category detail pane own their own overflow. On
narrow windows it falls back to natural page flow instead of trapping content
inside a clipped split pane.

Changed files:

- `apps/desktop/src/App.tsx`
- `apps/desktop/src/features/catalog/CatalogPages.tsx`
- `apps/desktop/src/styles.css`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Rebuilt Electron Marketplace rendered as a full-width bounded catalog workspace.
- Search and category controls stayed in the stable toolbar while the category region remained scrollable.
- Clicking the Voices filter updated the catalog to one result and changed the selected detail state; selecting Tools & integrations updated the detail pane and selection state as well.
- The shared desktop shell now stays viewport-bounded, with the navigation and header fixed while the Marketplace grid exposes its own scrollbar.
- Narrow-window flow remains handled by the <=900px fallback.
- `npm run build`, `npm run typecheck`, `npm run check:syntax`, `npm run audit:local-only`, `npm run audit:electron-runtime`, `npm run check:hygiene`, and `git diff --check` pass.

## Slice 199 — Restore stable package identifiers after rebrand

Status: `implemented_release_gate_verified`

The display rebrand remains `Skai Desktop`, while the install folder and Linux
launcher identifiers are restored to the existing lowercase `server-rock-ai`
package identifiers. This preserves upgrade and launcher continuity across the
rebrand and satisfies the fail-closed release metadata validator.

Changed files:

- `packaging/native/release-manifest.json`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run check:release-config` — passed with display/package identifier
  consistency and lowercase package validation.
- `git diff --check` — passed.

Privacy/security/licensing: package metadata only; no runtime routes,
credentials, profile data, or external services changed.

Rollback: restore the manifest identifiers only if the release channel has an
explicit migration plan for a new package namespace. Existing profile data is
not moved or deleted by this slice.

Remaining: signed installers, clean-machine install/upgrade/uninstall,
rollback, localization, and cross-platform release evidence remain open.

## Slice 210 — Enforce release-workflow audit in CI

Status: `implemented_ci_contract_verified`

Requirement alignment: the release workflow audit added in Slice 203 must be
enforced by the repository gates, not only run manually. This slice adds the
audit script to `npm run check:syntax`, native CI, and the tagged-release
verification job. A future edit that removes tag-only publishing, explicit
gates, signing evidence, or checksum enforcement now fails both source checks
and release verification before packaging/publishing.

Changed files:

- `package.json`
- `.github/workflows/native-ci.yml`
- `.github/workflows/native-release.yml`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run check:syntax` — passed and now parses
  `scripts/audit-release-workflow.cjs`.
- `npm run audit:release-workflow` — passed with the tag-only,
  gated, signed-evidence contract.
- `npm run check:hygiene` — passed (749 tracked files).
- `npm run check:release-config` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: CI configuration and local static checks only; no
credentials, release tags, artifacts, network calls, or third-party code were
introduced.

Risk and rollback: remove the two workflow steps and the syntax entry to return
to the prior advisory-only state; the release workflow's runtime gates remain
unchanged.

Remaining: an actual tagged CI run with owner-provided signing material,
clean-machine lifecycle evidence, and distro acceptance are still required.

## Slice 211 — Close the shared native UI token gap

Status: `implemented_ui_audit_build_verified`

Requirement alignment: the native UI already used semantic brushes and type
scale resources across its XAML shell, but the shared `App.xaml` token layer
still embedded raw accent-button colors and repeated typography values. This
slice moves those values behind named resources so theme/accent changes have a
single, reviewable source of truth. The audit now asserts those resource
contracts instead of allowing the gap to regress.

Changed files:

- `native/ServerRock.Native/App.xaml`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `npm run audit:native-ui` — passed with 285 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: visual resource definitions and local static audit
only; no network, credential, storage, provider, or execution behavior changed.

Risk and rollback: restore the prior literal values and remove the seven audit
contracts. No persisted data or package identifiers are affected.

Remaining: dynamic code-built views still contain a larger set of legacy
literal typography/color values; migrating those safely is the next UI
refinement slice, with decorative backdrop colors intentionally remaining
local to the backdrop renderer.

## Slice 212 — Tokenize GitHub review typography

Status: `implemented_ui_audit_build_verified`

Requirement alignment: the GitHub sign-in, repository clone, and change-review
dialogs are high-impact workflow surfaces. Their code-built text controls were
still using raw font sizes despite the shared native type scale. This slice
migrates those sizes to semantic resources, including the one-time device code,
so light/dark/accent and future density changes remain consistent across the
GitHub workflow.

Changed files:

- `native/ServerRock.Native/App.xaml`
- `native/ServerRock.Native/MainPage.GitHub.cs`
- `native/ServerRock.Native/MainPage.GitHub.Changes.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `npm run audit:native-ui` — passed with 286 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: presentation-only resource references; GitHub
credentials, repository validation, clone behavior, and conflict safeguards are
unchanged.

Risk and rollback: restore the prior `FontSize` literals and remove the device
code token contract. No persisted data or network behavior changes.

Remaining: other code-built dialogs still have legacy literal typography and
some historical color values; those will be migrated in focused surfaces with
visual review rather than a broad mechanical rewrite.

## Slice 213 — Tokenize Settings dialog typography and status text

Status: `implemented_ui_audit_build_verified`

Requirement alignment: the Settings dialog is the main customization and
connection surface, so its code-built explanatory text must follow the same
semantic type and contrast tokens as the XAML shell. This slice migrates the
advanced-generation, appearance, pets, host, archive, and local-model status
copy from raw font sizes and historical muted colors to the shared resources.
The user-selected accent preset definitions remain explicit by design; they
are input data for the accent picker, not incidental view styling.

Changed files:

- `native/ServerRock.Native/MainPage.Settings.cs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `npm run audit:native-ui` — passed with 286 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- Settings now has no incidental numeric `FontSize` or historical `ColorHelper` styling literals; only accent presets and transparent state remain.

Privacy/security/licensing: presentation-only resource references; settings
persistence, local model discovery/deletion safeguards, credentials, and
provider behavior are unchanged.

Risk and rollback: restore the previous Settings presentation values. No
stored preferences or network behavior changes.

Remaining: other code-built surfaces still require focused migration and visual
review; this slice does not claim full application-wide token coverage.

## Slice 214 — Tokenize chat history and source cards

Status: `implemented_ui_audit_build_verified`

Requirement alignment: Recents/folders and web-source cards are always-visible
chat-shell surfaces. Their code-built rows still carried historical palette
colors and numeric font sizes, which caused them to drift from the current
light/dark/accent token system. This slice migrates folder headers, empty
states, chat rows, archive controls, message source cards, and attachment
labels to semantic resources and adds focused audit contracts.

Changed files:

- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.Messages.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `npm run audit:native-ui` — passed with 289 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- Recents and Messages contain no incidental numeric `FontSize` or historical
  `ColorHelper` styling literals.

Privacy/security/licensing: presentation-only token references; chat storage,
source validation, external-link policy, and message actions are unchanged.

Risk and rollback: restore the previous row/source presentation values and
remove the focused audit contracts. No conversations or settings are changed.

Remaining: other code-built surfaces still need focused migration and visual
review; full application-wide token coverage is not claimed.

## Slice 215 — Add guarded Git worktree isolation boundary

Status: `implemented_core_verified_boundary`

Requirement alignment: `PROJECT-013` requires concurrent write agents not to
share an unguarded repository target. This slice adds a reusable Core
`GitWorktreeCoordinator` that validates the repository root, rejects the root
and nested targets, checks branch names, creates a sibling worktree on a named
branch, and releases only a registered clean worktree. Dirty release is
refused, and no force-removal path is exposed by the production service.

The coordinator deliberately does not dispatch models, tools, or processes and
does not claim that parallel agent orchestration is complete. A future caller
must acquire a lease before any write-capable agent session and retain the
existing approval/command boundaries.

Changed files:

- `native/SkaiDesktop.Core/Services/GitWorktreeCoordinator.cs`
- `native/SkaiDesktop.Core.Tests/GitHubTests.cs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- Full `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including real temporary-repository worktree lifecycle checks.
- Worktree tests prove sibling isolation, requested branch checkout, dirty-target refusal, clean registered release, and nested-target rejection.
- No production force-delete or unbounded process output path was added.

Privacy/security/licensing: local Git metadata and filesystem paths only; no
network, credentials, model routing, or third-party code changed. Git output
is bounded and branch/revision values are passed as argument-list entries.

Risk and rollback: remove the coordinator and focused tests; existing GitHub
review/commit behavior remains unchanged. A created lease should be released
cleanly or recovered explicitly by its caller before rollback.

Remaining: integrate lease acquisition into agent session creation, add process
and parallel-session isolation, expose review/recovery state in the UI, and
complete live Windows/Linux worktree acceptance.

## Slice 216 — Bind agent execution to durable worktree leases

Status: `implemented_core_verified_executor_isolation`

Requirement alignment: the isolated Git boundary is now used by the durable
agent-task executor. `ExecuteNextInWorktreeAsync` claims one dependency-ready
task, creates a sibling worktree on a task-specific branch, persists the
repository/path/branch lease on the task artifact, and injects the lease into
the caller-supplied handler. Completion, failure, and cancellation retain the
worktree for review; `ReleaseTaskWorktreeAsync` is explicit and delegates to
the clean-only coordinator release path.

The executor still does not choose models, invoke tools, start processes, or
dispatch parallel tasks. A retained worktree is visible in durable task state,
so a caller can review or recover it instead of losing changes silently.

Changed files:

- `native/SkaiDesktop.Core/Models/AgentTaskArtifact.cs`
- `native/SkaiDesktop.Core/Services/AgentTaskArtifactStore.cs`
- `native/SkaiDesktop.Core/Services/AgentTaskExecutor.cs`
- `native/SkaiDesktop.Core.Tests/AgentTaskArtifactTests.cs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- Full `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including handler lease injection, branch persistence, retained lease state, and explicit clean release.
- Task artifact validation accepts legacy no-lease records and rejects incomplete lease metadata.
- Cancellation/failure paths retain the lease for review rather than force-removing it.

Privacy/security/licensing: lease metadata is local bounded path/branch data;
no credentials, network requests, model routing, or third-party dependencies
were added. The handler remains explicitly injected and untrusted side effects
are not authorized by the executor.

Risk and rollback: remove the worktree fields, executor overload, release
method, and focused tests; the prior injected-handler executor remains usable.
Any retained worktree must be explicitly reviewed/released before rollback.

Remaining: connect this lease flow to the native Coding Agent UI/session
creation, add process and parallel-session isolation, and complete live
Windows/Linux acceptance.

## Slice 217 — Surface retained worktrees in Activity

Status: `implemented_native_ui_build_audit_verified`

Requirement alignment: durable isolation is not useful if a user cannot see or
recover the retained target. Activity & tasks now shows when an agent artifact
has a retained worktree, includes its repository/path/branch in task details,
and offers an explicit `Release clean worktree` action. The action delegates to
the Core clean-only release boundary; dirty worktrees remain visible and are
never force-deleted. No task is claimed, executed, or dispatched by this UI.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `npm run audit:native-ui` — passed with 293 contracts, including retained
  worktree status, detail, and release-action contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- The release action is guarded by the Core coordinator and reports refusal
  instead of deleting a dirty target.

Privacy/security/licensing: the UI displays local repository/worktree paths
already persisted on the task artifact; no credentials, network calls, model
routing, or new dependencies were added.

Risk and rollback: remove the Activity lease display/release action and the
executor field wiring; Core task execution remains available to non-UI callers.

Remaining: connect lease acquisition to the full Coding Agent session flow,
add process/parallel isolation, and complete live Windows/Linux acceptance.

## Slice 218 — Tokenize project navigation and browser status

Status: `implemented_native_ui_build_audit_verified`

Requirement alignment: project selection and bounded file browsing are the
primary Coding Agent entry points. Their code-built rows still used historical
palette colors and numeric font sizes, so active project state and browser
status could drift from the shared theme. This slice moves active/inactive
project rows, project actions, empty-state copy, and bounded file-browser
counts to semantic brushes and type-scale resources.

Changed files:

- `native/ServerRock.Native/MainPage.Projects.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `npm run audit:native-ui` — passed with 296 contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: presentation-only token references; project roots,
file filtering, bounded previews, and workspace trust behavior are unchanged.

Risk and rollback: restore the previous project-row presentation values and
remove the focused audit contracts. No project data or file contents change.

Remaining: the project explorer is still a bounded flat preview list rather
than a virtualized hierarchical tree; cross-platform and live first-project
acceptance remain open.

## Slice 231 — Verify the current Windows Release artifact smoke

Status: `verified_windows_release_artifact_startup_and_window_smoke`

Requirement alignment: release readiness requires evidence from the exact
published artifact, not only a Debug build. The current self-contained
`net10.0-desktop` `win-x64` Release publish completed, and the actual
`ServerRock.Native.exe` passed bounded startup smoke and top-level-window smoke
for eight seconds. The initial helper invocation against `createdump.exe`
exited immediately; it was a probe-selection error, not treated as product
evidence. Generated `dist/` output remains ignored and was not committed.

Evidence:

- `npm run check:release-config` — passed.
- `npm run audit:release-workflow` — passed.
- `npm run audit:native-ui` — passed with 322 contracts.
- `npm run check:hygiene` — passed with 754 tracked files.
- `npm run build:native` — passed, producing the self-contained
  `dist/native/win-x64/ServerRock.Native.exe` artifact.
- `NATIVE_SMOKE_SECONDS=8 npm run smoke:native:artifact --
  dist/native/win-x64/ServerRock.Native.exe` — passed startup smoke.
- `NATIVE_SMOKE_SECONDS=8 NATIVE_SMOKE_REQUIRE_WINDOW=1 npm run
  smoke:native:artifact -- dist/native/win-x64/ServerRock.Native.exe` — passed
  startup and window smoke.

Privacy/security/licensing: artifact startup only; no provider, credential,
project, or user profile data was used. Release workflow remains fail-closed
for missing signing/clean-machine evidence; this is not a signed-release claim.

Risk and rollback: no tracked product files changed; remove this evidence note
if the artifact path or smoke methodology changes.

Remaining: signed installer/feed evidence, clean-machine install/upgrade/
uninstall/rollback, full workflow acceptance, Linux distro artifacts, and
cross-platform accessibility remain open.

## Slice 230 — Surface the project-browser safety boundary

Status: `implemented_native_browser_boundary_accessibility_verified`

Requirement alignment: the new bounded project browser correctly excludes
protected names, dependency/build directories, and linked paths, but the UI
did not explain that boundary. The Coding Agent file header now identifies the
surface as a bounded safe view and exposes an accessible help description with
the 128-entry limit and exclusion categories. No file access is widened and
provider context behavior is unchanged.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/WorkspaceFileContext.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including the browser-boundary description assertion.
- `node scripts/audit-native-ui.cjs` — passed with 322 contracts.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: accessible explanatory text only. Existing
explicit folder selection, protected-path exclusions, bounded enumeration, and
local-only behavior are unchanged. No dependency, network, or license surface
was added.

Risk and rollback: restore the prior header text and remove the boundary
constant/test/audit contract. No project files or user data change.

Remaining: full repository-tree loading, ignore-state presentation beyond the
bounded explanation, richer folder actions, binary/PDF/image preview
integration, and live cross-platform acceptance remain open.

## Slice 229 — Separate project browsing from model-context sampling

Status: `implemented_core_bounded_browser_tree_build_test_verified`

Requirement alignment: the Coding Agent project explorer previously reused
`ReadSelectedFiles`, so navigation was limited to the 16-file/48k-character
model-context sample. `WorkspaceFileContext.EnumerateTextFiles` now exposes a
separate content-free, bounded navigation enumeration (128 text files) with
the same protected-file, dependency-directory, reparse-point, and access
failure boundaries. The virtualized folder disclosure UI uses this browser
surface; chat/provider context retains its original stricter content sample.

Changed files:

- `native/SkaiDesktop.Core/Infrastructure/WorkspaceFileContext.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including bounded browser enumeration and protected-path checks.
- `node scripts/audit-native-ui.cjs` — passed with 321 contracts.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: enumeration reads metadata only and does not send
file contents to providers. Existing explicit project-folder selection,
secret/dependency exclusions, symlink refusal, and bounded context behavior
remain unchanged. No dependency or network path was added.

Risk and rollback: restore the explorer's `ReadSelectedFiles` call and remove
the new Core enumeration/tests. The model-context path remains unchanged.

Remaining: full repository-tree loading, ignore-state presentation, richer
folder actions, binary/PDF/image preview integration, and live cross-platform
acceptance remain open.

## Slice 228 — Verify the staged desktop-host migration checkpoint

Status: `verified_agent_host_renderer_runtime_checkpoint_not_parity_complete`

Requirement alignment: the live branch now contains the next staged desktop
surface around the .NET agent host. The Electron/React renderer exposes Chat,
Models, Settings, Projects, and Agent Workbench navigation; local endpoint/model
preferences; model discovery through the host; and chat completion through the
existing OpenAI-compatible Core provider. The host advertises `runtime.health`,
`models.list`, and `chat.complete` over its versioned JSON-lines boundary.

This verifies a migration seam, not a replacement of the native Uno product.
The canonical handoff still requires native-first architecture and does not
permit claiming replacement parity from these checks.

Evidence for commit `b208026`:

- `npm run agent-host:build` — passed with 0 warnings and 0 errors.
- `npm run desktop:typecheck` — passed.
- `npm run desktop:build` — passed; Vite production bundle generated.
- Live stdio smoke request `runtime.health` — returned protocol v1, status
  `ready`, and capabilities `runtime.health`, `models.list`, `chat.complete`.

Privacy/security/licensing: this is verification of the existing local IPC
seam and renderer changes. The host remains a local child process, the
renderer uses the narrow bridge, and no new credentials or external service
was exercised by the smoke request.

Risk and rollback: revert `b208026` only with owner coordination; it is a
separate concurrent developer commit. The native app remains the fallback and
canonical product surface.

Remaining: real renderer model/chat journeys, settings/project persistence
acceptance, process failure/restart handling, accessibility, packaging, and
full feature parity remain open.

## Slice 227 — Extract deterministic startup recovery

Status: `implemented_core_startup_recovery_boundary_verified`

Requirement alignment: `MainPage.Lifecycle` directly loaded several local stores
and reconciled operations, mixing persistence recovery, warning construction,
and UI state assignment. `StartupRecoveryService` now owns the bounded queue,
conversation, project, undo, and interrupted-operation recovery path and
returns a typed result with availability flags and warnings. The page still
owns settings/drafts, companion loading, legacy-project compatibility, and
rendering. No provider, tool, shell, network, or UI work is started by the
service.

Changed files:

- `native/SkaiDesktop.Core/Services/StartupRecoveryService.cs`
- `native/SkaiDesktop.Core.Tests/StartupRecoveryTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPageServices.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `scripts/audit-native-ui.cjs`
- `docs/ARCHITECTURE.md`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including empty-profile and malformed-history recovery checks.
- `node scripts/audit-native-ui.cjs` — passed with 320 contracts.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: local recovery only. Existing files remain in place
on malformed input, queue/history/project/undo bounds remain enforced by their
stores, and interrupted operations are reconciled without replaying effects.
No new dependency, network path, credential, or license surface was added.

Risk and rollback: restore the prior lifecycle `try` blocks and remove the
recovery service/test. Existing JSON formats and profile paths are unchanged.

Remaining: application-service/view-model extraction, explicit DI lifetime
scopes, host/session orchestration, and live cross-platform acceptance remain
open.

## Slice 226 — Move command-palette metadata into Core

Status: `implemented_core_catalog_native_execution_boundary_verified`

Requirement alignment: command-palette action metadata and filtering were
embedded in `MainPage`, mixing UI-independent search data with dialog and page
execution callbacks. `CommandPaletteCatalog` now owns stable action IDs, titles,
keywords, and deterministic search in `SkaiDesktop.Core`; the native shell owns
only the ID-to-callback mapping and focus/dialog lifecycle. Core tests cover
unique IDs, keyword matching, and empty-result behavior. Existing command
labels, actions, keyboard behavior, and privacy boundaries are preserved.

Changed files:

- `native/SkaiDesktop.Core/Services/CommandPaletteCatalog.cs`
- `native/SkaiDesktop.Core.Tests/CatalogTests.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `scripts/audit-native-ui.cjs`
- `docs/ARCHITECTURE.md`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed, including command-palette catalog checks.
- `node scripts/audit-native-ui.cjs` — passed with 319 contracts.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: metadata and local filtering moved to Core only;
execution remains explicit native UI behavior. No network, credential,
provider, or data-storage behavior changed.

Risk and rollback: restore the inline command descriptors and remove the Core
catalog/tests. No persisted user data changes.

Remaining: application-service/view-model extraction, explicit DI lifetime
scopes, host/session orchestration, and live cross-platform acceptance remain
open.

## Slice 225 — Centralize the native page composition root

Status: `implemented_native_architecture_composition_root_build_verified`

Requirement alignment: the native architecture target calls for incremental
extraction from the MainPage hotspot while preserving the Uno/XAML shell and
Core boundary. `MainPageServices` now owns construction of the page's existing
Core services and stores, including the same `SRAI_*_PATH` environment seams.
The page receives one typed bundle at its lifecycle boundary, then keeps its
existing fields and behavior. This is a composition-root seam, not a framework
rewrite, data migration, or behavioral change; later application-service and
view-model extraction can proceed without duplicating stores or profile paths.

Changed files:

- `native/ServerRock.Native/MainPageServices.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `scripts/audit-native-ui.cjs`
- `docs/ARCHITECTURE.md`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj`
  — passed the Core checks.
- `node scripts/audit-native-ui.cjs` — passed with 317 contracts, including
  the composition-root and lifecycle assignment contracts.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: construction-only refactor. Existing local JSON
stores, session-only provider key behavior, project/file boundaries, and
approval gates are unchanged. No dependency, network path, credential, or
license surface was added.

Risk and rollback: restore the inline field initializers and remove
`MainPageServices`; no persisted data or runtime protocol changes are involved.

Remaining: application-service/view-model extraction, explicit DI lifetime
scopes, host/session orchestration, and live cross-platform acceptance remain
open. The Electron/agent-host prototype is not promoted to product runtime;
the canonical architecture remains native Uno/.NET as required by the handoff.

## Slice 224 — Tokenize Activity and agent-task typography

Status: `implemented_native_ui_token_audit_verified`

Requirement alignment: the Activity & tasks surface still used raw sizes for
agent-task headers/details, scheduled-task outcomes, pending approvals,
operation metadata, workflow labels, effect timelines, and planning guidance.
These now use the shared section, subtitle, and caption type-scale resources.
Task planning, approval countdowns, reconciliation actions, worktree release,
and workflow review behavior are unchanged.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `node scripts/audit-native-ui.cjs` — passed with 315 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: typography-only substitutions. Activity remains
local and redacted, approval and execution boundaries are unchanged, and no
new dependency or external data path is introduced.

Risk and rollback: restore the previous raw font sizes and remove the three
focused audit contracts. No task, workflow, approval, or user data changes.

Remaining: broader code-built legacy typography/color migration and live
cross-preset visual acceptance remain open.

## Slice 223 — Tokenize skills dialog surfaces

Status: `implemented_native_ui_token_audit_verified`

Requirement alignment: the Skills dialog still used raw font sizes and a
hard-coded muted brush for installed-skill details, status, and its section
label. These now use shared semantic typography and ink tokens, keeping the
dialog legible across appearance presets while preserving explicit enablement,
preview, rollback, and Markdown-only safety behavior.

Changed files:

- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `node scripts/audit-native-ui.cjs` — passed with 312 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: visual token substitutions only. Skill packages
remain local, Markdown-only, disabled by default, explicitly enabled, and
rollback-capable; no execution or network behavior changes.

Risk and rollback: restore the three previous local assignments and remove
the three focused audit contracts. No installed skill or user data changes.

Remaining: broader code-built legacy typography/color migration and live
cross-preset visual acceptance remain open.

## Slice 222 — Tokenize model-library status surfaces

Status: `implemented_native_ui_token_audit_verified`

Requirement alignment: model comparison and library status text still used
raw font sizes and one light-only muted brush. The comparison scope note,
pagination status, and endpoint/status text now use shared semantic typography
and `MutedInkBrush`, keeping the model surfaces readable across the selected
appearance presets without changing provider routing, remote consent, or
model-selection behavior.

Changed files:

- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `node scripts/audit-native-ui.cjs` — passed with 309 contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: visual token substitutions only. Model endpoints,
remote-consent gates, and local-only policy are unchanged; no new dependency
or outbound path is introduced.

Risk and rollback: restore the three previous local font/color assignments and
remove the three focused audit contracts. No model or user data changes.

Remaining: broader code-built legacy typography/color migration and live
cross-preset visual acceptance remain open.

## Slice 221 — Tokenize remaining small code-built media/review surfaces

Status: `implemented_native_ui_token_audit_verified`

Requirement alignment: three compact native surfaces still carried raw font
sizes or light-only brushes, which made them drift from the selected light,
dark, Deep Sea, and Deep Space presentation. The Code Review note, Image
Studio status/preview cards, and approved-memory disclosure now use shared
semantic typography and surface/line brushes. Behavior, local-only routing,
image generation controls, and memory consent semantics are unchanged.

Changed files:

- `native/ServerRock.Native/MainPage.CodeReview.cs`
- `native/ServerRock.Native/MainPage.ImageStudio.cs`
- `native/ServerRock.Native/MainPage.Memory.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `node scripts/audit-native-ui.cjs` — passed with 306 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: visual token substitutions only. Image generation
remains loopback-only and memory injection remains explicit, local, and
reviewable. No new dependency, network path, or license surface is added.

Risk and rollback: restore the previous local font/color assignments and
remove the five focused audit contracts. No user data or provider behavior
changes.

Remaining: broader code-built legacy typography/color migration and live
cross-preset visual acceptance remain open.

## Slice 220 — Add bounded project-folder disclosure

Status: `implemented_native_ui_hierarchy_build_audit_verified`

Requirement alignment: the project explorer now presents folder disclosure
rows above the bounded sampled file list. Top-level folders start expanded;
each folder row has an accessible expand/collapse action, stable local state,
semantic styling, and indentation. Files remain safe preview buttons, and the
existing filtering, protected-file exclusions, editor focus return, and
ListView collection boundary are preserved.

This is intentionally a bounded hierarchy over the existing
`WorkspaceFileContext.ReadSelectedFiles` sample. It does not claim a complete
repository tree, ignore-state controls, or live cross-platform acceptance.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `node scripts/audit-native-ui.cjs` — passed with 301 contracts, including
  folder disclosure state, hierarchy construction, and accessible toggles.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore`
  — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: presentation and local expansion-state changes
only; file filtering, protected-file exclusions, bounded previews, and project
trust scopes are unchanged. No new dependency or external data path is added.

Risk and rollback: remove the folder-row rendering and restore the previous
flat file-row loop. No project data or file contents change.

Remaining: full repository-tree loading, ignore-state presentation, richer
folder actions, cross-platform acceptance, and live first-project journey
evidence remain open.

## Slice 219 — Virtualize the project file list

Status: `implemented_native_ui_build_audit_verified`

Requirement alignment: the project file surface previously used a `StackPanel`,
which materialized every bounded preview row and had no explicit collection
accessibility boundary. It now uses a bounded WinUI `ListView` with keyboard
navigation, its own scroll boundary, and the existing safe preview buttons.
The current path filtering, protected-file exclusions, and editor focus return
are preserved. This is the virtualization portion of the explorer requirement;
it does not claim a hierarchical folder disclosure tree yet.

Changed files:

- `native/ServerRock.Native/ProjectNavigationView.xaml`
- `native/ServerRock.Native/ProjectNavigationView.xaml.cs`
- `native/ServerRock.Native/SidebarView.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/HANDOFF_BACKLOG.md`

Evidence:

- `npm run audit:native-ui` — passed with 297 contracts, including the
  virtualized list and accessible project-file boundary.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj --no-restore` — passed with 0 warnings and 0 errors for desktop and Windows targets.
- `git diff --check` — passed.

Privacy/security/licensing: collection-control and layout changes only; safe
file enumeration, secret filtering, bounded previews, and project trust scopes
are unchanged.

Risk and rollback: restore the StackPanel property types and child collection
calls. No project data or file contents change.

Remaining: add hierarchical folder disclosure/ignore-state presentation,
cross-platform acceptance, and live first-project journey evidence.

## Slice 209 — Add durable injected agent-task execution lifecycle

Status: `implemented_core_verified_executor_boundary`

Requirement alignment: the handoff requires durable agent execution state,
claiming, completion, failure, cancellation, and recovery visibility. The
existing task store persisted plans and supported a claim transition, but had
no reusable executor boundary. This slice adds `AgentTaskExecutor`, which
claims one dependency-ready queued task, invokes an explicitly injected
handler, and persists the handler's completed, failed, or cancelled outcome.

The executor deliberately does not choose a model, invoke MCP/tools, run a
shell, access files, or dispatch parallel work. Those effects remain explicit
caller responsibilities with their own permission and approval surfaces.

Changed files:

- `native/SkaiDesktop.Core/Services/AgentTaskExecutor.cs`
- `native/SkaiDesktop.Core.Tests/AgentTaskArtifactTests.cs`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Full native Core suite — passed, including durable injected-handler
  completion, failure, and cancellation checks.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- `npm run check:syntax`, `npm run check:hygiene`, and
  `npm run check:release-config` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the executor adds no new side effects or
dependencies. Handler summaries and bounded artifact labels go through the
existing task-store validation and local atomic persistence.

Risk and rollback: remove the executor and three focused test calls; the
existing plan/claim store remains intact. A handler exception is converted to
a durable failed state so a crashed caller cannot leave an ordinary failure
silently looking queued.

Remaining: real model/tool dispatch, parallel specialist isolation, background
execution, worktree/container isolation, approval orchestration, and packaged
runtime acceptance remain open.

## Slice 208 — Surface Git conflicts with safe recovery guidance

Status: `implemented_build_and_audit_verified`

Requirement alignment: Git workspaces need conflict recovery that preserves
local and remote work. The existing push preflight already refuses to push
when the remote branch is ahead; this slice adds explicit handling for
unmerged porcelain statuses (`UU`, `AA`, `DD`, and related `U` states). The
GitHub review dialog labels those files as conflicts, prevents selecting them
for staging, disables commit/push while conflicts remain, and directs the user
to resolve locally and reopen review. No automatic merge, reset, checkout, or
destructive conflict resolution is performed.

Changed files:

- `native/SkaiDesktop.Core/Services/GitHubWorkspaceService.cs`
- `native/SkaiDesktop.Core.Tests/GitHubTests.cs`
- `native/ServerRock.Native/MainPage.GitHub.Changes.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with 278 contracts, including conflict
  notice and non-staging guidance.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- Full native Core suite — passed, including `GitHub workspace identifies
  unmerged conflict paths for recovery`.
- `npm run check:hygiene` and `npm run check:release-config` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: local Git status classification and UI guidance
only; no network operation, credential handling, file mutation, or dependency
was added.

Risk and rollback: remove `IsConflictStatus`/`HasConflicts`, the focused test,
and the dialog notice/guards. Existing conservative push preflight remains.

Remaining: an interactive conflict editor/three-way merge review, worktree
isolation, and packaged live Git acceptance remain open. Those require a
separate explicit design and stronger runtime evidence.

## Slice 207 — Add bounded persistent project-file tabs

Status: `implemented_build_and_visual_smoke_verified`

Requirement alignment: the project workbench needs persistent tabs for files
that are being reviewed. This slice adds an eight-entry, relative-path tab
strip to the Coding Agent workspace bar. Tabs persist in local settings for
the matching project, can be reopened in the existing guarded editor, and can
be closed without touching the files. Stale or moved files are omitted after
the existing workspace path/hash validation. The current editor remains a
reviewable modal surface; simultaneous multi-pane editing and drag reorder are
not claimed.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with 276 contracts, including named tab,
  close, persistence, and rendering contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- Full native Core suite — passed, including `project file tabs survive settings save and reload`.
- `npm run check:hygiene` and `npm run check:release-config` — passed.
- Native Windows visual smoke rendered the updated shell without visible
  layout regression; the tab strip is correctly hidden outside Coding Agent
  mode. The compact 560px and live tab-click journey remain unverified.
- `git diff --check` — passed.

Privacy/security/licensing: only bounded project IDs and relative file paths
are stored locally. Tab metadata contains no file contents, credentials,
network data, or new dependencies.

Risk and rollback: remove the tab strip, tab event handlers, bounded settings
fields, normalization, audit contracts, and focused test. Existing guarded
file reads, editor saves, and undo history remain intact.

Remaining: simultaneous multi-file editor panes, drag/reorder semantics,
conflict recovery, worktree isolation, and packaged live workbench acceptance
remain open.

## Slice 206 — Persist and resume the last reviewed project file

Status: `implemented_build_and_audit_verified`

Requirement alignment: the project workbench needs continuity across app
restarts. This bounded slice persists the last reviewed project file's project
ID and relative path in the existing local settings store, validates that the
file still belongs to the selected project, and exposes a named `Resume file`
action in the Coding Agent workspace bar. It does not claim multi-file tabs or
automatic file opening; those remain separate requirements.

Changed files:

- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

The persisted path is bounded and normalized; the resume action only appears
for the matching active project and a file that passes the existing guarded
workspace hash/path checks. Missing or moved files simply hide the action and
leave saved settings intact.

Evidence:

- `npm run audit:native-ui` — passed with 273 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- Full native Core suite — passed, including `last project file context survives settings save and reload`.
- `git diff --check` — passed.

Privacy/security/licensing: only a bounded project ID and relative path are
stored in the existing local settings file; file contents, credentials, and
network data are not added to settings.

Risk and rollback: remove the resume button, event wiring, two settings fields,
normalization, and focused test to roll back. Existing file editing and undo
behavior remains independent.

Remaining: true persistent multi-file tabs, tab close/reorder state, conflict
recovery, worktree isolation, and packaged live workbench acceptance remain
open.

## Slice 205 — Tokenize project workbench code-built surfaces

Status: `implemented_build_and_audit_verified`

Requirement alignment: the project workbench must keep file browsing, quick
open, reviewed edits, undo history, and terminal work readable across the
shared light/dark/native theme system. The XAML shell had already been moved to
semantic tokens, but the code-built project file and terminal surfaces still
used literal colors and font sizes. This slice routes those controls through
the existing `UiTheme` brushes, font-scale resources, and code font resource.

Changed files:

- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `native/ServerRock.Native/MainPage.Terminal.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

The targeted audit now rejects literal `ColorHelper.FromArgb`, numeric
`FontSize`, and `new SolidColorBrush` styling in the project file/terminal
partial classes while leaving intentional appearance-backdrop and other
feature-specific rendering contracts out of scope for later slices.

Evidence:

- `npm run audit:native-ui` — passed with 270 contracts.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- Full native Core suite — passed (`Native core checks passed.`).
- `npm run check:hygiene` — passed (748 tracked files).
- `npm run check:release-config` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: local visual styling only; no provider routing,
filesystem permissions, credentials, network behavior, or dependencies changed.

Risk and rollback: remove the semantic substitutions and targeted audit guard
to roll back; the underlying project editor, undo, and terminal behavior is
unchanged. The semantic brushes intentionally inherit the current palette, so
future theme work must preserve terminal contrast.

Remaining: persistent tabs, conflict recovery, worktree isolation, live
packaged project-workbench acceptance, and broader code-built UI tokenization
remain open.

## Slice 204 — Extend native UI responsive regression contracts

Status: `implemented_audit_verified_live_resize_unverified`

Requirement alignment: the handoff requires usable narrow-window navigation,
keyboard access, and a 560px acceptance pass. The native shell already has a
compact drawer, stretch scrim, short-window composer sizing, Escape close, and
focus restoration. This slice makes those seams explicit in the static UI
audit so later XAML/C# edits cannot remove them silently.

Changed files:

- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with 270 contracts, including the compact
  drawer scrim, breakpoint, short-window sizing, and Escape-close contracts.
- Native Windows accessibility inspection showed the initial composer focus,
  named navigation controls, named drawer actions, and the compact-navigation
  keyboard path in source. The available resize interaction did not produce a
  reliable 560px window, so live 560px visual and modal focus acceptance is not
  claimed.
- `git diff --check` — passed.

Privacy/security/licensing: static local UI inspection only; no network,
credentials, provider routing, user data, or dependencies changed.

Risk and rollback: remove the four responsive contracts and this ledger entry
to roll back; the runtime UI is unchanged.

Remaining: execute the 560px and Popup focus journey with a supported native
resize/accessibility harness and record the packaged-artifact result.

## Slice 203 — Add fail-closed release workflow contract audit

Status: `implemented_script_verified`

Requirement alignment: release acceptance requires tagged publishing to remain
fail-closed, signing gates to be explicit, and checksum/signature evidence to
be present before a GitHub Release can be published. The existing workflow had
these runtime checks, but no repository-local guard detected accidental
removal during later edits. This slice adds a bounded static audit and exposes
it as `npm run audit:release-workflow`.

Changed files:

- `scripts/audit-release-workflow.cjs`
- `package.json`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `docs/HANDOFF_BACKLOG.md`

The audit checks the tracked workflow for the tag trigger, diagnostic manual
dispatch, tag-only release job, literal `true` gates for migration/signing/
packaged smoke, Windows Authenticode inputs and evidence, Linux GPG inputs and
detached-signature evidence, non-empty signature enforcement, checksum
generation, and release write permission. It does not inspect or require
secrets on a developer workstation and does not publish anything.

Evidence:

- `npm run audit:release-workflow` — passed with the tag-only,
  gated, signed-evidence contract.
- `node --check scripts/audit-release-workflow.cjs` — passed.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: local static inspection only; no credentials,
network calls, release tags, artifacts, or third-party code were introduced.

Risk and rollback: the script can fail CI if the workflow contract is changed
without updating the guard. Roll back by removing the script and package
entry; the workflow itself remains unchanged.

Remaining: actual signing material, clean-machine lifecycle runs, and Linux
distro acceptance still require external release infrastructure and owner
decisions documented in the backlog.

## Slice 202 — Complete native view typography tokenization

Status: `implemented_build_and_audit_verified`

Completed the second pass of the native UI audit by replacing remaining literal
`FontSize` values in view XAML with the shared type scale. Composer actions,
conversation navigation tools, header controls, companion actions, and split
conversation headings now inherit the same typography contract as the rest of
the shell.

Changed files:

- `native/ServerRock.Native/ComposerInputView.xaml`
- `native/ServerRock.Native/ConversationNavigationView.xaml`
- `native/ServerRock.Native/MainPageHeader.xaml`
- `native/ServerRock.Native/SidebarView.xaml`
- `native/ServerRock.Native/SplitConversationView.xaml`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed with 265 contracts, including the new
  no-literal-view-font-size guard.
- `npm run check:syntax` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Privacy/security/licensing: typography-only view changes; no provider,
filesystem, credential, or network behavior changed.

Rollback: restore the five view files and remove the typography audit guard;
runtime behavior and persisted data are unaffected.

Remaining: live High Contrast and compact-window verification, plus the
broader handoff UI gaps and release-artifact acceptance.

## Slice 201 — Refactor native shell views onto semantic design tokens

Status: `implemented_build_audit_and_visual_smoke_verified`

Audited the high-traffic native shell surfaces against `DESIGN.md` and moved
the header badges, Coding Agent workspace bar, composer status banner, stop
action, privacy footnote, and empty-state hero onto shared semantic brushes and
type-scale resources. This keeps accent/theme changes coherent and prevents
view-level hex colors from silently bypassing the design system.

Changed files:

- `native/ServerRock.Native/App.xaml`
- `native/ServerRock.Native/MainPageHeader.xaml`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml`
- `native/ServerRock.Native/ComposerInputView.xaml`
- `native/ServerRock.Native/ConversationSurface.xaml`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed with 264 contracts, including the new
  no-hard-coded-view-color guard.
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed for 747 tracked files.
- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed (`Native core checks passed.`).
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- Visual smoke: the native shell rendered at the desktop viewport without an
  observed clipping or overlap failure.

Privacy/security/licensing: visual resources only; no provider, filesystem,
credential, or network behavior changed.

Rollback: restore the five view/token files and remove the audit guard; runtime
behavior and persisted data are unaffected.

Remaining: migrate the remaining hard-coded type sizes to the token scale,
verify High Contrast and compact-window states live, and continue the broader
handoff UI gaps (persistent tabs, inspector, and full project workbench).

## Slice 200 — Quick open for bounded project files

Status: `implemented_native_build_audit_verified_live_ui_blocked_by_picker_host`

Coding Agent users can now press `Ctrl+P` or choose Quick open project file from
the command palette. The dialog searches the existing bounded,
secret-filtered text-file enumeration and opens the selected path through the
same reviewable editor used by the project sidebar. It does not add roots,
read excluded files, or bypass approval before a write.

Changed files:

- `native/ServerRock.Native/MainPage.CodingAgent.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed (`Native core checks passed.`).
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 263 contracts after updating the
  focus-restoration contract for the shared editor helper.
- Live Windows attempt: the native build launched and Coding Agent mode opened;
  the first-project flow reached the Windows `Select Folder` picker, but its
  `PickerHost.exe` window was not exposed as a targetable window by the
  computer-use runtime, so keyboard/search/open acceptance could not be
  completed without unsafe coordinate guessing.

Privacy/security/licensing: the feature reuses `WorkspaceFileContext` bounds,
secret exclusions and the existing local editor; no provider or external
service is contacted.

Rollback: remove the command/shortcut and quick-open method; sidebar file
opening and workspace permissions remain unchanged.

Remaining: live Windows keyboard/search/open acceptance after a targetable
folder-picker path is available, Linux parity and a full virtualized project
explorer remain open.

## Slice 190 — Local reusable Personas (Gems)

Status: `implemented_core_ui_build_verified`

Added the simplest useful slice of the backlog item for saved Gems/personas.
Personas are local, bounded role instructions that users can create, edit,
activate, deactivate, and delete from the command palette or More workspace
options. An active persona is added to future non-documents-only provider
context with an explicit user-authored priority label; existing conversations
are not rewritten and documents-only mode excludes the persona by design.

Changed files:

- `native/SkaiDesktop.Core/Models/SavedPersona.cs`
- `native/SkaiDesktop.Core/Models/AppSettings.cs`
- `native/SkaiDesktop.Core/Infrastructure/JsonSettingsStore.cs`
- `native/SkaiDesktop.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.Personas.cs`
- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`
- `docs/FUTURE_FEATURES.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Core persistence tests passed, including persona round-trip and active-ID recovery.
- UI audit contracts cover command discovery, saved-list labeling, and instructions labeling.
- Persona input is bounded to 24 entries, 80-character names, and 4,000-character instructions.
- Persona data is stored in the existing local settings file; no new network call,
  credential, dependency, or automatic provider action was added.

Privacy/security/licensing: persona text is user-authored local data. It is
sent only when the user sends a future request while a persona is active, is
excluded from documents-only requests, and is labeled as lower priority than
system and safety instructions. No third-party content or package was added.

Rollback: remove the Personas command/action and delete the saved-persona
settings fields; existing chats, pets, provider credentials, and project data
are unaffected.

Remaining: bundled persona tools/knowledge, sharing, sync, import/export, and
team administration remain open backlog work.

## Slice 191 — Six-frame Percy state animations

Status: `implemented_asset_runtime_build_verified`

Expanded Percy from one static image per state into six aligned frames for idle,
thinking, coding, malicious-review, stuck, success, and error, plus a six-frame
walk loop derived from the approved walk source. The native runtime preloads
Percy's state frames and cycles them at 8 FPS while keeping the existing reduced-
motion, cross-fade, wander, and transform safeguards. Other companions and local
custom sprites retain their existing behavior.

Changed files:

- `assets/pets/make-percy-frames.ps1`
- `assets/pets/percy-animation-sprite-sheet.png`
- `assets/pets/percy-*-1.png` through `percy-*-6.png`
- `assets/pets/percy-*-frames.png`
- `assets/pets/percy-walk-1.png` through `percy-walk-6.png`
- `assets/pets/percy-walk-frames.png`
- `assets/pets/README.md`
- `native/ServerRock.Native/MainPage.Companion.cs`
- `native/ServerRock.Native/MainPage.CompanionAnimation.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- The generated production atlas is 1,536×2,048 with eight rows of six 256×256 frames.
- Every Percy state strip and walk strip is 1,536×256 and transparent.
- `npm run audit:native-ui` includes the six-frame runtime contract.
- Existing companions, custom sprite paths, reduced-motion behavior, and state transitions remain unchanged.

Privacy/security/licensing: only locally packaged/generated artwork and local
frame selection changed. No network, credential, provider, or user-data behavior
changed. The generated artwork is original project content and does not copy
Firefox/Mozilla assets.

Rollback: remove the six-frame Percy assets and restore the previous single-frame
Percy URI selection and preload loop. Existing companion preferences remain valid.

Remaining: add richer hand-authored frame timing and an in-app animation preview
when the broader pet customization backlog is revisited.

## Slice 192 — Audit and standardize all pet animation strips

Status: `implemented_all_pet_assets_runtime_build_verified`

The pet audit found that only Percy had production six-frame strips and that
Percy's old generated state frames carried opaque edge pixels. Replaced the
animation layer for all six built-in pets with deterministic six-frame strips
for all seven states plus a six-frame walk loop. The generator clears a shared
transparent edge band, keeps a 256×256 cell contract, and emits a per-pet review
atlas. The runtime now preloads and cycles the six-frame strips for every built-in
pet, including walking, while custom sprites and reduced-motion behavior remain
unchanged.

Changed files:

- `assets/pets/make-pet-frames.ps1`
- `assets/pets/<pet>-<state>-1.png` through `<pet>-<state>-6.png` for all six pets
- `assets/pets/<pet>-<state>-frames.png` for all seven states and all six pets
- `assets/pets/<pet>-walk-1.png` through `<pet>-walk-6.png` for all six pets
- `assets/pets/<pet>-walk-frames.png` and `<pet>-animation-sprite-sheet.png`
- `assets/pets/README.md`
- `native/ServerRock.Native/MainPage.Companion.cs`
- `native/ServerRock.Native/MainPage.CompanionAnimation.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- Every built-in pet has 7×6 state frames plus 6 walk frames.
- Every production frame is 256×256; every strip is 1,536×256; every atlas is 1,536×2,048.
- Edge-alpha audit passes after the shared six-pixel cleanup band.
- Native UI audit now checks all 252 frame files and six per-pet atlases.
- Desktop and Windows builds verify that the glob-packaged assets resolve through the native app.

Privacy/security/licensing: local artwork and frame selection only; no network,
provider, credential, or user-data behavior changed. The art remains project-owned
generated/derived content.

Rollback: restore the previous Percy-only animation selection and remove the new
generated strips. Existing companion IDs, state preferences, and custom sprite
paths remain compatible.

Remaining: a live visual smoke run across each pet/state and hand-authored motion
timing remain worthwhile follow-up acceptance work.

## Slice 187 — Validate release metadata consistency

Status: `implemented_script_gate_verified`

The release configuration check now reads the native release manifest, validates
its bounded package fields and stable lowercase identifiers, and cross-checks
its display name against `native/Directory.Build.props`. It still preserves the
existing opt-in signing-material and environment-version checks. A future
rebrand now fails the gate if packaging metadata is updated in only one source.

Changed files:

- `scripts/validate-release-config.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run check:syntax` — passed.
- `npm run check:release-config` — passed with package metadata validation.
- `npm run check:hygiene` — passed.
- `npm run audit:native-ui` — passed with 236 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: this is a local fail-closed validation gate. It
does not read credentials, contact providers, alter package files, or change
runtime routing. No dependency or third-party code was added.

Rollback: restore the previous release validator; existing manifest and build
behavior remain unchanged.

Remaining: signed installers, release-feed integration, clean-machine install,
upgrade, uninstall, rollback, localization, and cross-platform release
evidence remain open.

## Slice 188 — Rebrand the product to Skai Desktop

Status: `implemented_rebrand_build_tests_audit_verified`

The user-facing product identity is now `Skai Desktop`, with `Skai` used for
short assistant labels and local export names. Native package metadata, the
release manifest, the secondary desktop identity source, workflow artifact
labels, launch errors, README, and current product documentation use the new
name. Stable identifiers remain intentionally unchanged: the local profile
path, GitHub credential entropy and keyring label, technical User-Agent product
token, repository namespaces, install folder, and launcher ID are migration
contracts rather than display branding.

Changed files:

- `native/SkaiDesktop.Core/Models/ProductIdentity.cs`
- `native/SkaiDesktop.Core.Tests/ProductIdentityTests.cs`
- `native/Directory.Build.props`
- `packaging/native/release-manifest.json`
- `apps/desktop/shared/productIdentity.ts`
- `scripts/open-native.cjs`
- `.github/workflows/native-artifacts.yml`
- `README.md`
- current product and research documentation under `docs/`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including the Skai Desktop identity and stable-identifier contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run check:syntax` — passed.
- `npm run check:release-config` — passed, including manifest/MSBuild display-name consistency.
- `npm run audit:native-ui` — passed with 236 contracts.
- `npm run audit:future-features` — passed with 160 items and 18 checked.
- `npm run check:hygiene` and `git diff --check` — passed.

Privacy/security/licensing: display labels and export filename stems changed;
existing local profile data, credential protection, protocol headers, provider
routes, and third-party licensing boundaries are unchanged. No pet files or
pet behavior were modified by this slice.

Rollback: restore the previous display labels and `ProductIdentity` values;
retain the stable storage/protocol identifiers and package IDs. No migration
or deletion is required.

Remaining: update future signed installers, localized resources, published
artifacts, and external distribution listings when those release surfaces are
configured.

## Slice 189 — Make Percy the official Skai Desktop companion

Status: `implemented_asset_runtime_build_verified`

The generated blue wolf is now the official Skai Desktop mascot, named Percy.
The same transparent artwork is configured as the Uno application icon source,
and Percy is the default companion for new profiles. Existing saved pet IDs are
preserved; the existing five companions remain selectable. A generated 4×4
Percy atlas includes idle, walk, thinking, coding, malicious, stuck, success,
and error poses. Pre-cut 256×256 state frames and a dedicated walk sheet are
checked into `assets/pets/`, matching the current animation runtime contract.

Changed files:

- `assets/branding/skaiwolficon.png`
- `assets/pets/percy-sprite-sheet.png`
- `assets/pets/percy-walk-sprite-sheet.png`
- `assets/pets/percy-*.png`
- `assets/pets/cut-percy-atlas.ps1`
- `assets/pets/README.md`
- `assets/pets/cut-frames.sh`
- `native/ServerRock.Native/Assets/Icons/skaiwolficon.png`
- `native/ServerRock.Native/ServerRock.Native.csproj`
- `native/ServerRock.Native/CompanionStateStore.cs`
- `native/ServerRock.Native/MainPage.Companion.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.Quicklinks.cs` (cross-target `DataTemplate` compatibility fix exposed by the Windows packaging build)
- `scripts/audit-native-ui.cjs`
- `docs/PET_OVERHAUL.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors.
- The Windows build also compiles the existing Quicklinks surface through the WinUI XAML compiler; its prior desktop-only `DataTemplate` factory was replaced with an equivalent cross-target template without changing quicklink behavior.
- The generated Windows manifest names `Skai Desktop` and references emitted `skaiwolficon*` package assets.
- Percy frame validation confirmed 256×256 state images and the source atlas is present.
- `npm run audit:native-ui` — passed with Percy asset contracts.
- The complete Core test suite passed before this asset/UI slice; no pet behavior or existing pet tests were removed.

Privacy/security/licensing: this adds locally packaged/generated artwork only;
no network, provider, credential, user-data, or execution behavior changes. The
artwork is original generated content and does not copy Firefox/Mozilla assets.

Rollback: restore the prior icon source and default pet ID, remove Percy asset
references, and leave existing saved companion preferences untouched.

Remaining: wire the generated walk cycle into the wander UI phase when that
phase is implemented; current runtime motion uses the existing transform
animation and the seven supported states.

## Slice 186 — Centralize outbound protocol identity

Status: `implemented_build_tests_audit_verified`

Outbound User-Agent headers for GitHub, web search, and model-library catalog
requests now use a stable technical identifier from `ProductIdentity` instead
of duplicated literals. The identifier is deliberately independent from the
display name so a future rebrand does not alter protocol identity or require
manual service-by-service edits.

Changed files:

- `native/SkaiDesktop.Core/Models/ProductIdentity.cs`
- `native/SkaiDesktop.Core/Services/ModelLibraryService.cs`
- `native/SkaiDesktop.Core/Services/WebSearchProvider.cs`
- `native/SkaiDesktop.Core/Services/GitHubApiClient.cs`
- `native/SkaiDesktop.Core.Tests/ProductIdentityTests.cs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore` — passed, including the stable protocol-identity contract.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed with 236 contracts.
- `git diff --check` — passed.

Privacy/security/licensing: only outbound User-Agent construction changed;
request destinations, payloads, credentials, and provider consent boundaries
are unchanged. No new dependency or third-party code was added.

Rollback: restore the service-local User-Agent literals and remove the added
identity contract; no persisted data or network behavior other than the same
header values is affected.

Remaining: complete rebrand verification across packaging metadata, installers,
localization, and all release artifacts; full keyring parity and OS isolation
remain open.
