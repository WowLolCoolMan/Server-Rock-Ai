# Live capability matrix

Snapshot commit: `870b62b` plus the live local-provider validation

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
| Shell and navigation | partial | `native/ServerRock.Native/MainPage.*`, `SidebarView.*`, `scripts/audit-native-ui.cjs`; static audit passes 89 contracts; live Ctrl+K command palette filtering and settings routing verified | Typed routes, navigation history, persistent tabs/layout, inspector and live focus/resize acceptance |
| Conversations | partial | Streaming, persistence, attachments, folders, forks, archive import/export, pins, tags, bounded FIFO follow-up queue, Queue/remove/clear/reorder controls, queue persistence across restart, live restart/accessibility evidence, live automatic dispatch-after-failure evidence, bounded local search across message metadata/content, live search dialog/result/open-chat journey, and successful live response with a compatible local model | Durable context continuity, model-specific response compatibility/recovery |
| Projects and Git workspaces | partial | `ProjectStore`, `GitHubWorkspaceService`, project context, guarded changes, undo and Git review tests | Full project workbench, conflict recovery, worktree isolation, tested Git push reconciliation |
| Agent workbench | partial | Coding Agent proposals, approvals, terminal commands, reviewed file changes, durable operation run/event contract, chat lifecycle integration, and reviewed reconciliation decisions | Activity UI acceptance, scheduler, parallel agents and task artifacts |
| Editor and review | partial | `WorkspaceFileEditor`, `WorkspaceTextDiff`, bounded previews and undo tests | Multi-tab editor, external-change conflicts, syntax-aware editing and inspector integration |
| Terminal and tools | partial | Real interactive terminal session, bounded commands, cancellation and secret-shaped environment filtering | OS sandbox truth, richer tool transport, process isolation and cross-platform PTY acceptance |
| Models and local runtimes | partial | Local runtime discovery, model library, downloads/import checks, measured local performance, conservative capability profiles, persisted vision override, bounded provider-advertised model metadata, and connection status evidence | Hardware/resource scheduling, richer provider coverage and unload/queue states |
| Routing and providers | partial | OpenAI-compatible transport, local-only endpoint guard, OpenRouter session key and vision formats; durable operation provenance; capability-aware preflight with persisted override, bounded discovery metadata, visible selected-model capability status, live compatible-local-model response acceptance, explicit local-only recovery, accessible fallback confirmation, and live model-compatibility failure classification | Capability-aware multi-route selection, proactive compatibility detection, and durable provider identity across all provider attempts |
| Workflows and scheduled tasks | partial | Activity surface includes a durable concurrent approval inbox with approve/deny, expiry timestamps, identity/outcome summaries, stale-expiry handling, and a bounded accessible countdown; it also exposes dependency-ordered plans, dry-run previews, condition results, exact-scope grants, a read-only adapter, and durable effects; live Activity & tasks accessibility is verified | Seeded live pending-approval countdown acceptance, write/shell/network adapters, richer branching, parallel limits, scheduling and full audit/export |
| Skills marketplace | partial | Read-only built-in catalog and truthful unavailable states in `SkillCatalogService` | Provenance-aware discovery, staged install, fixture testing, enablement, update diff and rollback |
| MCP and integrations | partial | Plugin catalog and GitHub integration exist; unavailable integrations are labeled honestly | MCP/ACP connections, scoped grants, health/revocation, diagnostics and real integrations |
| Images and media | partial | ComfyUI queue/poll flow, image attachments and local vision paths have Core tests | Resumable media jobs, richer artifact storage, capability gating and runtime acceptance |
| Activity and diagnostics | partial | Runtime status banner, redacted diagnostic export, durable operation journal, chat lifecycle integration, Activity & tasks history surface, explicit reconciliation outcomes, and provider/model/privacy provenance | Search/exported operation evidence and live accessibility acceptance |
| Settings and appearance | working | Persisted settings, accent/density, light/dark palettes, model/voice/host controls; Core persistence tests | Full validation matrix, platform-specific diagnostics and live accessibility acceptance |
| Companion/pet | partial | First-party companion state, selection and persistence exist | Complete presentation/positioning/collision/accessibility acceptance |
| Updates and releases | partial | Signed-manifest verification and fail-closed CI contracts are documented/implemented in parts | Signed installers, clean-machine install/upgrade/uninstall/rollback and platform evidence |
| Security and privacy | partial | Bounded I/O, path checks, redaction, approval surfaces and local/cloud labeling are present | OS isolation, keyring parity, threat-model coverage and full negative scenario suite |
| Windows/Linux parity | unverified | Windows native build and Core tests are available locally; repository docs contain historical Linux evidence | Fresh live Linux build/run, exact distro scope, accessibility, packaging and release artifacts |

## Gate 0 evidence collected

- Live checkout: `main` at `870b62b`, with the user-provided
  `ServerRock_AI_FINAL_COMPLETE_HANDOFF/` directory intentionally left
  untracked and untouched.
- Handoff register: 126 rows consisting of 86 premium requirements and 40
  uploaded remediation issues; every row initially declares
  `requires_live_repo_recheck`.
- Current focused evidence: Core test harness, native desktop build, and the
  83-contract native UI audit have passed in prior slices; no new claim of
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

## Backlog ordering

1. Validate live approval accessibility/countdown and chat queue controls,
   then add richer branching, parallel limits, scheduling, and full audit/export.
2. Finish the shell/navigation seams and live focus/resize acceptance around
   the existing native UI.
3. Add explicit provider capability discovery and routing policy before broad
   model/provider expansion.
4. Build workflow/task execution on the durable operation contract, then add
   MCP/skills/integrations with provenance and permission gates.
5. Complete cross-platform, accessibility, packaging, security and release
   acceptance using actual artifacts and recorded evidence.

No item is marked release-complete by this matrix. Each slice must append
commands, outcomes, changed files, tests, platform scope, privacy/licensing
impact and rollback notes to `docs/GOAL_EXECUTION.md`.
