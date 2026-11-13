# Modular monolith with isolated workers

## Process boundaries
1. Desktop shell: native window lifecycle, menu/tray, safe file dialogs, OS open-folder, updater integration; no agent authority in renderer.
2. Renderer/presentation: UI only, view models, accessibility, layout, read-only projections, explicit user actions.
3. Typed capability/IPC gateway: schema validation, caller identity, request IDs, rate limits, permissions, subscriptions, protocol versions.
4. Server Rock .NET Core: domain services, provider/model registry, workflow engine, context manager, permissions, package manager, workspace manager, storage, diagnostics.
5. Isolated workers: agent tools, PTYs, indexing, downloads, optional MCP processes; supervised lifecycle and bounded resources.
6. External providers: local models, remote APIs, ComfyUI/Hugging Face, GitHub, MCP servers, ACP agents.

## Core invariants
- UI never directly holds provider API keys or launches privileged commands.
- UI commands are typed and idempotent where applicable; backend emits sequenced events.
- Every process has a health endpoint/heartbeat, explicit shutdown and crash recovery.
- No arbitrary renderer-to-OS access; if Electron, contextIsolation, sandbox, strict CSP, no nodeIntegration, restricted navigation and validated preload API.
- All platform-dependent behavior behind interfaces. Avoid hardcoded paths.
- SQLite is authoritative for structured app state; human-readable Chat History exports and backups remain first-class. Use WAL carefully, migrations, backup API and corruption handling.
- Prefer one modular monolith, not microservices; isolated child processes only where isolation/reliability is justified.
