# Target architecture — incremental refactor

## Projects and responsibilities
- Native (Uno): views, navigation, dialogs, view models, OS integration adapters; no persistence/business logic in click handlers.
- Core: domain models, services, workflow state machine, provider adapters, skill/MCP interfaces, storage abstraction, diagnostics, permissions.
- Core.Tests: domain, contract, persistence, migration, security and resilience tests.
- Optional test harness: platform-specific UI automation, packaging smoke and fault injection.

## Interfaces (illustrative, adapt to existing contracts)
`IAppPaths`, `IStorageLayoutService`, `IConversationRepository`, `ICredentialStore`, `IProviderRegistry`, `IModelRouter`, `IAgentExecutionService`, `IOperationJournal`, `IApprovalService`, `ISkillRegistry`, `ISkillInstaller`, `ISkillRouter`, `IMcpConnectionManager`, `IDiagnosticsService`, `IPlatformIntegration`.

## Data flow
UI Intent -> typed command -> preflight/permission/context -> preview -> explicit approval when needed -> executor -> journal -> verification -> result/recovery -> persisted history -> UI projection.

## Design constraints
- DI boundaries and cancellation tokens across async calls.
- Atomic writes for user data; schema versions and migrations.
- No direct secret logging or user-provided shell command interpolation.
- UI updates dispatched safely; state derived from observable operation records.
- Platform services abstracted; no Windows-only APIs in Core.
- Add seams gradually; do not replace entire MainPage until tests cover behavior.
- Use adapters around supported SDKs; keep third-party implementation behind interfaces.
