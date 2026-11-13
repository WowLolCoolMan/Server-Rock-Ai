# Full product scope — premium desktop development environment

## Product thesis

Server Rock AI is a first-party, local-first, cross-platform workspace for chat, code, assets, automation and AI agents. A first-time user can start a truthful chat in seconds without configuring the entire ecosystem. Experienced users can supervise parallel code agents, review changes, orchestrate repeatable workflows, manage local/cloud models, install reviewed skills and recover from failures without leaving the app.

**Premium is not a number of sidebar buttons.** Each implemented capability needs complete discoverability, delightful but restrained visual interaction, correct execution, honest status, error recovery, keyboard support, accessibility, cross-platform behavior and persistence. Borrow product ideas from Codex, Claude, Discord and Steam, not their source code, trade dress, artwork, names or proprietary assets.

## Domains, journeys and depth expectations

| Domain | Primary path | Deep capabilities | States/quality bar |
|---|---|---|---|
| Home | See recent work → resume | Pinned/recent projects, quick chat, pending approvals, global activity, first-run checklist | Empty/offline/loading/restored |
| Chats | Pick model → converse | Branches/forks, search, history, attachments, rich markdown, citations, retries, edit/resend, tokens/usage where measured, copy/export | Partial streaming, interrupted, provider error, privacy consent |
| Projects | Open folder or GitHub repo | Project notes, scoped directories, file explorer, recent tasks, Git status, project settings, pinned models/skills | Missing path, invalid repo, permission denied, dirty worktree |
| Agent workbench | Define goal → run → approve → review | Parallel agents, isolated worktrees, plans, terminal, test results, diffs, inline review, checkpoints, task handoff | Queued/running/waiting/paused/cancelling/failed/completed/reconciling |
| Editor | Open artifact/file → edit safely | Multi-tab text/code, search, diff, external editor, save/discard, conflict detection, syntactic highlighting | Dirty/read-only/changed externally/conflicting/saved |
| Terminal & tools | Run reviewed command | Real local PTY, input/output, cwd, environment inspection, cancel, clean teardown, shell selection | Noninteractive failures, disconnects, permission prompts |
| Models | Discover → inspect → select | Providers, pins, health, streaming speed measured locally, compatible tasks, downloads, quantizations, GPU memory, unload | Unknown/offline/auth required/busy/unsupported/corrupt |
| Routing | Set profile → send task | Cloud/local/auto, capability matching, cloud consent, quota/failure classification, provider fallback, sticky local | No silent data exfiltration or model change |
| Workflows | Choose template → run | Visual DAG, conditions, retries, tool policies, scheduled triggers after explicit consent, step-level audit | Draft/validated/running/paused/blocked/completed/failed |
| Skills Marketplace | Discover → review → install → enable | Version pinning, source/commit/licensing, tree hash updates, dependency scan, test-on-fixture, rollback, category and search | Candidate vs verified vs installed vs enabled vs executable |
| MCP/integrations | Find → connect → authorize | Permission scopes, health, revocation, secrets, logs, diagnostics, retry, workspace grants | Connected/degraded/disconnected/revoked |
| Images & media | Choose provider/workflow → create | ComfyUI task graph, queue, job cards, artifacts, progress, cancel and retry, storage | Local availability, incompatible media, job resumability rules |
| Activity center | Inspect active jobs | Tool events, errors, notifications, unresolved approvals, search/export diagnostics, durable status | Notification never pretends task succeeded |
| Settings | Search category → edit → save | Appearance, profiles, permissions, Git, data, models, downloads, keyboard, accessibility, updates | Save/cancel validation, focus restoration, rollback |
| Companion/pet | Optional enable → configure | First-party companion sprites, low-interference anchoring, accessibility and show/hide | Never intercept underlying UI hit tests |
| Help & update | Learn → diagnose → update | Command reference, onboarding, release notes, updater signatures, errors/support bundle | Clear version/channel, no phantom update |

## Feature-completeness rubric

For each feature require: accessible entry point; permission and dependency disclosure; real service backend; cancel/retry/undo where applicable; empty/loading/working/error/offline/permission-denied states; durable user data; keyboard/assistive technology checks; telemetry-free/explicit privacy behavior; documented tests; Windows and Linux verification or a visible explicit platform limitation. Items not meeting rubric may remain a deliberately restricted experimental feature, not a production-ready badge.

## User archetypes

- **Chat-first user**: first launch → discover local provider or configure cloud → choose model → chat → return tomorrow to same history.
- **Solo developer**: reopen Git project → agent audits → inspect plan/diff/test → approve granular actions → commit with review.
- **Power user**: orchestrate parallel Codex/Claude/local agents in separate isolated worktrees; compare outcomes and reconcile conflicts.
- **Marketplace user**: install a source-verified skill, approve capabilities, test on a sample, enable per project, inspect updates safely.
- **Linux user**: launch on supported distro, configure keyring and model runtime, perform equal tasks without fake Windows-specific controls.

## Explicit exclusions

Never represent unbuilt features as working, treat every Hugging Face model as universally runnable, claim sandboxing based only on a button, promise unsupported Windows versions, send local project data to cloud without explicit profile/consent, claim 250 entries are verified, or ship an untested installer.
