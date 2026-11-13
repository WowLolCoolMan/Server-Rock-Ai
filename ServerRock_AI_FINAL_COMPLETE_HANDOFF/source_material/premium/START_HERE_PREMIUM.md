# Server Rock AI — Premium desktop product execution pack

**Edition:** 2026-11-10 · **Purpose:** complete, version-aware handoff for a coding agent and its human reviewer · **Status:** specifications + evidence inventory; NOT an implemented app.

## Mandatory reading order
1. `SPEC_PRECEDENCE.md` and `AGENT_HANDOFF_PROMPT.md`.
2. `upgrade/evidence/CURRENT_REPOSITORY_BASELINE.md`, `upgrade/evidence/REPO_TO_FEATURE_MAP.md` and the repo's live `README.md`, `DESIGN.md`, `docs/ARCHITECTURE.md`, `docs/PRODUCTION_READINESS_GAPS.md`, `docs/UI_REFERENCE_GUIDE.md`.
3. `upgrade/product/PRODUCT_PRINCIPLES.md` and `upgrade/ux/NAVIGATION_SYSTEM.md`, `upgrade/ux/SCREEN_BLUEPRINTS.md`, `upgrade/ux/INTERACTION_STATE_MATRIX.md`.
4. `upgrade/architecture/CORE_BOUNDARIES.md`, `upgrade/architecture/AGENT_RUNTIME.md`, `upgrade/architecture/DATA_STORAGE.md`, `upgrade/architecture/PERMISSIONS_AND_THREAT_MODEL.md`.
5. `upgrade/marketplace/SKILL_MARKETPLACE_FULL_SPEC.md`, `upgrade/platform/CROSS_PLATFORM_RELEASE.md`, `upgrade/quality/QUALITY_GATES.md`.
6. For execution, follow `upgrade/agents/EXECUTION_PLAYBOOK.md`, `upgrade/agents/TWO_AGENT_COLLABORATION.md`, `upgrade/quality/SCENARIO_ACCEPTANCE.md`.
7. Use existing roadmap/ and audit/ files as the broad backlog and inherited research. Use upgraded machine-readable `upgrade/data/*.json` for concrete screen, state and acceptance inventories. Never trust historical status labels without checking code.

## Product objective
Deliver a local-first, premium, full-featured, elegant AI desktop development workspace for Windows and Linux, approaching the UX completeness of Codex, Claude, Discord and Steam **without imitating copyrighted interfaces or assets**. Users can chat, open projects, run coding agents, parallelize tasks safely, browse diffs/terminal, use local and hosted models, manage MCP and optional skills, inspect activity, and install signed updates.

## Hard constraints
- Existing application is **Uno Platform / C# / XAML / .NET 10**. KEEP IT. Electron/React in previous chats was an exploratory recommendation, **not approval to rewrite**. Current repository documentation explicitly says the legacy Electron UI has been removed. Any architecture migration requires an ADR, comparative prototypes and explicit owner approval.
- Two contributors: Windows with Codex and Linux with Claude. Shared contracts, separate branches and evidence-driven merges.
- Odysseus may inspire ideas only. **NO Odysseus source, components, assets, dependencies or integration**.
- Preserve prior code, Git state, profiles, history, user projects and secrets. Never clean/reset/stash, destroy data or silently migrate without user consent. Never claim a plan is a working implementation.
- Existing product identity is separate from Server Rock web/Core services unless a current, explicit requirement authorizes an integration. No silent Server Rock account dependencies.
- Explicit user opt-in for skill installation, model downloads, cloud data egress, commands with side effects, and permission escalation.
- Default local-first. Agent Machines/VMs remain future backlog, NOT a prerequisite.
- **No development duration estimates or calendar promises** in this pack.

## What is new versus the 119-file predecessor
- Exact workspace navigation and routing specifications, compact/expanded layout, route persistence and command palette.
- Premium design system contract: component anatomy, layouts, full UI state grids, interactive feedback, typography, dark/light, accessibility, and window resizing.
- Complete core workflows: onboarding; chat; code changes; approvals; parallel worktrees; cloud/local switching; skills marketplace; MCP; GitHub; updates/recovery.
- Type-safe agent operation model, event journal, permissions, cancellation/fault recovery, provider capability + local/cloud policies, SQLite/JSON migration guidance.
- Production test cases and required Windows/Linux real-package verification.
- 250-entry historical marketplace candidate catalog + metadata caveats; this is NOT 250 validated installable skills.
- Machine-readable route, component, task/event, scenario and requirement inventories; a validation script checks bundle consistency.

## First agent action (read-only)
Open the actual repository and record: `pwd`, OS, architecture, .NET SDK, Git branch/HEAD, `git status --short`, relevant repo guidance, and actual build/test results. Then create/update a truthful `docs/GOAL_EXECUTION.md` in the repo (only after understanding the working tree), compare code to requirements, and select one reversible end-to-end slice. Do not overwrite that ledger if it exists.

## Release gate
A card, tab or test mock is not a feature. Feature complete = working frontend + correct backend + persistence where required + error/cancel/recovery + permissions + keyboard/screen-reader + Windows/Linux evidence + reviewed diff.

**Primary output:** a usable product, not a decorative mockup or an ever-expanding markdown collection.
