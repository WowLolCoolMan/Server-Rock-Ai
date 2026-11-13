# Odysseus → Server Rock: precise reference implementation map

Source: https://github.com/odysseus-dev/odysseus (default `dev`). Reviewed tree `34e4666f357d458eb31de21b6badf52ce2ef7a96` on 2026-11-08. Odysseus is **AGPL-3.0-or-later**. Study its behavior and design; do not copy AGPL source or assets into a differently licensed project without compliance review. A complex self-hosted Python/FastAPI/browser frontend isn't a drop-in replacement for Uno C# desktop.

| Priority | Reference file(s) | What it demonstrates | Target Server Rock module | Caveat |
|---|---|---|---|---|
| High | `docs/skills-audit-architecture.md` | Separates route/job/runner/evaluation/publication/persistence authority | `Core.Agent.Skills` | Odysseus notes unresolved approval and continuation limits |
| High | `services/memory/skills.py`, `docs/skills-lifecycle.md` | Disk-backed SKILL.md, frontmatter, draft/approved, owner scope and progressive disclosure | `ISkillRegistry` | Reimplement .NET parser + persistence; cannot adopt Python module verbatim |
| High | `services/skills/{job_coordinator,agent_runner,audit_workflow,evaluation,publication,model_selection}.py` | Skill runtime phases, tests, audit and publication | `ISkillLifecycleService` | Never let evaluator verdict grant execution permission |
| High | `docs/AGENT_TURN_CONTRACT.md`, `src/turn_contract.py` | Capability selection, offered/executable set, exact scope | `CapabilityResolver` | User authorization must still be separate |
| High | `docs/BACKGROUND_TOOL_JOBS.md` | Durable originating-chat background result delivery, de-duplication, progress | `JobStore`, `RunProgressVM` | Not all Odysseus jobs are background-safe |
| High | `static/js/modalManager.js` | Explicit open/minimize/close semantics, per-window state and z-order | `OverlayHost` and `WindowCoordinator` | Odysseus itself describes modal fragility; simplify |
| High | `static/js/modalVisibility.js` | Cancel stale close animations when a modal reopens | Uno `OverlayTransitionCoordinator` | Need async cancellation tokens, not JS timers |
| High | `static/js/modalSnap.js` | Docking with reserved workspace width, minimum center content width | `PaneLayoutService` | Server Rock should prefer simpler docked panes to draggable chaos |
| Medium | `static/js/sidebar-layout.js` | Full/mini/hidden sidebar state and mobile drawer | `NavigationShellVM` | Preserve consistent back behavior and focus |
| Medium | `frontend/src/features/notes/ui/windowLayout.js` | Window layout separation from note data and behavior | `IDockLayoutService` | Don't recreate free-floating windows unless users need them |
| Medium | `docs/frontend-architecture/assessment.md` | Frontend architecture audit methodology | UI architecture ADRs | Odysseus is browser-based; tests need Uno adapters |
| Medium | `docs/runtime-decomposition/wave-3-browser-authority.md` | Resource identity and fail-closed authority boundaries | Browser/MCP adapters (future) | Explicitly disabled operations are not a shipped feature |
| Medium | `THREAT_MODEL.md` | Privileged-tool trust model and risks | `SECURITY.md` | Odysseus admits no full shell sandbox |
| Medium | `tests/test_modal_reopen_lifecycle.py`, `tests/test_modal_dock_composer_clearance.py`, `tests/test_skill_importer_security.py` | Regression test ideas for stale modal events, clearance and supply-chain | Native UI/integration tests | Recreate test cases, not original test implementation |
| Medium | `ROADMAP.md` | Authors' acknowledged technical debt and real-world issues | Roadmap risk register | Do not presume reference project is bug-free |

## Actionable differences

1. Odysseus is self-hosted multi-module Python + JS; Server Rock is native single-user desktop C#/Uno. Prefer native routed pages + docked panes instead of fully porting Odysseus draggable modal machinery.
2. Odysseus has many more integrated services and an existing agent/skills pipeline. Use its separation of responsibilities as the blueprint for new **interfaces**, not its module dependencies or risk assumptions.
3. Odysseus `modalManager.js` has a monotonic top z-order and minimize/restore contracts; Server Rock currently uses `ContentDialog`, a backdrop and a companion canvas without a single state machine. The core concept is one overlay owner, one active modal and no unowned focus.
4. Odysseus `modalSnap.js` adjusts main workspace width when docked; Server Rock should reserve minimum composer/chat dimensions and collapse secondary panels rather than overlaying them.
5. Odysseus's own ROADMAP calls out modal/window positioning, prompt bloat, and integration reliability. Treat it as useful evidence of what not to copy.
6. Odysseus's threat model notes privileged shell and prompt-injection gaps. Server Rock's future autonomous executor needs independent containment beyond prompt instructions.

## Research before implementation

Inspect exact cited files at the pinned commit, compare against active upstream, preserve license records and author attribution if any code reuse is proposed, then write an ADR explaining the native alternative. Do not add Odysseus as a runtime dependency merely to obtain UI concepts.
