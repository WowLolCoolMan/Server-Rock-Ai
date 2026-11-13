# Electron migration — Stage 1

Stage 1 establishes a parallel desktop migration seam without replacing the
working Uno application.

## Implemented boundary

```text
Electron main process
  ├─ sandboxed React renderer (context-isolated preload bridge)
  └─ local .NET Agent Host (versioned JSON-lines stdio protocol)
       └─ SkaiDesktop.Core reference (existing domain boundary)
```

The renderer can request `runtime.health` through the preload bridge. The main
process owns the child-process lifecycle and does not expose Node.js to the
renderer. The host rejects unsupported protocol versions and unknown methods.

## Current scope

- `apps/desktop`: Electron + React + TypeScript prototype shell.
- `services/agent-host`: .NET 10 local host with protocol version 1.
- `apps/desktop/shared/contracts.ts`: renderer-side protocol types.
- `apps/desktop/electron/preload.cjs`: narrow context-isolated bridge.
- `apps/desktop/electron/main.cjs`: host lifecycle and request correlation.

This slice does not duplicate chat, model discovery, persistence, or agent
execution. The next slice should adapt existing Core provider and settings
services behind host-owned application contracts, then add a real chat request
and streaming event path.

## Verification evidence

- Agent host build: passed with 0 warnings and 0 errors.
- Manual stdio health request: returned `agent-host`, protocol version `1`, and
  `ready` status.
- Renderer TypeScript check: passed.
- Renderer production build: passed.
- Electron runtime: launched a real window, rendered the shell, and displayed
  `Agent host connected` / `READY` from the live host handshake.
- Existing Core tests: passed.
- Existing native UI audit: currently fails on two pre-existing uncommitted
  focus-contract changes in `MainPage.Infrastructure.cs` and
  `MainPage.Sidebar.cs`; those files were not modified by this slice.

## Rollback

Remove `apps/desktop`, `services/agent-host`, the Stage 1 root scripts, and this
document. The existing Uno projects and their data paths are unchanged.
