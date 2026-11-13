# Live capability matrix

Snapshot: current synchronized `main` checkout; refreshed through the live
Windows split-divider slice and local-provider validation

This is the Gate 0 live-repository audit required by the handoff. The
126-row register at
`ServerRock_AI_FINAL_COMPLETE_HANDOFF/index/COMBINED_REQUIREMENTS_126.csv`
remains the source-of-record for individual requirement IDs. This matrix is a
conservative domain view used to choose the next vertical slice; it does not
turn a specification, source comment, or static screen into proof of a working
feature.

Status vocabulary: `working` means the current source and focused tests show a
usable path but may still need platform acceptance; `partial` means meaningful
pieces exist but the complete journey is missing; `absent` means no real
implementation was found; `blocked` means a required external decision or
capability prevents progress; `unverified` means evidence is insufficient.

## Domain audit

| Domain | Status | Current evidence | Main gap / next dependency |
| --- | --- | --- | --- |
| Shell and navigation | partial | `native/ServerRock.Native/MainPage.*`, `SidebarView.*`, focused `ProjectNavigationView` and `ConversationNavigationView` controls, bounded branch-aware and settings-persisted `NavigationHistory`, and `scripts/audit-native-ui.cjs`; static audit covers accessible Back/Forward controls; live Ctrl+K command palette filtering, settings routing, per-workspace draft restoration, decomposed-sidebar Windows accessibility inspection, and live Back/Forward activation with persisted route history verified | Persistent tabs/layout geometry, inspector, typed dialog routes, and live focus/resize acceptance |
| Conversations | partial | Streaming with throttled 50 ms native rendering that preserves manual scroll position, persistence, attachments, folders, forks, JSON/Markdown chat export plus archive import/export, pins, tags, bounded FIFO follow-up queue, Queue/remove/clear/reorder controls, queue persistence across restart, repaired and live-verified queue-management event wiring, live queue reorder/remove/clear evidence with a disposable queue path, live queue-during-streaming and automatic-dispatch evidence with a healthy local model, live restart/accessibility evidence, live automatic dispatch-after-failure evidence, bounded local search across message metadata/content with deterministic ordinal matching, live search dialog/result/open-chat journey, successful live response with a compatible local model, separate Chat/Coding Agent composer drafts across workspace switches, provider-side deterministic context compaction that marks omitted history before an oversized request is sent, opt-in DuckDuckGo web search with bounded snippets, persisted source metadata, clickable source cards, citation-aware prompt instructions, live six-result search and cited local-model response evidence, a Settings-managed local memory editor with approval/provenance, durable enable/edit/delete controls, versioned local JSON backup export, explicit duplicate-safe JSON backup restore, bounded opt-in global/active-project memory injection marked as untrusted reference context, live Windows memory-manager accessibility inspection, live disposable memory-manager CRUD evidence (view, edit, save, reselect, and delete) with a disposable profile, live disposable eligible-note review/save/send evidence with a healthy local model, live disposable approved-memory injection after app restart with the same disposable profile, live Windows inspection of folder creation, chat export/import, pin, tag-edit, archive, and fork controls, live disposable folder/tag/pin/move mutation with restart persistence, per-message fork implementation with bounded copied-prefix tests and live disposable activation evidence, bounded local current-chat message search with command-palette entry, selected-result jump, and live Windows acceptance, a live Windows read-only review dialog for approved memory injection, a live Windows disposable Save As type list showing both JSON archive and Markdown transcript choices plus a live disposable Markdown file write/readback with deterministic content assertions, a live Windows read-only split view with an accessible saved-chat comparison picker and normal-surface restoration, a live Windows bounded split-ratio slider with settings-backed restart persistence, and bounded local prompt snippet save/insert controls | Durable semantic conversation-context continuity and profile migration beyond approved-memory injection, live model-expansion acceptance for snippets, draggable divider semantics, drag-and-drop pane layouts, multiple independently editable/composing panes, cross-platform packaged rendering, per-conversation Markdown directory layout, freshness/answer-accuracy evaluation, and broader provider coverage remain open |
| Projects and Git workspaces | partial | `ProjectStore`, `GitHubWorkspaceService`, project context, guarded changes, undo and Git review tests; isolated-index selected commits now exclude unrelated pre-staged work while preserving the user's index; push now refreshes `origin` before evaluating ahead/behind state | Full project workbench, conflict recovery, worktree isolation, live Git dialog acceptance and complete push reconciliation |
| Agent workbench | partial | Coding Agent proposals, approvals, terminal commands, reviewed file changes, durable operation run/event contract, chat lifecycle integration, reviewed reconciliation decisions, and a bounded local dependency-aware agent task-artifact queue with persisted roles, result summaries, artifact labels, Activity planning/queue/cancel/search controls, a non-mutating readiness preview, a read-only task detail view, and live Windows acceptance of dependency selection with persisted dependency labeling and cancellation | Actual parallel-agent dispatch, process/worktree isolation, task execution/results beyond local review, background execution, and cross-platform acceptance |
| Editor and review | partial | `WorkspaceFileEditor`, `WorkspaceTextDiff`, bounded previews and undo tests; reviewed writes now serialize per target path and reject the losing concurrent review hash | Multi-tab editor, external-process conflict evidence, syntax-aware editing and inspector integration |
| Terminal and tools | partial | Real interactive terminal session, bounded commands, cancellation that kills the process tree, and secret-shaped environment filtering | OS sandbox truth, richer tool transport, process isolation and cross-platform PTY acceptance |
| Models and local runtimes | partial | Local runtime discovery across Ollama and a generic OpenAI-compatible endpoint, model library, downloads/import checks, measured local performance, conservative capability profiles, persisted vision override, bounded provider-advertised model metadata, connection status evidence, provider-agnostic model discovery/selection/pinning evidence on a disposable compatible endpoint, bounded side-by-side comparison for two to four pinned models with explicit remote consent, and live Windows comparison evidence for two distinct pinned Ollama models with separate labeled responses | Hardware/resource scheduling, richer provider coverage and unload/queue states; remote-provider consent, durable provider labeling across all provider types, and remote comparison remain unverified |
| Routing and providers | partial | OpenAI-compatible transport, session-only BYOK for any compatible endpoint (including OpenRouter), live disposable loopback compatible-provider discovery/selection/pinning, local-only endpoint guard and vision formats; durable operation provenance; capability-aware preflight with persisted override, bounded discovery metadata, visible selected-model capability status, live compatible-local-model response acceptance, explicit local-only recovery, accessible fallback confirmation, live model-compatibility failure classification, explicit unavailable-selected-model policy with no silent switch, pre-send blocking when a discovered inventory excludes the selected model, and local-only enforcement when selecting a pinned route | Capability-aware multi-route selection, richer proactive compatibility detection for advertised capabilities, durable provider identity across all provider attempts, and remote-route live acceptance |
| Workflows and scheduled tasks | partial | Activity surface includes a durable concurrent approval inbox with approve/deny, expiry timestamps, identity/outcome summaries, stale-expiry handling, and a bounded accessible countdown; it also exposes dependency-ordered plans, dry-run previews, condition results, exact-scope grants, a read-only adapter, and durable effects; the bounded scheduler persists due/claimed/completed/failed/paused state, prevents replay after restart, exposes schedule/pause/resume and reviewed Run now controls that surface due prompts without dispatch, supports bounded hourly, two-hour, daily, and weekly recurrence after successful surfaced runs, and live Activity evidence now shows a completed scheduled task with the explicit outcome `Surfaced in the composer for review; no provider request was sent.` | Reviewed execution adapter, OS session host, write/shell/network adapters, automatic result logging, timezone/locale matrix, backoff policies, richer branching, parallel limits, autonomous background execution, and full audit/export |
| Skills marketplace | partial | Built-in catalog plus a live-inspected accessible Skills surface for installed local packages; `SkillPackageInstaller` validates pinned provenance syntax, bounded Markdown-only packages, symlink/path safety, package digests, atomic activation, rollback, explicit package warning, disabled-by-default installation, durable user enable/disable grants, pre-install version/digest/permission/update preview, reviewed prompt loading, deterministic local date/time variable expansion, explicit opt-in `{{USER_NAME}}` substitution before review, and live Windows expansion of a variable-bearing built-in skill prompt | GitHub download/authentication and remote ref existence, publisher trust, live Windows install/enable/preview, cross-platform and packaged acceptance |
| MCP and integrations | partial | Plugin catalog and GitHub integration exist; bounded stdio and Streamable HTTP JSON/SSE clients initialize, retain a negotiated session ID/protocol version, discover conservatively classified tools, share persisted connection/revocation policy, parse OpenAPI 3 operations and bounded local component schema references into untrusted read/write descriptors without execution or implicit grants, and expose a live-inspected local JSON review picker | Selecting/rendering a real schema, authenticated retrieval, external `$ref` resolution, long-lived notifications/resources, ACP/process supervision, UI lifecycle, health diagnostics, audit records and real integrations |
| Images and media | partial | ComfyUI queue/poll flow, image attachments and local vision paths have Core tests | Resumable media jobs, richer artifact storage, capability gating and runtime acceptance |
| Activity and diagnostics | partial | Runtime status banner with bounded actionable in-memory history, redacted diagnostic export, durable operation journal, chat lifecycle integration, durable started/completed/failed/cancelled provider-attempt provenance, Activity & tasks history surface, explicit reconciliation outcomes, provider/model/privacy provenance, explicit versioned local operation-evidence export, bounded local operation search, and bounded workflow-event search with separate accessible result lists; live Windows Activity & tasks inspection exposed accessible Export, Search, Schedule, provider-attempt rows with run IDs/sequences, and interrupted-operation Cancel/Fail decisions | Live keyboard activation of Activity actions, seeded live workflow-event result matching, live status-history keyboard/screen-reader acceptance, OS session hosting, parallel agent artifacts, Linux parity and packaged acceptance |
| Settings and appearance | working | Persisted settings, accent/density, light/dark palettes, Deep Sea/Deep Space animated ambient presets, reduced-motion control, model/voice/host controls; Core persistence tests; inline endpoint/model/local-only validation keeps invalid Save attempts open with an accessible error | Broader platform-specific diagnostics and complete live accessibility matrix |
| Companion/pet | partial | First-party companion state, selection and persistence, bounded local custom-sprite paths, composer avoidance, drawer occlusion guard, keyboard movement, and live Windows selection/placement evidence | Cross-platform packaged positioning, full breakpoint/scale collision matrix, modal focus acceptance, and custom-sprite picker acceptance remain open |
| Updates and releases | partial | Core now verifies an RSA-authenticated release manifest, channel/version monotonicity, and staged artifact SHA-256; packaging reads manifest-driven display/launcher metadata, Windows shortcuts target the versioned executable with adjacent runtime files, the current self-contained `win-x64` Release artifact passed bounded startup-and-window smoke, the current checkout passes both `net10.0-desktop` and `net10.0-windows10.0.26100` builds, disposable install/upgrade/rollback/uninstall preserved profile data, repository hygiene and release-configuration gates pass, and CI remains fail-closed without silent installation | Signed installers, release-feed integration, clean-machine install/upgrade/uninstall/rollback and platform evidence |
| Security and privacy | partial | Bounded I/O, path checks, redaction, approval surfaces and local/cloud labeling are present; repository hygiene passes across 325 tracked files and the release configuration validator remains internally consistent; runtime product display identity is centralized in `ProductIdentity` for rebranding | OS isolation, keyring parity, threat-model coverage and full negative scenario suite |
| Windows/Linux parity | unverified | Windows native/App SDK builds and Core tests are available locally; Ubuntu 24.04 WSL is installed, but a fresh current-checkout Linux build attempt stopped because that distro has no `dotnet` command; repository docs contain historical Linux evidence | Install/enable a supported Linux .NET 10 SDK or use a CI Linux runner, then perform a fresh build/run, exact distro scope, accessibility, packaging and release-artifact validation |

## Gate 0 evidence collected

- Live checkout: `main`, synchronized with `origin/main`, with the user-provided
  `ServerRock_AI_FINAL_COMPLETE_HANDOFF/` directory intentionally left
  untracked and untouched.
- Handoff register: 126 rows consisting of 86 premium requirements and 40
  uploaded remediation issues; every row initially declares
  `requires_live_repo_recheck`.
- Current focused evidence: Core test harness, native desktop build, and the
  144-contract native UI audit have passed for the current slice; no new claim of
  Linux or packaged-release validation is made here.
- Existing slice evidence is recorded in `docs/GOAL_EXECUTION.md`.
- Live search evidence: the native Windows app exposed `Search your workspace`,
  the `Search chats in both workspaces` field, a matching `FIRST_DONE` result,
  and `Open chat`; opening the result loaded the saved conversation and its
  `Queued follow-up validation message`.
- Live command-palette evidence: Ctrl+K opened `Command palette`; the
  `Search command palette actions` field filtered to `Connection & settings`,
  and running the action opened the accessible Settings surface.
- Live provider evidence: the configured local endpoint advertised five local
  models. The selected `huihui_ai/Qwen3.8-abliterated:latest` closed its stream
  without content and the app exposed a distinct connection error. Using the
  existing Settings route, `wtwk-qwen35-4b:latest` connected and returned
  `LIVE_OK` in the native conversation with Copy/Retry actions; the original
  model selection was restored afterward. No cloud route was used.
- Live recovery evidence: after the original local model failed, the native
  error bubble exposed `Choose another local model after connection failure`
  with help text stating that no model would be switched automatically;
  activating it opened the accessible `Model Library` and listed five
  installed local models.
- Live fallback-consent evidence: selecting `wtwk-qwen35-4b:latest` from that
  recovery library exposed `Switch local model?` with explicit current/target
  models and no-cloud/no-automatic-retry language; choosing `Keep current
  model` preserved the original model preference.
- Live failure-classification evidence: the original local model's stream
  closure produced the accessible runtime status `Model compatibility issue ·
  choose another local model or check Settings`, while retaining the recovery
  chooser and without changing the selected model.

## Recent slice update

Slice 152 verified the split-view divider as a direct pointer interaction on
Windows. The accessible `Drag split pane divider` gutter accepted a live drag,
changed the bounded ratio slider from 50 to 55, and retained the existing
settings-backed ratio event. Drag-and-drop pane reordering, independently
editable panes, and cross-platform packaged rendering remain open requirements.

Slice 153 added a separate local Markdown-folder export action. It creates one
validated, sanitized per-conversation `.md` file while preserving the existing
single-file JSON and Markdown exports. Core filename coverage, native build,
and the 202-contract UI audit pass; live FolderPicker/write/readback evidence
and rich Markdown editing remain open.

Slice 154 verified durable workspace composer drafts. A debounced local settings
write and startup restore survived a disposable Windows process restart; the
remaining gap is encrypted or cross-device draft synchronization and
attachment-draft persistence.

Slice 155 verified separate durable attachment drafts. A bounded atomic local
store restored a seeded `notes.md` chip into the native composer on Windows;
encrypted storage, cross-device synchronization, and picker-based add/remove
acceptance remain open.

Slice 156 repaired Activity and agent-task search focus handoff and verified a
native keyboard query entry on Windows. Full screen-reader announcements,
Linux parity, and keyboard acceptance for export/schedule actions remain open.
- Live unavailable-selection evidence: Settings temporarily saved
  `missing-model`; the app reported `Connected · selected model missing-model is
  unavailable · choose a model in Model Library`, kept `missing-model` in the
  accessible ModelButton description, and did not auto-save or switch to an
  advertised model. The original model was restored afterward; no cloud route
  was used.
- Live settings-validation evidence: Settings was opened through the command
  palette, the endpoint draft was changed to `not an endpoint`, and Save kept
  the dialog open with the accessible message `Enter a valid http:// or
  https:// model endpoint.`. Cancel then closed the dialog while the original
  local endpoint and model remained selected.
- Live workspace-draft evidence: a Chat draft and a separate Coding Agent draft
  were entered in the native Windows app; switching workspaces and switching
  back restored each exact draft while the selected model remained unchanged.
- Git selected-commit evidence: the Core Git fixture pre-staged
  `pre-staged-user-work.md`, committed a separate reviewed selection, and
  verified that the new commit omitted that file while the original index kept
  it staged. No force push or remote mutation was used.
- Live pre-send model-guard evidence: after temporarily selecting
  `missing-model`, the native Windows app preserved the draft and announced
  `Selected model missing-model is unavailable · choose a model in Model Library
  before sending`; no conversation turn was created. The original model was
  restored afterward.

## Matrix reconciliation — current through Slice 160

The compact domain rows above predate the latest vertical slices. Their gaps
must be read together with these newer verified results:

- Conversations now include the live draggable split divider, persisted bounded
  split ratio, per-conversation Markdown-folder export implementation, durable
  workspace composer drafts, and separate durable attachment drafts. Remaining
  gaps are the live picker/write-back journey, cross-device or encrypted draft
  sync, multi-pane editing, and broader provider/cross-platform acceptance.
- Activity and workflows now include live Schedule task form acceptance and a
  safe cancel path. Scheduler persistence/run-now behavior has Core coverage,
  while timezone/locale, full keyboard/screen-reader Popup focus, autonomous
  execution, and packaged/Linux acceptance remain open.
- Shell/workspace now includes bounded in-app documentation from More and Ctrl+K
  with an accessible scroll region. Localization, richer searchable help, and
  typed dialog routing remain open.
- Slices 157–158 deliberately did not claim full dialog focus acceptance: the
  Windows accessibility tree continues to expose the underlying composer after
  Activity opens, despite loaded-content and low-priority focus retries.

## Backlog ordering

1. Validate live approval accessibility/countdown and chat queue controls,
   then add richer branching, parallel limits, scheduling, and full audit/export.
2. Finish the shell/navigation seams and live focus/resize acceptance around
   the existing native UI.
3. Extend the explicit model-selection policy into provider capability
   discovery and multi-route selection before broad model/provider expansion.
4. Build workflow/task execution on the durable operation contract, then add
   MCP/skills/integrations with provenance and permission gates.
5. Complete cross-platform, accessibility, packaging, security and release
   acceptance using actual artifacts and recorded evidence.

No item is marked release-complete by this matrix. Each slice must append
commands, outcomes, changed files, tests, platform scope, privacy/licensing
impact and rollback notes to `docs/GOAL_EXECUTION.md`.

Recent slice note — Slice 157: dialog action focus is now dispatcher-backed
for Activity & tasks, status history, and agent-task planning, with 206 native
UI audit contracts passing. A live Windows accessibility check still reported
the underlying composer as focused after Activity opened, so keyboard and
screen-reader focus acceptance remains an explicit open gap.

Recent slice note — Slice 158: dialog focus now retries after loaded content at
low dispatcher priority and the native audit passes 208 contracts. Activity
rendering and actions were rechecked live on Windows, but the accessibility
tree still reports `PromptBox` as focused; this remains an explicit release
gap pending a supported Popup focus bridge or dedicated accessibility harness.

Recent slice note — Slice 159: live Windows Activity acceptance opened the
Schedule task dialog, exposed accessible prompt/date/time/workspace/recurrence
controls and the review-only safety disclosure, then cancelled without creating
a task or mutating provider/conversation state. Persistence, run-now, timezone
and autonomous execution remain open.

Recent slice note — Slice 160: added bounded in-app documentation reachable from
More and Ctrl+K. Live Windows verification rendered five guide sections in an
accessible scroll region; native audit now passes 212 contracts and the future
feature inventory has 11 checked items. Localization, richer help content, and
Popup focus acceptance remain open.

Recent slice note — Slice 161: live Windows agent planning accepted an existing
local prerequisite, persisted the relationship as `after Disposable final
dependency task`, and exposed the saved plan with accessible queue/cancel
controls. The disposable plan was cancelled before execution, so the Agent
Workbench gap is now parallel dispatch/isolation/background execution rather
than dependency selection.

Recent slice note — Slice 162: Activity and status-history dialogs now declare
an explicit close default and the native audit passes 214 contracts. The live
Windows accessibility tree still reports `PromptBox` after Activity opens, so
the Popup focus gap remains open pending a supported focus bridge or dedicated
accessibility harness.

Recent slice note — Slice 163 continuation: Activity and status-history dialogs
now also use the supported WinUI `FocusManager.TryFocusAsync` bridge and cycle
their local tab navigation. Build and the 218-contract native audit pass, but
live Windows accessibility still reports `PromptBox` after Activity opens. The
remaining issue is therefore the Uno/Win32 Popup accessibility bridge, not an
omitted dialog focus target or an unsupported focus API.

Recent slice note — Slice 166: added a bounded local Prompt snippets surface
reachable from Ctrl+K and More. Users can save up to 24 snippets of up to 4,000
characters, insert a selected snippet into the composer, or explicitly request
a model expansion that opens a reviewable preview before insertion. Live Windows
save-and-insert acceptance passed. A compatible local model then returned an
editable expansion preview, and the explicit Use expanded text action inserted
the reviewed candidate into the composer.

Recent slice note — Slice 164: scheduled tasks now support durable Cancel from
Activity while scheduled or paused. Core cancellation/non-claimability tests,
218 native UI contracts, and a live Windows create → Activity → Cancel journey
passed; the UI explicitly reported that no prompt or provider was called.
Autonomous/background scheduling remains intentionally unresolved.

Recent slice note — Slice 165: Settings → Appearance now separates the persisted
light/dark palette from three ambient presets: None, Deep Sea (animated bubbles
over a blue gradient), and Deep Space (animated stars, planets, and drifting UFO
silhouettes). Reduced decorative motion pauses the animation while retaining the
selected visual. Core settings persistence and desktop build verification passed;
live Windows visual acceptance remains required before this is release-complete.

Recent slice note — Slice 167: added a review-first built-in translator reachable
from Ctrl+K and More. A disposable phrase was translated to Spanish through the
selected local model, shown in an editable review dialog, and explicitly inserted
into the composer without sending a message. Provider failure, cancellation,
localization, and cross-platform accessibility coverage remain open.

Recent slice note — Slice 168: added bounded local Quicklinks reachable from
Ctrl+K and More. A disposable bookmark was saved, selected, exposed through the
explicit Open/Remove controls, and removed locally in a live Windows journey.
Only credential-free http/https URLs are accepted; opening remains user-driven.

Recent slice note — Slice 169: added a persisted Advanced → Developer mode
toggle and a gated Ctrl+K/More diagnostics surface. Live Windows enabled the
mode, saved it, and rendered bounded local runtime/model facts without exposing
credentials, running commands, changing approvals, or sending provider data.

Recent slice note — Slice 170: oversized text and PDF attachments now use a
bounded lexical retrieval pass before prompt construction, while smaller files
retain full-text injection and images retain the existing vision path. Core
coverage proves query-relevant chunk selection and output bounding. Vector
backends, reranking, OCR, and a full NotebookLM-style corpus remain open.

Backlog reconciliation — provider-side bounded context compaction and the
persisted dependency-aware local agent task list are now checked in
`FUTURE_FEATURES.md` because their Core tests and prior Windows acceptance
evidence prove the bounded implementations. Semantic summarization, 1M+
context, autonomous execution, and parallel delegation remain explicitly open.

Recent slice note — Slice 172: pending text/PDF/image attachments now expose a
local read-only Preview action. The implementation builds and the native audit
passes, but the live Windows file-picker journey could not be completed because
the picker did not appear from the running Uno/Win32 surface; the preview
acceptance remains open.

Recent slice note — Slice 173: the desktop target now defines an explicit
`SRAI_DESKTOP` picker bridge so Win32 window-handle initialization is compiled
for native picker surfaces. The desktop build passed, but the picker still did
not appear during repeated live activation; preview acceptance remains open.

Recent slice note — Slice 174: Composer Tools now includes an explicit local
path fallback for attachment workflows when the native picker is unavailable.
The path dialog reads only after the user chooses Attach, applies the existing
type, signature, size, character, image-byte, and file-count checks, and keeps
the result removable before sending. Live Windows acceptance attached the
handoff README, opened its bounded read-only preview, then removed the pending
attachment. Full RAG, OCR, and app-preview coverage remain open.

Recent slice note — Slice 175: hardened the desktop companion surface. Custom
state sprites are now restricted to fully qualified local SVG/PNG/JPEG/WEBP
paths; the companion is hidden while the compact navigation drawer is open;
speech/name regions do not consume pointer input; and placement moves away from
the composer when its calculated bounds overlap. Live Windows acceptance
selected Byte, confirmed the companion rendered after drawer dismissal, and
verified the selected name in the accessibility tree. Cross-platform and full
breakpoint/scale/modal acceptance remain open.

Recent slice note — Slice 176: the custom pet-state sprite dialog now has an
explicit local-path fallback alongside the native picker. It validates a fully
qualified existing local SVG/PNG/JPEG/WEBP path, caps the file at 8 MB, stores
only the path, and keeps Clear custom sprite available. Live Windows acceptance
saved `assets/pets/byte-success.png` for the success state, rendered it on the
Byte companion, then cleared the temporary customization and restored the pet
to hidden. Picker, cross-platform, and full breakpoint acceptance remain open.

Recent slice note — Slice 177: Activity & tasks agent artifacts now expose an
explicit read-only `Inspect task details` action. It shows the bounded local
instruction, status/role, result summary, and artifact labels without claiming,
dispatching, or sending work. Parallel execution, isolation, background work,
and cross-platform acceptance remain open.
