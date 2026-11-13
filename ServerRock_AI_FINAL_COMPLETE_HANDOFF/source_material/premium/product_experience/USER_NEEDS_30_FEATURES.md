# Server Rock AI — User-centered product differentiation, November 2026

## Research grounding
The product should not claim to exceed ChatGPT/Codex across the board. OpenAI already supports parallel Codex agents, skills, automations, sandboxing, desktop computer use and project workflows. Differentiate through cross-provider/local-first transparency, deterministic model/skill compatibility, inspectable context, installation lifecycle and a consistently predictable native experience.

Sources:
- https://openai.com/index/introducing-the-codex-app/
- https://learn.chatgpt.com/docs/app
- https://help.openai.com/en/articles/20001275-chatgpt-work-and-codex
- https://kairos.jetbrains.com/research/articles/configurable-ai-coding-assistants
- https://kairos.jetbrains.com/research/articles/developer-needs-and-feasible-features
- https://borowiecki.dev/pdf/2610.20618

JetBrains 2026 research highlights demand for context visibility, customization, stability, and validation. Cursor forum research highlights pricing, robustness, setup, latency and workflow dependability. These are evidence-based user-need signals, not proof that any proposed Server Rock AI feature has been built.

## Mandatory workflow contract
Every side-effecting operation uses: intent -> capability/preflight -> preview -> explicit approval when needed -> queued -> executing with cancellation -> verification -> result or failure -> rollback/recovery when possible.
- Never mark success before verification.
- Never run incompatible/disabled skills or tools.
- No silent local-to-cloud transfer.
- No unexplained background work or unexpected file edits.
- Keep accessible keyboard focus, loading, empty, cancellation and error states.
- Maintain durable operation IDs, scoped permissions, idempotency, audit events, and restart recovery.
- Define explicit 'no write' semantics for preview/cancel.
- Native Uno/.NET 10 remains baseline; no unapproved Tauri rewrite.
- Release hardening: test AOT feasibility; otherwise compatible obfuscation, trimming tests, signed artifacts, integrity and secret protection. Open source means no absolute reverse engineering prevention.

## Thirty features by user need

### 01. Approval preview for every consequential action [P0; Trust]
Preview exact files, commands, data destinations and reversibility before execution.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 02. Universal undo and recovery journal [P0; Trust]
Checkpoint edits and reversible settings, with clear non-reversible warnings.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 03. Task timeline and event log [P0; Trust]
Visible queued/running/waiting/failed/completed states, timestamps and logs.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 04. Evidence-backed completion reports [P0; Trust]
Link changed files, executed tests, actual outputs and unresolved issues.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 05. Privacy and data-route indicator [P0; Trust]
Show local versus remote provider, attachments sent, retention options.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 06. Permission profiles and scoped execution [P0; Trust]
Read-only, reviewed edits, trusted workspace; enforce runtime scopes.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 07. Model/provider switch without losing task [P0; Control]
Preserve project state, recompute skill eligibility, show capability changes.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 08. Model routing with cost/latency policy [P1; Control]
Select local/cloud by capability, privacy, latency and budget, explain choice.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 09. Context inspector [P0; Control]
Show exactly what files, memories and tool outputs enter the prompt.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 10. Project rules and task overrides [P1; Control]
Stable project instructions plus temporary task-specific preferences.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 11. Skills marketplace and My Skills [P0; Control]
Curated discover/install/enable/pin/update/remove lifecycle with trust metadata.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 12. Deterministic compatibility-aware skill router [P0; Control]
Installed+enabled, provider/model/tool/dependency gates, progressive loading.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 13. One-minute onboarding [P0; Speed]
Discover local endpoints, import settings and complete a first successful task.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 14. Universal command palette [P1; Speed]
Keyboard access to models, chats, skills, projects, commands and settings.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 15. Persistent project workspaces [P0; Speed]
Resume conversations, tasks, files, model and permissions without setup.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 16. Download and installation center [P1; Speed]
Pause/retry/cancel, dependency visibility, resumable downloads, failure details.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 17. Performance and resource dashboard [P1; Speed]
VRAM/RAM, model load, tokens/sec, first token latency and bottlenecks.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 18. Offline-first graceful fallback [P0; Speed]
Explain what works offline; fallback with consent and no silent cloud transfer.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 19. Parallel agents with isolated worktrees [P1; Capability]
Multiple scoped tasks, conflict detection and reviewable merges.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 20. Agent plan and milestones [P0; Capability]
User-visible phases, blockers, next actions, acceptance criteria.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 21. Automated verification loop [P0; Capability]
Build/test/lint/browser checks with evidence; no fabricated success.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 22. MCP and tool integration manager [P1; Capability]
Install/connect/test/revoke with permissions, process isolation and health.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 23. Workflow recipes [P1; Capability]
Reusable multi-step templates for coding, research, assets, automation.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 24. Visual UI testing [P1; Capability]
Screenshot comparison, accessibility checks, focus and interaction smoke.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 25. ComfyUI and multimodal workspace [P2; Capability]
Images/audio/video where supported; clear model/task compatibility.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 26. GitHub and CI intelligence [P1; Capability]
Issues, PRs, branches, failed builds and review workflows.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 27. Premium design system [P0; Simplicity]
Dark/light themes, native Uno components, consistent typography and density.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 28. Task-centered navigation [P0; Simplicity]
Chat, Agents, Models, Marketplace, My Skills, Activity, Settings.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 29. Human-readable chat history [P1; Simplicity]
Accessible Chat History folder, indexed search, exports and migrations.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

### 30. Guided recovery and actionable errors [P0; Simplicity]
What failed, why, what is safe to retry, what data was preserved.
Acceptance: demonstrate a working end-to-end scenario, explicit failure handling, keyboard access, and verified state transitions. Document current code status separately from target.

## Competitive assessment
- Codex already has parallel agents, worktrees, skills, automations and sandboxing: these are parity requirements, not unique differentiators.
- ChatGPT desktop already has projects, local/computer context and file creation; do not call those exclusive.
- Differentiation hypothesis: one native Windows/Linux application with user-selected local/cloud runtimes, observable data routing, portable skills, model-aware hard gates, inspectable context, clear ownership of installs and user data.
- Validate with usability testing: first-task completion, time to recover from failure, unintended action rate, task success, performance, weekly retention and uninstall/reinstall correctness.
