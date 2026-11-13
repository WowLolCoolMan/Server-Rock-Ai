# ADR-002: Electron renderer feature boundaries

Status: Accepted
Date: 2026-11-13

## Context

The Electron replacement is the approved migration target, but `apps/desktop/src/App.tsx` currently owns navigation, persistence, transport, feature state, and all page markup. This makes UI iteration risky and makes the premium rebuild difficult to validate in slices.

## Decision

Split the renderer by responsibility:

- `src/app`: application composition, route/view state, and shared renderer types.
- `src/services`: typed request wrappers around `window.serverRockAgent`.
- `src/components`: accessible visual primitives and shell components.
- `src/features/<name>`: feature state and view composition for chat, projects, workbench, models, image studio, terminal, memory, and settings.
- `src/design-system`: tokens, theme contracts, and reusable style foundations.

The migration will be incremental. Existing behavior remains the compatibility contract, and the Electron main/preload and agent-host boundaries are not broadened as part of visual refactoring.

## Consequences

The first slices add files and indirection, but they make feature ownership explicit, reduce the blast radius of UI changes, and let visual and behavior tests target bounded surfaces. The existing monolith can be retired only after each feature has been moved and verified.
