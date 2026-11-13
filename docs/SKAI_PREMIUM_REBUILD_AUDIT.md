# Skai Desktop Premium Rebuild Audit

Date: 2026-11-13

## Scope

This audit covers the Electron renderer, its preload/main boundary, the persistent agent host, and the repository documentation that governs the migration. The pasted master implementation prompt is treated as a product and engineering brief; repository instructions remain authoritative for safe implementation.

## Exact file-level findings

| Area | Finding | Evidence | Priority |
| --- | --- | --- | --- |
| Renderer architecture | The entire React surface is concentrated in one component with inline state, transport calls, navigation, and view markup. | `apps/desktop/src/App.tsx` (44 KB, 147 lines; many very long JSX/function lines) | P0 |
| Renderer structure | There are no feature folders, shared primitives, hooks, or service modules under `apps/desktop/src`. | `apps/desktop/src/App.tsx`, `main.tsx`, `styles.css` | P0 |
| Styling | Global CSS is a single stylesheet with repeated visual values and no explicit token contract. | `apps/desktop/src/styles.css` | P0 |
| Transport boundary | Preload exposes one generic request method, which is secure but leaves protocol details embedded in the renderer. | `apps/desktop/electron/preload.cjs`, `apps/desktop/src/App.tsx` | P1 |
| Desktop shell | Electron main has a guarded host-process and file-picking boundary; it should remain stable while the renderer is migrated. | `apps/desktop/electron/main.cjs` | P1 |
| Runtime | The local agent host is a large protocol loop and should be wrapped by typed renderer services rather than called ad hoc. | `services/agent-host/Protocol/AgentHostLoop.cs`, `apps/desktop/shared/contracts.ts` | P1 |
| Verification | Syntax, hygiene, renderer typecheck, and renderer production build pass before this slice. | `npm run check:syntax`, `npm run check:hygiene`, `npm run desktop:typecheck`, `npm run desktop:build` | P0 |
| Product polish | The running UI has a broad feature surface but weak hierarchy: large empty chat space, dense controls, flat surfaces, and no responsive layout contract. | Live Electron capture, 1440x1200 viewport | P1 |
| Naming | Product identity is sourced through `PRODUCT_DISPLAY_NAME`; new work must not hardcode a brand name. | `apps/desktop/shared/productIdentity.ts`, `apps/desktop/src/App.tsx` | P0 |

## Design contract for the rebuild

- Preserve the local-first and approval-boundary behavior.
- Keep the agent-host and Electron security boundaries unchanged unless a slice requires a contract change.
- Use semantic design tokens for color, type, spacing, radii, elevation, motion, and density.
- Build from accessible primitives and feature components; page components compose features instead of owning transport details.
- Support dark, light, deep-sea, and deep-space backdrops through theme data attributes and reduced-motion behavior.
- Every visual slice must be checked at desktop and narrow widths, with keyboard focus and empty/loading/error states represented.
- Do not introduce a dependency solely for visual polish when a small local primitive is sufficient.

## Migration order

1. Establish renderer types, request service, design tokens, and shell primitives.
2. Extract navigation/history and the chat workspace into feature components without changing behavior.
3. Extract settings, projects, workbench, models, image studio, terminal, and memory one feature at a time.
4. Add the premium visual system and responsive states after the structural seams are stable.
5. Add new product capabilities only through typed contracts and independently verifiable vertical slices.

## Current slice acceptance

- The audit is checked into the repository.
- Renderer API access has one typed seam.
- Renderer design tokens exist in a dedicated module and are loaded before feature styles.
- Existing syntax, hygiene, typecheck, and production build remain green.
- Native/AirLLM dirty work is not staged or modified by this slice.
