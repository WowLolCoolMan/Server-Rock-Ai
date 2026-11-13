# Server Rock AI — Source-grounded 100-step roadmap
Date: 2026-11-10

## Decision
Keep Uno Platform + C#/.NET 10. Refactor rather than rewrite. Elevate the static marketplace into a real package lifecycle. Treat predictable workflows and professional UI as product requirements. Agent Machines stays **backlog only**.

## What was verified from GitHub
- `docs/ARCHITECTURE.md`: Uno native UI + Core, no shipping Electron UI; `MainPage` partial architecture.
- `native/SkaiDesktop.Core/Services/SkillCatalogService.cs`: three static built-in prompt templates, not an installable marketplace.
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`: dialogs and prompt loading, not dynamic skill installation/execution.
- `native/SkaiDesktop.Core/Services/PluginCatalogService.cs`: static integration catalog with honest built-in/unavailable labels.
- `native/ServerRock.Native/MainPage.Chat.cs`: streaming, consent and cancellation flow exists; do not replace without parity.
- `native/ServerRock.Native/MainPage.Recents.cs`: conversations.json persistence, folders, archives and branches; preserve data.
- `native/ServerRock.Native/MainPage.ModelLibrary.cs`: existing local model browsing, download, switching and route labeling.
- `docs/NATIVE_RELEASE_ACCEPTANCE.md` and `docs/PRODUCTION_READINESS_GAPS.md`: packaged smoke, signing, installer and accessibility gates remain.
- `docs/NATIVE_MIGRATION.md`: single native runtime and platform-service boundaries.

GitHub source URLs:
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/docs/ARCHITECTURE.md
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/docs/PRODUCTION_READINESS_GAPS.md
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/docs/NATIVE_RELEASE_ACCEPTANCE.md
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/native/SkaiDesktop.Core/Services/SkillCatalogService.cs
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/native/ServerRock.Native/MainPage.PluginsAndSkills.cs
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/native/ServerRock.Native/MainPage.Chat.cs
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/native/ServerRock.Native/MainPage.Recents.cs
https://github.com/WowLolCoolMan/Server-Rock-Ai/blob/main/native/ServerRock.Native/MainPage.ModelLibrary.cs

## Important caveat
This is a static source/documentation audit plus a review of the supplied ZIP. No code was modified, compiled or runtime-tested here. The 100 steps are a sequenced engineering proposal, not 100 confirmed bugs. Some existing code already implements portions of proposed features and must be preserved. Confirm working tree state and branch before implementation.

## Hard workflow contract
Intent -> Preflight -> Preview -> Approval (if required) -> Execute -> Verify -> Complete or Recover. Never claim success from UI state alone. Every side effect needs visible scope, cancel semantics, logs, error handling and ownership.

## Release hardening
AOT must be proven compatible, not presumed; use signed distributions, optional obfuscation where appropriate, no bundled secrets. Open source cannot be made non-reverse-engineerable.

## Out of scope for current delivery
Agent Machines / virtual machines; major framework rewrite; unreviewed auto-install of hundreds of GitHub skills; unsupported claims of parity or superiority.

## Exact 100-step sequence

### A. Baseline and safety
1. **SR-001** — Freeze a read-only repository inventory and capture the inspected commit SHA. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
2. **SR-002** — Record Windows and Linux build prerequisites, target frameworks and supported OS versions. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
3. **SR-003** — Run existing Core tests and retain raw results before edits. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
4. **SR-004** — Run existing native UI static audits and document what they do not test. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
5. **SR-005** — Produce a feature inventory mapping every visible control to actual behavior. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
6. **SR-006** — Map current navigation, dialogs, modal nesting and keyboard focus paths. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
7. **SR-007** — Map every file, network, process and credential side effect. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
8. **SR-008** — List known P0/P1 release gaps with owners and reproducible evidence. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
9. **SR-009** — Establish a no-destructive-git rule and preserve all local changes. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
10. **SR-010** — Define a release baseline with measurable startup, task and failure metrics. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### B. Architecture and workflow foundation
11. **SR-011** — Introduce dependency injection at app composition root without breaking existing Core services. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
12. **SR-012** — Extract navigation state from MainPage into a typed navigation service. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
13. **SR-013** — Introduce MVVM view models for Chat, Coding Agent, Models, Marketplace and Activity. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
14. **SR-014** — Move side-effecting operations behind application-service interfaces. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
15. **SR-015** — Define durable operation IDs, actor, workspace, scope and correlation IDs. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
16. **SR-016** — Implement a typed workflow state machine with guarded transitions. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
17. **SR-017** — Implement preflight capability, dependency and permission checks. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
18. **SR-018** — Implement preview artifacts for file edits, commands, installs and remote data transfer. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
19. **SR-019** — Implement explicit approval receipts and expiration rules. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
20. **SR-020** — Implement cancellable execution, restart recovery and idempotent retries. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### C. Visual design and accessibility
21. **SR-021** — Write visual principles, information hierarchy and density rules for all screens. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
22. **SR-022** — Create shared light/dark color, typography, spacing and elevation tokens. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
23. **SR-023** — Build reusable Uno buttons, inputs, cards, banners, dialogs and status components. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
24. **SR-024** — Design a stable left navigation rail with adaptive widths and labels. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
25. **SR-025** — Build first-class Chat, Agents, Models, Marketplace, My Skills and Activity destinations. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
26. **SR-026** — Replace full-feature modal dialogs with dedicated pages where appropriate. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
27. **SR-027** — Build one shared model selector showing provider, endpoint and privacy route. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
28. **SR-028** — Define skeleton, empty, loading, success, failure and offline states. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
29. **SR-029** — Implement keyboard shortcuts, focus order, accessible names and screen-reader semantics. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
30. **SR-030** — Run screenshot-based visual regression across themes, DPI, window sizes and both OSes. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### D. Chat and project UX
31. **SR-031** — Preserve conversation IDs, folders, archives, branches and legacy import semantics. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
32. **SR-032** — Design human-readable chat export with an indexed Chat History directory. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
33. **SR-033** — Add fast full-text search with filters and private local indexing. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
34. **SR-034** — Add a context inspector showing instructions, files, attachments and tool outputs. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
35. **SR-035** — Add per-project instruction files and visible temporary overrides. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
36. **SR-036** — Add source-linked attachment preview with explicit truncation indicators. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
37. **SR-037** — Expose token usage, cost estimates, latency and model limitations when available. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
38. **SR-038** — Make stop, retry, edit and regenerate states explicit and reliable. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
39. **SR-039** — Add safe workspace snapshot/restore for supported local changes. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
40. **SR-040** — Test recovery from interrupted streams, corrupt history and insufficient disk space. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### E. Models and routing
41. **SR-041** — Inventory existing provider adapters and real capability discovery. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
42. **SR-042** — Implement provider health checks with actionable errors and timestamps. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
43. **SR-043** — Define model capability descriptors for tools, vision, context and structured output. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
44. **SR-044** — Add deterministic local/cloud routing policies and explain selection. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
45. **SR-045** — Never silently send local context to a remote provider during failover. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
46. **SR-046** — Implement model switching while preserving task and context consistency. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
47. **SR-047** — Build a first-run model connection wizard with verified success test. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
48. **SR-048** — Implement resumable model download states and checksums where supported. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
49. **SR-049** — Surface RAM/VRAM, model load and throughput metrics where measurable. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
50. **SR-050** — Test provider timeouts, rate limits, malformed responses and reconnection. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### F. Skill marketplace
51. **SR-051** — Define versioned SkillManifest with source, publisher, dependencies and permissions. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
52. **SR-052** — Design registry/catalog schema separating curated entries from installed packages. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
53. **SR-053** — Import existing three built-in skills as backward-compatible entries. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
54. **SR-054** — Build native Discover Marketplace screen with search, filters and trust metadata. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
55. **SR-055** — Build native My Skills screen with installed, enabled, pinned and update states. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
56. **SR-056** — Implement pinned GitHub revision download into a staging directory. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
57. **SR-057** — Validate paths, archive sizes, symlinks, signatures/hashes and manifest structure. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
58. **SR-058** — Create atomic installation records with ownership ledger and rollback. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
59. **SR-059** — Implement version checks, update preview, staged update and downgrade. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
60. **SR-060** — Implement clean removal without deleting shared or user-owned dependencies. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### G. Skills, MCP and agents
61. **SR-061** — Build deterministic skill eligibility gates for OS, model, tools and permissions. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
62. **SR-062** — Implement relevance ranking only after hard compatibility filters. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
63. **SR-063** — Implement progressive skill instruction loading with token-budget limits. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
64. **SR-064** — Expose why each skill was selected, rejected or unavailable. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
65. **SR-065** — Create a skill authoring/test/publish workflow with human review. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
66. **SR-066** — Separate MCP server lifecycle and permissions from prompt-only skills. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
67. **SR-067** — Implement MCP process health, timeout, restart and revocation controls. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
68. **SR-068** — Create agent plan milestones with explicit acceptance criteria. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
69. **SR-069** — Introduce isolated parallel agent workspaces and conflict-aware merge review. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
70. **SR-070** — Create a replayable agent activity timeline with evidence and redacted logs. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### H. Integration and reliability
71. **SR-071** — Verify existing GitHub device flow, credential persistence and disconnect behavior. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
72. **SR-072** — Add visible reviewed diff, command and commit approval summaries. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
73. **SR-073** — Add automated build, lint and test verification to coding workflows. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
74. **SR-074** — Add actionable failure diagnostics without exposing secrets. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
75. **SR-075** — Add a centralized downloads and long-running operations center. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
76. **SR-076** — Implement secure secret references instead of embedding credentials in skill manifests. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
77. **SR-077** — Add backup/restore of settings, chats and installed-package metadata. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
78. **SR-078** — Test offline mode, revoked credentials, disconnected providers and partial installs. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
79. **SR-079** — Test cancellation at each operation state including during restart. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
80. **SR-080** — Create structured diagnostics export with consent and redaction. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### I. Release engineering
81. **SR-081** — Publish reproducible Windows and Linux build instructions and dependency lockfiles. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
82. **SR-082** — Evaluate Native AOT compatibility with Uno, reflection and XAML; do not assume support. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
83. **SR-083** — Test safe trimming/obfuscation only on compatible components. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
84. **SR-084** — Add signed artifacts, checksums and provenance verification to release pipeline. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
85. **SR-085** — Test installer upgrade, uninstall, rollback and user-data retention. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
86. **SR-086** — Run packaged app smoke on Windows 11 and specified Linux distributions. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
87. **SR-087** — Test screen reader, keyboard, scaling and focus against packaged builds. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
88. **SR-088** — Measure cold start, idle memory, UI responsiveness and long-running task stability. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
89. **SR-089** — Run threat-model review of GitHub packages, MCP, command execution and network scopes. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
90. **SR-090** — Define no-go release gates and publish an evidence-based release candidate report. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

### J. Launch, validation and backlog
91. **SR-091** — Recruit representative local-model, cloud-model and coding-agent beta testers. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
92. **SR-092** — Measure first useful task completed within five minutes and identify friction. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
93. **SR-093** — Measure unexpected action rate and approval clarity through usability tests. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
94. **SR-094** — Measure marketplace install/update/remove success and recovery rates. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
95. **SR-095** — Compare task outcomes against Codex and ChatGPT without unsupported superiority claims. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
96. **SR-096** — Prioritize top user pain points from beta reports with reproducible cases. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
97. **SR-097** — Write in-app onboarding and troubleshooting for Windows and Linux. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
98. **SR-098** — Document compatibility matrix and clearly label unimplemented integrations. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
99. **SR-099** — Record Agent Machines VM concept as post-launch backlog only, with no implementation work. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.
100. **SR-100** — Ship only after all P0 gates pass and publish a transparent limitations list. *Exit evidence:* test positive, negative, cancellation and persistence where applicable.

## Phase exit gates
- A: baseline is reproducible, no user data or working tree changes.
- B: operations follow explicit state transitions; preview/cancel are side-effect safe.
- C: native UI meets cross-platform visual and accessibility checks.
- D: chat data survives migration and interruption.
- E: provider routing, data transfer and failure handling are observable.
- F: install/update/rollback/remove skills operate against actual files and manifests.
- G: skills/MCP/agents respect enforced compatibility and permission scopes.
- H: real integrations have verified error/recovery behavior.
- I: signed packages, supported-OS runtime and uninstall/rollback pass.
- J: user success, usability and transparent release limitations are measured.

## Suggested implementation slices
Slice 1: steps 1-20 (baseline and workflow engine).
Slice 2: steps 21-40 (design system, navigation, chat safety).
Slice 3: steps 41-70 (models, marketplace, router, agent integration).
Slice 4: steps 71-90 (integrations and production release).
Slice 5: steps 91-100 (beta and launch).
Each slice must be independently testable and reversible.
