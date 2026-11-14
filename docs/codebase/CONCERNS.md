# Concerns and development direction

## Highest-priority findings

| Priority | Finding | Evidence | Impact | Recommended direction |
|---|---|---|---|---|
| High | The live Electron pet bypasses the contributed grid system. | `native/SkaiDesktop.Core/Services/PetWanderPlanner.cs`, `native/SkaiDesktop.Core.Tests/PetWanderPlannerTests.cs`, `apps/desktop/electron/main.cjs` | The unit-tested behavior is not the behavior users see; the free-form pixel loop can regress around composer space, resizing, DPI, monitor edges, and hide/minimize transitions. | Make a single planner/adapter contract authoritative for Electron. Feed it real work-area, window, composer, scale, drag, and reduced-motion state. Add Electron-window smoke tests. |
| High | Marketplace is visually present but not a real discovery/install system on main. | `apps/desktop/src/features/catalog/MarketplacePage.tsx`, `services/agent-host/Protocol/AgentHostLoop.cs`, Core catalog/model services | Users cannot use the intended many-provider model/voice/image/skill/MCP workflow; the UI advertises cards while the runtime contract is absent. | Add typed `marketplace.*` host methods for provider polling, normalized results, filters, provenance, trust/license checks, download/resume/cancel, install/rollback, and health. Then bind every card/filter/action to live state. |
| High | Agent Workbench has durable planning/approval concepts, but the actual autonomous execution loop is incomplete. | `docs/CAPABILITY_MATRIX.md`, `docs/HANDOFF_BACKLOG.md`, Agent Host task/approval code | “Skai works on itself” cannot be reliable until model/tool dispatch, process isolation, artifact capture, restart recovery, and reviewable commit/push are one tested path. | Make the golden vertical loop the product spine before adding many more surfaces. |
| High | The product has two native UI histories. | `apps/desktop/`, `native/ServerRock.Native/`, `docs/ELECTRON_MIGRATION_CUTOVER.md` | Fixes can land in Uno/Core and not reach Electron, as happened with the pet grid; regressions become invisible behind passing native tests. | Declare Electron the first-class shell or explicitly keep both supported. Freeze or retire duplicate UX code after parity. |
| High | Packaging and platform readiness are incomplete. | `docs/CAPABILITY_MATRIX.md`, `docs/PRODUCTION_READINESS_GAPS.md`, release scripts/workflows | Windows development builds can pass while clean-machine installs, signed updates, profile migration, and Linux behavior fail. | Add release-candidate gates for installer lifecycle, clean profile, upgrade/recovery, Linux smoke, and packaged Electron UI. |
| High | OS-level execution isolation is weaker than the product’s local-agent ambition. | `README.md`, Agent Host terminal/worktree services, security/readiness docs | Bounded paths and approvals reduce risk but are not the same as a hardened process/container boundary for arbitrary tools and model-produced commands. | Define a threat model and enforce OS-backed process/worktree/network boundaries before enabling broader autonomous execution. |

## Important medium-priority gaps

- Renderer UI and layout state remain high-churn. `apps/desktop/src/styles.css`, `App.tsx`, and `useDesktopWorkspace.ts` carry shell, responsive, focus, route, and feature continuity concerns. The reference grid should become an explicit layout model with named zones and breakpoint contracts, not just CSS adjustments.
- Testing is strong in Core but weak at the live product boundary. Build/audit success can still coexist with a static marketplace or a pet regression. Add browser/Electron E2E and packaged acceptance before calling a surface complete.
- Image generation is ComfyUI-dependent. `docs/LOCAL_IMAGE_GENERATION.md` describes a native stable-diffusion.cpp direction, but real model download, generation, cancellation, persistence, and reload are not complete.
- JSON-backed state is appropriate for the local-first stage but needs explicit schema versions, migrations, backup/restore tests, and corruption recovery as the number of durable features grows. [TODO]
- `apps/desktop/package.json` uses `latest` dependency ranges. Reproducible desktop builds need pinned versions and a reviewed lockfile/update policy.
- There is no standard linter, formatter, renderer coverage threshold, structured logging contract, or dependency/security audit gate. [TODO]

## Fragile change surfaces

- `services/agent-host/Protocol/AgentHostLoop.cs` — centralized routing and response shaping.
- `apps/desktop/src/styles.css` — broad shell and feature styling.
- `apps/desktop/src/App.tsx` and `apps/desktop/src/app/useDesktopWorkspace.ts` — route/layout state continuity.
- `apps/desktop/electron/main.cjs` — host lifecycle, IPC allowlist, window policy, and pet motion.
- `docs/CAPABILITY_MATRIX.md`, `docs/GOAL_EXECUTION.md`, and other high-churn planning documents — useful intent records, but they should not be mistaken for runtime evidence.

## Recommended product heading

The best near-term direction is a reliable local “agent operating system” vertical slice, not more disconnected feature cards:

1. Chat creates a project-scoped task.
2. The agent produces a plan and requests explicit approval.
3. A supervised worker runs in an isolated worktree with bounded tools.
4. The UI streams activity and artifacts with durable restart/reconcile state.
5. The user reviews an exact diff, runs tests, and commits/pushes intentionally.
6. Marketplace can discover and install the skills/models/tools that the worker is actually capable of using.
7. The pet reflects real task state and wanders using the tested grid planner without interfering with the workspace.

That slice makes the “Skai can work on itself” goal measurable and gives every other surface a real runtime to represent.

## Intent versus current reality

- **Intent:** a feature-rich local marketplace with models, voices, images, skills, tools, and MCP. **Reality:** the current Electron marketplace is a local/static catalog; Core has useful lower-level pieces, but the host/UI contract for live provider polling and installation is missing.
- **Intent:** a self-improving local agent workspace. **Reality:** task/approval/artifact primitives exist, but complete model/tool dispatch and safe background execution are still partial.
- **Intent:** the contributed grid pet behavior. **Reality:** Core planner and tests exist, but Electron uses a separate random pixel implementation.
- **Intent:** a modern Electron product shell. **Reality:** Uno remains a substantial compatibility tree and contains behavior that is more mature than the live Electron path.
- **Intent:** broad desktop release readiness. **Reality:** Windows checks are comparatively strong; clean packaged lifecycle, Linux, and full UI acceptance remain open.

## Questions requiring product decisions

1. [ASK USER] Should Electron be the sole first-class product surface, with Uno frozen/retired after parity, or must both remain supported?
2. [ASK USER] Should Percy use grid-constrained movement inside the app workspace by default, or must the desktop-wide always-on-top mode remain the default even if it requires a separate geometry adapter?
3. [ASK USER] Is live remote marketplace discovery/install a near-term release requirement, or should the next milestone deliberately remain local-catalog-only while agent execution is completed?
4. [ASK USER] Is autonomous self-editing the next release spine, or is it a longer-term goal after marketplace and media breadth?

## Evidence

- `README.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/PET_OVERHAUL.md`
- `docs/HANDOFF_BACKLOG.md`
- `docs/PRODUCTION_READINESS_GAPS.md`
- `docs/ELECTRON_MIGRATION_CUTOVER.md`
- `docs/LOCAL_IMAGE_GENERATION.md`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/features/catalog/MarketplacePage.tsx`
- `services/agent-host/Protocol/AgentHostLoop.cs`
- `native/SkaiDesktop.Core/Services/PetWanderPlanner.cs`
- `native/SkaiDesktop.Core.Tests/PetWanderPlannerTests.cs`
