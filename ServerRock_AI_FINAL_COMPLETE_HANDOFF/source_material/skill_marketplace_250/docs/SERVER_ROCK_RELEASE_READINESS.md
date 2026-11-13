# Server Rock: AI — Native Desktop Product Audit and Remediation Plan

**Baseline inspected:** public `WowLolCoolMan/Server-Rock-Ai` repository, main branch, 2026-11-08. Target: native Uno Platform desktop (`net10.0-desktop` Skia on Linux/Windows; separate Windows App SDK target), `SkaiDesktop.Core` net10.0 library, and native Core console tests.

**Scope and evidence standard:** Selected source files plus README, `docs/ARCHITECTURE.md`, `docs/PRODUCTION_READINESS_GAPS.md`, `docs/NATIVE_RELEASE_ACCEPTANCE.md`, and `docs/MODEL_LIBRARY.md` were read. This is **not** a full recursive code audit, clean build, packaged install, runtime accessibility test, penetration test, or security certification. Recommendations below are marked **OBSERVED** when directly supported by inspected code/docs and **VERIFY** where a failure is plausible but unproven. A product is not “good” merely because these tasks are written down; it becomes release-ready when acceptance evidence exists.

## Executive findings

1. **The architecture is credible but incomplete.** Uno front end and UI-independent Core library are sensible. Existing local/cloud model discovery, protected code-edit approval, and signed Core updater are assets worth preserving.
2. **The Skill Marketplace is presently a static preview, not a real marketplace.** `SkillCatalogService.BuiltIns` contains three read-only prompts; the Skills dialog copies a prompt into `PromptBox`, and `PluginCatalogService` marks the “Skills Pack” as not available yet. Do not advertise install/update/reviews as working until integrated.
3. **Existing release documents explicitly list major P0 gates as unresolved:** packaged GUI interaction acceptance, real signing, real installer/uninstall/rollback runs on supported OSes, representative Linux distros and import flows. These are blockers for broad distribution.
4. **The current model transport streams OpenAI-compatible chat completions.** This is useful for local/hosted text models but not evidence of portable tool calling, agentic loop execution, or generic Hugging Face model execution. Skill routing must be capability-based, not model-name-based.
5. **The README documents ComfyUI local image workflows and a non-universal Hugging Face model import path.** Preserve that distinction in marketing and the model picker.

## Severity and release milestones

- **P0 / must close before public install/update release:** correctness, integrity, user data, production security, packaged acceptance.
- **P1 / must close before advertising a first-class AI workspace:** core workflow completeness, marketplace, capability routing, provider reliability, UI clarity and accessibility.
- **P2 / quality investments:** discoverability, aesthetics, performance, diagnostics, extensions.

## A. P0 — Distribution, integrity and data safety

| ID | Priority | Finding / required change | Source evidence | Acceptance criterion |
|---|---|---|---|---|
| R01 | P0 | Test exact **signed** shipping binaries on clean Windows 11 and representative Linux desktops. Record Windows build, CPU architecture, Linux distro/windowing system and native deps. | `docs/NATIVE_RELEASE_ACCEPTANCE.md` records WSLg/short artifact smoke, not full distro support | CI artifacts installed and launched with an interaction pass; attached logs and reproducible matrix |
| R02 | P0 | Configure actual Windows Authenticode certificate/timestamp and Linux GPG or equivalent signed distribution provenance. Keep signing keys out of repo. | `docs/PRODUCTION_READINESS_GAPS.md`, `docs/NATIVE_RELEASE_ACCEPTANCE.md` | Reproducible release workflow fails closed without keys; signatures validated on clean test machines |
| R03 | P0 | Ship and exercise genuine installer/uninstaller/upgrade/rollback paths; do not conflate a portable zip with a native installer. | Same docs, `packaging/native/` | Verify fresh install, upgrade, interrupted update, rollback and uninstall; persisted profiles remain intact |
| R04 | P0 | Verify signed update manifests and hashes, reject redirects/host substitution, fail closed on mismatched channel, and test key rotation. | README Core updater description | Automated negative tests for tampering, mismatched hashes/versions, network failure and key rollover plan |
| R05 | P0 | Validate representative legacy chat import in actual packaged Windows/Linux UI; preview, cancel, restart, idempotence and interrupted imports. | `docs/PRODUCTION_READINESS_GAPS.md` | No data loss; repeat import does not duplicate or overwrite edited chats |
| R06 | P0 | Add privacy threat model for imported skills, connected credentials, local model endpoints, cloud requests and ComfyUI workflows. | `MainPage.xaml.cs` key/session fields; marketplace absent | Document data paths, trust boundaries, abuse scenarios and mitigations; security tests for each high-risk action |
| R07 | P0 | Ensure skills cannot auto-execute scripts, run shell commands, open networks or read user files solely because a Markdown file requests it. | New marketplace feature, currently absent | Explicit per-capability permission review; untrusted instructions never authorize a tool; denied calls are blocked |
| R08 | P0 | Enforce safe install destinations, no traversal/symlink escape, file caps, per-skill version pins and human-approved updates. | New marketplace feature | Tests with malicious paths, symlinks, large repos, bad Git content and network interruptions |
| R09 | P0 | Audit secret handling/logging and persistent storage across GitHub sign-in, cloud providers, logs and support bundles. | README says OpenRouter key session-only; GitHub uses platform credentials | Secret-leak canaries; no tokens in manifests, logs, exports, crash reports or JSON settings |
| R10 | P0 | Build a user-controlled backup/export, data-delete and recovery contract for conversations, settings, workspaces and skills. | README states local JSON stores; installer preserves profiles | Clean restore and delete tests; missing/corrupt stores recover gracefully; no silent overwrite |

## B. P1 — Actual skills marketplace and agent execution

| ID | Priority | Required change | Source evidence | Acceptance criterion |
|---|---|---|---|---|
| R11 | P1 | Replace static `SkillCatalogService.BuiltIns` presentation with a Core `ISkillRegistry` backed by indexed, locally persisted catalog/install metadata; keep existing three built-ins as entries. | `native/SkaiDesktop.Core/Services/SkillCatalogService.cs` | Search 250 candidates by name/tag/capabilities; offline mode shows cached records |
| R12 | P1 | Evolve `SkillsDialogAsync` from “load prompt” to a marketplace: Discover, Installed, Updates, Details, Permissions, Versions, Activity. | `native/ServerRock.Native/MainPage.PluginsAndSkills.cs` | Accessible keyboard navigation; no falsely enabled installs; visible blocked reasons |
| R13 | P1 | Distinguish **skill instructions**, **skill with scripts**, **MCP/tool package**, **reference-only catalog**. | Earlier research; existing `PluginCatalogService` mixes categories | UI labels distinguish installation from tool connection, licensing and executable privileges |
| R14 | P1 | Add explicit GitHub skill install service to Core, with private per-user data dirs and revision/tree hashes. No app binary changes. | `GitHubApiClient` exists; no skill installer in Core | Win/Linux install/uninstall/upgrade smoke; user data never saved beside executable |
| R15 | P1 | Compute update status from the skill *subtree SHA*, not whole-repo HEAD. Surface Available, Current, Modified, Offline, Error, Incompatible. | New feature | Unrelated commits do not false-flag skill; offline/error never say Up to date |
| R16 | P1 | Add update diff/permission diff, changelog/commit link, staging and rollback. No silent auto-update. | New feature | Modified local skill yields conflict; failed upgrade restores previous version |
| R17 | P1 | Build a skill permission manifest covering filesystem, shell, network, GitHub/connector actions and model/data routing; tie it to runtime approvals. | Existing Coding Agent already has review-before-edit/command patterns | Unit tests show prompt injection cannot bypass approval; least-privilege grants are per user/project |
| R18 | P1 | Validate YAML frontmatter, name, description, compatibility, required files and scripts. Treat metadata as untrusted input. | Agent Skills open spec | Malformed or unsupported skills show actionable errors; no arbitrary code execution during import |
| R19 | P1 | Preserve source attribution, license, upstream repository/tree SHA and review state. Add opt-in revocation/quarantine path. | Marketplace will reference third-party work | Every installed skill has traceable provenance; unknown license blocks redistribution and warns before install |
| R20 | P1 | Implement **progressive skill selection**: lexical + embedding candidates, capability filter, top-k selection, then load `SKILL.md` only when needed. | `BuildProviderMessages` currently assembles base context; no dynamic skill loader observed | 1000 skills indexed without ballooning each request; measurable routing precision/recall |
| R21 | P1 | Implement an explicit agent tool loop, JSON-schema tool definitions and tool-result handling, or fall back to instruction-only skills for incapable models. | `OpenAiCompatibleProvider.StreamChatAsync` inspected and shows streamed text; tool loop not evident there | Test real tools on at least one local and one cloud model; clearly mark unsupported models |
| R22 | P1 | Provide four meta skills: problem solver, selector, skill creator and GitHub scout; require users to approve creation/installation. | Original skills supplied in this package | Each passed benchmarks and can be invoked without hidden external dependencies |
| R23 | P1 | Build GitHub discovery with freshness, license/quality filters, deduplication, provenance and human review. | User requirement; GitHub client exists | New candidates suggested with reason/evidence, never auto-installed; scoped network settings |
| R24 | P1 | Add skill-aware diagnostics/logs for selection, skipped reasons, permission prompts, tool results and update checks. Redact input/output secrets. | New feature | Exportable redacted evidence shows why skill was/wasn't selected; no private prompts in telemetry |
| R25 | P1 | Add compatibility gates for Python, Node, Git, Docker, .NET, Unity and FiveM skills; detect exact versions before offering execution. | `dependency-doctor` is manifest-only, not full resolver | Fail gracefully and suggest correct runtime; no invented dependencies or unsupported auto-installs |

## C. P1 — Model compatibility, provider resilience, ComfyUI and Hugging Face

| ID | Priority | Required change | Source evidence | Acceptance criterion |
|---|---|---|---|---|
| R26 | P1 | Introduce `ModelCapabilities` persisted per provider+endpoint+model: tools, JSON, vision, audio, context, file types, reasoning, max output, privacy/locality, known/probed. | `ModelLibraryService` scans model inventories; README documents heterogeneous models | Incompatible skill/attachment blocked before sending; users see evidence and manual override controls |
| R27 | P1 | Distinguish model selection from actual model readiness (installed/downloaded/loaded/endpoint healthy); retain per-endpoint pinned identities. | `docs/MODEL_LIBRARY.md` already tracks identities; some load steps exist | UI reflects preparing/error/ready and never silently claims a model is runnable |
| R28 | P1 | Add provider-specific adapters for tool calls and structured outputs; streaming-compatible cancellation and timeouts. | `OpenAiCompatibleProvider` chat-completions SSE path | Cross-provider test corpus: streaming, tool calls, malformed chunks, rate limit, recovery |
| R29 | P1 | Make cloud/local routing transparent per request, including attachments, retrieved project snippets, skill scripts and prompts. | README remote/local privacy guidance | Before upload show destination and data categories; cancel means no send |
| R30 | P1 | Add fallback policy with explicit consent and never leak locally scoped data to cloud automatically when local fails. | Multiple local/cloud endpoints supported | Simulate outage; local-only mode stays local and warns rather than silently switching |
| R31 | P1 | Display real usage/cost/latency and provenance where provider reports it; label estimates as estimates. | `docs/MODEL_LIBRARY.md` uses character-derived tok/s estimates | Benchmarks distinguish measured vs estimated; no invented pricing or context sizes |
| R32 | P1 | Improve Hugging Face compatibility preflight: task type, weights, sharding, backend, accelerator/VRAM, dependencies, license, gated access and storage. | README and `docs/MODEL_LIBRARY.md` explicitly describe limited install routes | Non-compatible model gives precise reason and supported runtime alternatives |
| R33 | P1 | ComfyUI workflow manager: template presets, import/export, node dependency check, missing-model guidance, execution history, cancel/retry, privacy. | `ComfyImageProvider.GenerateAsync` uses user-supplied API workflow and loopback-only endpoint | API-format workflow validated before queue; missing nodes detected; failure shows node, not blank image |
| R34 | P1 | Make image task outputs first-class in history and generated artifact folders, not only temporary preview cards. | `MainPage.ImageStudio.cs` currently constructs a dialog result stack | Generated images persist with workflow revision and model attribution when user chooses save |
| R35 | P1 | Harden SSE/HTTP handling against truncated streams, malformed responses, excessive redirects and retries after cancellation. | `OpenAiCompatibleProvider` bounded requests and 5-minute timeout | Fault-injection tests and no duplicate requests after cancel |

## D. P1 — UX, accessibility and stability

| ID | Priority | Required change | Source evidence | Acceptance criterion |
|---|---|---|---|---|
| R36 | P1 | Replace oversized nested transient modal flows with persistent navigation for Marketplace, Model Library and Settings where the workflow demands many steps. | `docs/ARCHITECTURE.md`: one MainPage with most dialogs built dynamically | Navigation state/focus preserved across tabs, details and back; no controls hidden behind modal |
| R37 | P1 | Fix Skills dialog's duplicate Done/Close semantics; create explicit Use/Install/Cancel with accurate status. | `MainPage.PluginsAndSkills.cs` uses PrimaryButtonText Done and CloseButtonText Close | Only one dismiss action; changing/installing skills cannot silently navigate away |
| R38 | P1 | Standardize loading, retry, error, empty, disabled, confirmation and progress states across model downloads, GitHub and marketplace. | Existing release gap docs request consistent statuses | Every async action has visible progress + cancellation/retry and focus restoration |
| R39 | P1 | Run **live** screen reader, keyboard-only, high contrast, DPI scaling, narrow window and Linux Skia tests. Static greps aren't accessibility acceptance. | `docs/NATIVE_RELEASE_ACCEPTANCE.md` mentions 83 static contracts only | Narrator/Orca sessions recorded; WCAG contrast/focus and full keyboard paths pass |
| R40 | P1 | Move toward testable view-model/service boundaries instead of MainPage owning workflow state and UI business logic. Refactor incrementally; don't rewrite indiscriminately. | `docs/ARCHITECTURE.md` says no DI/view-model layer; MainPage partial files | Core command/state operations unit-testable without XAML; UI smoke covers lifecycle |
| R41 | P1 | Add crash recovery and persisted job state for Git clones, skill updates, model downloads and ComfyUI tasks. | Current app has persisted settings, but marketplace job history absent | Force kill mid-operation and restart; no corrupt installs or falsely completed UI status |
| R42 | P1 | Improve first-run setup flow: choose local/cloud, detect existing runtimes, privacy controls, optional marketplace discovery, test connection. | README existing provider configuration | A new user can get first reply with clear requirements; failure paths explain next action |

## E. P2 — Quality, scale and polish

| ID | Priority | Required change | Acceptance criterion |
|---|---|---|---|
| R43 | P2 | Add unified model/task/skill discovery: search by human goal, filter by capabilities, installed status, license and rating. | Task query finds matching skill/model without manual jargon |
| R44 | P2 | Add skill evaluation harness: relevance, precision, tool success, cost, latency, refusal/error analysis and regression goldens per model. | Failed/low-quality skill automatically marked Needs review, not universally “elite” |
| R45 | P2 | Build a structured activity timeline for planning, skill selection, tool calls, approval, code changes and verification. | Every completed action traceable and exportable with redaction |
| R46 | P2 | Visual design consistency: scalable typography, spacing tokens, responsive details pane, dense/comfortable modes, dark theme. | Design review screenshots on Windows/Linux with no clipping/overlap |
| R47 | P2 | Add user-facing repair action for broken local services, unsupported extensions, expired credentials and missing model runtimes. | Setup diagnosis distinguishes endpoint unreachable vs model unloaded vs auth missing |
| R48 | P2 | Build meaningful help/docs, data use disclosure, accessibility shortcuts, tool permission explanation, migration guide and troubleshooting. | First-time-user usability test completes core workflows without developer support |
| R49 | P2 | Performance budgets: startup, large chat search, catalog index, 10k skill metadata, low-end local LLM runtime and huge model lists. | Set explicit budget and benchmark across machines; loading large catalogs remains responsive |
| R50 | P2 | Third-party maintenance controls: advisory feed, license change, abandoned dependency, incompatible upstream, integrity mismatch and pinning. | Warning states and review workflow demonstrated with synthetic fixtures |

## Explicit non-findings and strengths to preserve

- No evidence was obtained that Server Rock currently executes arbitrary marketplace scripts: no marketplace executor exists in inspected files. This is **future risk**, not a discovered current exploit.
- Protected Coding Agent proposal review and guarded undo are described and partially visible in `MainPage.CodingAgent.Proposals.cs`; preserve these approval gates rather than replacing with unrestricted auto-execution.
- The Core updater already documents Ed25519 manifests, SHA-256 checks and explicit installer prompts; **verify** and harden these instead of inventing a second updater.
- Existing docs appropriately distinguish supported local GGUF/ONNX routes from general Hugging Face model availability. Avoid claiming universal model execution.
- Current Core project uses `net10.0`; changing target frameworks without confirming Uno/Skia compatibility would be dangerous.

## Recommended implementation order (cross-cutting)

**Phase 0 — freeze baseline (P0):** branch, snapshot `git status`, back up real profiles, run Core tests, native build and current UI audit. Do not remove untracked files or rewrite history. Record source SHA and exact SDK environment.

**Phase 1 — protect distribution (P0):** product security and privacy requirements, signed binary/release path, installer clean OS tests, data resilience, platform support and live accessibility. Independent of marketplace content.

**Phase 2 — native marketplace Core (P1):** registry, deterministic Git installer, update index, permission checker, compatibility inspector, local JSON/SQLite persistence, status/error model, tests.

**Phase 3 — agent runtime (P1):** skill routing, independent tool adapter, explicit plan/act/observe loop, provider capability negotiation, approval and trace logging.

**Phase 4 — UX and integration (P1):** permanent Marketplace screen, installed/update views, skill creator/scout flows, clear model compatibility, ComfyUI and HF workflow improvements.

**Phase 5 — release proof (P0+P1):** artifact-specific testing, security and failure simulations, accessibility testing, installers, rollback and code signing, release evidence checklist. Do not market readiness until complete.

## Suggested engineering handoff format

For each remediation ticket: **source files → current observed behavior → expected behavior → implementation steps → migrations/security implications → tests (positive/negative) → screenshot/CLI evidence → owner/status → risk/rollback**. Preserve existing user settings, source tree changes and working integrations. Document test commands and actual results, not just plans.
