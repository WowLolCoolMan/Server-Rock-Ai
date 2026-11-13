# Implementable foundation: domains, dependencies, native integration

## Current and target
Keep `ServerRock.Native` (Uno/XAML shell) → `SkaiDesktop.Core` (app/domain services) → platform adapters/providers; `SkaiDesktop.Core.Tests` tests independent services. Extract the existing giant `MainPage.*` feature partials **incrementally** into view-models and application services once behavior tests exist. Do not rewrite all UI at once. Uno XAML with standard desktop controls is the main interface; no embedded React/Electron/Tauri framework by default.

## Proposed packages/assemblies (refactor boundaries, not mandate for new projects immediately)
- **ServerRock.Domain:** ids, tasks, state machine, capability/permission types, value objects, domain events; no IO, no UI.
- **ServerRock.Application:** use-case handlers, projection generation, orchestration, approval and replay coordination.
- **SkaiDesktop.Core:** existing providers, workspaces, storage, tooling; progressively adapt as modules.
- **ServerRock.Platform:** `IPlatformServices` for PTY, keyring, window activation, sandbox, notifications, file dialogs, process signals; Windows/Linux implementations.
- **ServerRock.Native:** shell, layout/views, view models, navigation state, accessible components, dialog coordinator.
- **ServerRock.Tests/Integration:** unit/contract/process tests, seeded fixtures and fault injection.

## No UI-to-filesystem mutation bypass
UI sends a typed request (`start_run`, `preview_write`, `request_tool_approval`, etc.) to application services. Service validates workspace authorization, provider permissions and cancellation, journals the intent/result and projects status for UI. UI never directly applies arbitrary tool output or shell instructions by manipulating files.

## Suggested core interfaces
`IWorkspaceRepository`, `IConversationRepository`, `ITaskRepository`, `IEventJournal`, `IApprovalService`, `IExecutionScheduler`, `IAgentAdapter`, `IProviderRegistry`, `IModelRouter`, `IToolExecutor`, `IWorkspaceFileService`, `IWorktreeManager`, `ITerminalService`, `ISkillRegistry`, `ISkillInstaller`, `IMcpManager`, `ISecretStore`, `IStorageService`, `IUpdateService`, `IAppNavigator`, `IOverlayCoordinator`, `IDiagnosticExporter`.

## Dependency rules
- Domain must not reference Uno, HTTP, PTY, databases or platform APIs.
- Core/application must not reference XAML controls or implicitly start privileged OS operations.
- No circular feature references; communication via typed commands/events and DI.
- Provider adapters own HTTP protocol parsing; cannot write workspace files themselves.
- Skill host and MCP host cannot create authorization grants; they inherit the run's capability envelope.
- UI reads immutable state projections; use dispatcher-safe updates and cancellation propagation, not static mutable shared fields.
- Use feature folders and view models, not one giant Settings dialog with hundreds of click handlers.

## Composition and lifetime
App startup creates DI container, configuration, storage migration validation and session manager. Each run owns cancellation scope, model provider connection, tool budget and audit correlation ID. Each terminal owns a supervised process/session. Shell and UI windows subscribe to disposable state streams; no orphan event listeners. Closing a window should not accidentally kill operations if the chosen run policy allows them to outlive the UI.

## Integration tests for architecture
Verify Core loads headlessly on Windows/Linux; UI can be recreated without duplicating a running task; provider failures do not crash shell; all command handlers reject invalid ids/cross-workspace paths; shutdown disposes all IPC streams, child processes and timers.
