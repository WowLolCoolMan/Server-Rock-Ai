# Execution sequence — functionality before decoration, polish throughout

This is an **ordering of dependencies**, not a calendar or time estimate. Every slice ends with reproducible evidence and a reviewable diff, not merely a completed checklist.

## Gate 0 — establish reality

- Read repo guidance, inspect actual Git HEAD/status, SDKs and OS, current tests, build and package targets; preserve all dirty and untracked work.
- Produce `current_capability_matrix`: working, partial, absent, broken, unknown + file references, test commands and observed outcomes.
- Capture screenshots of actual native UI at multiple widths; trace navigation, overlays, focus and active sessions. Compare to `DESIGN.md` and supplied UI references.
- Import the 126 combined requirements as a **seed**, resolve duplicates/obsolete findings and link each to source lines, issue/test and assigned owner. Maintain one canonical status ledger.

## Gate 1 — reliable app shell and design system

- Normalize XAML theme dictionaries, tokens, light/dark palettes, typography, iconography, reusable controls and animations with reduced-motion support.
- Extract app navigation and view model state from `MainPage.*` while preserving project/chat behavior. Add typed routes, back/forward, saveable panel geometry, global Ctrl+K, real native menus and keyboard support.
- Implement overlay coordinator to fix stacked dialogs, hidden full-screen canvases, hit testing, focus traps/restoration and narrow-screen clipping. Add deterministic native UI regression coverage and live visual tests.
- Build onboarding, empty/loading/offline/error states, notification/activity UX and reliable model-selector interactions.

## Gate 2 — conversation / project workspace complete

- Keep streaming chat, attachments, branches, archives, projects, Git and undo working while moving to tested presentation services.
- Add scoped project workbench: session list, file tree/editor tabs, diffs, true PTY terminal, test panel and inspector dock. Preserve drafts/state and unsaved changes through route switching/restarts.
- Full approval UX: exact command preview, file preview/diff, allow/deny, action trace, truthful result, safe undo/review.

## Gate 3 — durable agents and model adapters

- Establish task ID, event journal, scheduler and cancellation/reconciliation. Only after this add robust parallel jobs and task groups.
- Build provider capability discovery, cloud credentials, local model health/hardware metadata and transparent routing/fallback policy.
- Implement compatible agent adapters (ACP / Codex App Server) behind versioned contracts; don't claim unsupported capabilities.
- Implement isolated worktrees, Git conflict review and task artifact persistence.

## Gate 4 — ecosystem

- Create trusted read-only skill candidate directory first, then reviewed installation lifecycle, test-on-fixture, enablement, update detection and rollback.
- Add MCP server management, explicit permissions, connection health and diagnostics; then workflow builder/runner, schedules (opt-in), images/media queue and integrations.
- Add genuinely useful marketplace browsing, model downloads and comparison, search, settings, help and release notes. Avoid dense global menus as a substitute for contextual controls.

## Gate 5 — quality, release and adoption

- Run scenario suite on Windows and Linux, packaging signatures, clean installer upgrade/uninstall/rollback, keyring/service failures, GPU/no-GPU, scaling, offline, crash recovery and accessibility.
- Run usability checks: new user starts chat, developer performs complete code review, two-agent task merges, skill installs and rolls back, offline user resumes, errors are explainable.
- Publish only after release gates, privacy/third-party license review, support docs, signed updates and exact-platform test evidence.

## Ownership across two developers

Windows developer: native shell/navigation/design system, Windows integration and UI acceptance; Linux developer: core runtime/agent and Linux integration, security and release evidence. These are suggested areas of responsibility rather than exclusive ownership. Maintain shared typed contracts, source-of-truth issue ledger, branch ownership, short reviewable PRs and cross-platform integration checkpoints. Never edit both the same file concurrently without coordination; prefer separate branches/worktrees and interface-first agreements.

## Exit evidence per item

Issue ID → underlying defect/user requirement → actual file links → before/after screenshots where visual → tests/logs with OS/build SHA → data/permissions impact → rollback plan → **owner-reviewed diff** → updated ledger. `done` requires working result in the actual application; `blocked` requires a reproducible reason and next action.
