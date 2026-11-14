# Structure

## Top-level layout

- `.github/workflows/` — CI, acceptance, and release automation.
- `apps/desktop/` — Electron main/preload process, React renderer, features, shell components, assets, and desktop scripts.
- `services/agent-host/` — executable .NET JSON-lines host and protocol loop.
- `native/SkaiDesktop.Core/` — shared domain services, adapters, validators, persistence, provider clients, and policy logic.
- `native/SkaiDesktop.Core.Tests/` — focused .NET behavior and contract tests.
- `native/ServerRock.Native/` — legacy Uno/XAML desktop surface retained for compatibility and historical feature parity.
- `assets/` — pet sprites, branding, and other static media.
- `docs/` — product intent, capability matrix, migrations, release/readiness notes, and the generated codebase map.
- `scripts/` and `packaging/` — audits, smoke checks, packaging helpers, and release support.

## Runtime entry points

- Electron main: `apps/desktop/electron/main.cjs`.
- Electron preload: `apps/desktop/electron/preload.cjs`.
- React entry: `apps/desktop/src/main.tsx`.
- React composition: `apps/desktop/src/App.tsx`, `apps/desktop/src/app/AppRouteView.tsx`, and `apps/desktop/src/app/useDesktopWorkspace.ts`.
- Agent Host entry: `services/agent-host/Program.cs`, with request dispatch in `services/agent-host/Protocol/AgentHostLoop.cs`.
- Shared services: `native/SkaiDesktop.Core/Services/` and `native/SkaiDesktop.Core/Providers/`.
- Legacy Uno entry: `native/ServerRock.Native/App.xaml` and `MainPage.xaml`.

## Feature organization

The renderer is organized by product feature under `apps/desktop/src/features/`: chat, coding, catalog/marketplace, models, images, settings, workbench, terminal, memory, voice, and related surfaces. Cross-cutting layout and overlays live under `apps/desktop/src/components/`.

The Agent Host is more centralized than the renderer: most request routing and response shaping is concentrated in `AgentHostLoop.cs`, while the actual domain behavior is delegated to Core services. This makes the protocol boundary clear but makes the host loop a high-risk change surface.

## Important boundary

The intended boundary is Electron renderer → typed preload bridge → Agent Host → Core service/provider → local runtime or local persistence. The legacy Uno surface is outside the active Electron route and should not become a second source of product truth without an explicit decision. [ASK USER]

## Evidence

- `README.md`
- `apps/desktop/src/App.tsx`
- `apps/desktop/src/app/AppRouteView.tsx`
- `apps/desktop/electron/main.cjs`
- `apps/desktop/electron/preload.cjs`
- `services/agent-host/Protocol/AgentHostLoop.cs`
- `native/SkaiDesktop.Core/Services/`
- `native/ServerRock.Native/MainPage.xaml`
