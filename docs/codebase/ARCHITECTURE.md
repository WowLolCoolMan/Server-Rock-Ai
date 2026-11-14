# Architecture

## Intended architecture

Skai is a local-first desktop workspace. Electron owns the window lifecycle and the secure IPC boundary. React owns the product UI and route state. The Agent Host owns durable, policy-aware application operations. Core owns domain behavior and local provider adapters. Local services provide inference, image generation, speech, GitHub actions, and other explicitly configured capabilities.

## Request flow

1. Electron starts the main window and the persistent Agent Host process.
2. The preload exposes a small typed request surface to the renderer; Node APIs are not exposed directly to React.
3. Renderer hooks such as `useDesktopWorkspace` call approved request methods.
4. `AgentHostLoop` parses JSON-lines requests, checks the allowed method set and request shape, then delegates to Core.
5. Core validates project paths, permissions, hashes, provider capabilities, cancellation, and local-only policy before touching state or a runtime.
6. The response returns through stdio and preload to renderer state.

## Architectural patterns

- Adapter/provider boundaries for local runtimes and external integrations.
- Explicit capability preflight and “no silent provider switching” model selection.
- Durable JSON-backed stores for conversations, projects, tasks, schedules, approvals, journals, and profile state.
- Approval/DAG/worktree abstractions for guarded agent work.
- Bounded path, command, request-size, timeout, and secret-redaction policies.
- Separate standalone Electron BrowserWindow for the desktop pet.

## Pet architecture and regression

The Core layer contains a tested `PetWanderGrid`, `PetWanderPlanner`, and `PetWalk` implementation. It models a 10×10 grid, blocked rectangles, king-move paths, clamping, deterministic randomness, glide timing, and facing. The current Electron pet does not use it. `apps/desktop/electron/main.cjs` instead selects a random free-form pixel target and moves the BrowserWindow directly with `setPosition`.

This is an integration regression, not an algorithm gap. The contributed grid behavior exists in Core and has extensive tests, but the live Electron product path bypasses it. The next architecture should expose one shared planner/adapter contract to the Electron window controller, including work-area, composer, resize, DPI, monitor, drag, hide/minimize, and reduced-motion inputs. An equivalent TypeScript/Node port is acceptable only if its behavior is contract-tested against the Core planner; two independently evolving wander implementations are not.

## Main architectural risks

- Electron and legacy Uno contain overlapping shell and pet concepts, creating two possible product truths.
- `AgentHostLoop.cs` is a large orchestration boundary; a new feature can be implemented in Core but remain unreachable until a method, preload contract, renderer hook, and UI state are all added.
- Marketplace/domain capability is present in Core, but the current Electron marketplace is a static local catalog and the Agent Host has no corresponding marketplace request family.
- Durable agent records and approvals exist, but the actual model/tool execution loop is not yet a complete vertical path.

## Recommended target shape

Make the Electron shell the only first-class product surface, preserve Core as the domain source of truth, and make every user-visible capability travel through a typed request/response contract. Treat the UI as a projection of runtime state rather than a catalog of aspirational cards. Use a single golden vertical loop—chat → plan → approval → isolated edit → diff → test → commit/push—as the release spine. Marketplace discovery/install and the pet grid should plug into that same contract discipline.

## Evidence

- `README.md`
- `native/SkaiDesktop.Core/Services/PetWanderPlanner.cs`
- `native/SkaiDesktop.Core.Tests/PetWanderPlannerTests.cs`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/src/components/overlays/PetWindow.tsx`
- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/src/features/catalog/MarketplacePage.tsx`
- `docs/CAPABILITY_MATRIX.md`
