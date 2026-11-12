# Live capability matrix

Snapshot commit: `0696124` plus the activity-surface working slice

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
| Shell and navigation | partial | `native/ServerRock.Native/MainPage.*`, `SidebarView.*`, `scripts/audit-native-ui.cjs`; static audit passes 83 contracts | Typed routes, persistent tabs/layout, command palette, inspector and live focus/resize acceptance |
| Conversations | partial | Streaming, persistence, attachments, folders, forks, archive import/export, pins and tags in `native/ServerRock.Core.Tests` | Message queue, richer search, durable context continuity, live keyboard/accessibility acceptance |
| Projects and Git workspaces | partial | `ProjectStore`, `GitHubWorkspaceService`, project context, guarded changes, undo and Git review tests | Full project workbench, conflict recovery, worktree isolation, tested Git push reconciliation |
| Agent workbench | partial | Coding Agent proposals, approvals, terminal commands, reviewed file changes, durable operation run/event contract, chat lifecycle integration, and reviewed reconciliation decisions | Activity UI acceptance, scheduler, parallel agents and task artifacts |
| Editor and review | partial | `WorkspaceFileEditor`, `WorkspaceTextDiff`, bounded previews and undo tests | Multi-tab editor, external-change conflicts, syntax-aware editing and inspector integration |
| Terminal and tools | partial | Real interactive terminal session, bounded commands, cancellation and secret-shaped environment filtering | OS sandbox truth, richer tool transport, process isolation and cross-platform PTY acceptance |
| Models and local runtimes | partial | Local runtime discovery, model library, downloads/import checks and measured local performance | Capability probes, hardware/resource scheduling, richer provider coverage and unload/queue states |
| Routing and providers | partial | OpenAI-compatible transport, local-only endpoint guard, OpenRouter session key and vision formats | Explicit profiles, capability-aware routing, classified fallback consent and durable provider identity |
| Workflows and scheduled tasks | partial | Activity surface shows dependency-ordered workflow plans, read-only dry-run previews, versions, step states, approval actions, condition results, executor-unavailable labels, exact-scope grants, and a workflow-wired read-only adapter with durable effect events | Effect timeline UI, write/shell/network adapters, richer branching, parallel limits, scheduling and full audit/export |
| Skills marketplace | partial | Read-only built-in catalog and truthful unavailable states in `SkillCatalogService` | Provenance-aware discovery, staged install, fixture testing, enablement, update diff and rollback |
| MCP and integrations | partial | Plugin catalog and GitHub integration exist; unavailable integrations are labeled honestly | MCP/ACP connections, scoped grants, health/revocation, diagnostics and real integrations |
| Images and media | partial | ComfyUI queue/poll flow, image attachments and local vision paths have Core tests | Resumable media jobs, richer artifact storage, capability gating and runtime acceptance |
| Activity and diagnostics | partial | Runtime status banner, redacted diagnostic export, durable operation journal, chat lifecycle integration, Activity & tasks history surface, and explicit reconciliation outcomes | Search/exported operation evidence and live accessibility acceptance |
| Settings and appearance | working | Persisted settings, accent/density, light/dark palettes, model/voice/host controls; Core persistence tests | Full validation matrix, platform-specific diagnostics and live accessibility acceptance |
| Companion/pet | partial | First-party companion state, selection and persistence exist | Complete presentation/positioning/collision/accessibility acceptance |
| Updates and releases | partial | Signed-manifest verification and fail-closed CI contracts are documented/implemented in parts | Signed installers, clean-machine install/upgrade/uninstall/rollback and platform evidence |
| Security and privacy | partial | Bounded I/O, path checks, redaction, approval surfaces and local/cloud labeling are present | OS isolation, keyring parity, threat-model coverage and full negative scenario suite |
| Windows/Linux parity | unverified | Windows native build and Core tests are available locally; repository docs contain historical Linux evidence | Fresh live Linux build/run, exact distro scope, accessibility, packaging and release artifacts |

## Gate 0 evidence collected

- Live checkout: `main` at `3e34156`, with the user-provided
  `ServerRock_AI_FINAL_COMPLETE_HANDOFF/` directory intentionally left
  untracked and untouched.
- Handoff register: 126 rows consisting of 86 premium requirements and 40
  uploaded remediation issues; every row initially declares
  `requires_live_repo_recheck`.
- Current focused evidence: Core test harness, native desktop build, and the
  83-contract native UI audit have passed in prior slices; no new claim of
  Linux or packaged-release validation is made here.
- Existing slice evidence is recorded in `docs/GOAL_EXECUTION.md`.

## Backlog ordering

1. Expose durable workflow effect events in Activity, then add richer
   branching, parallel limits, scheduling, and full audit/export.
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
