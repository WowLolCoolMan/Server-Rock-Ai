# Architecture

## Core Sections (Required)

### 1) Architectural Style

- Primary style: layered, feature-split native UI with UI-independent Core services and adapter boundaries.
- Why: `ServerRock.Core` owns models/services/infrastructure, while `ServerRock.Native` composes them through partial UI feature files; the migration adds a typed host seam rather than replacing the native app.
- Primary constraints: preserve local-first routing, keep approvals around side effects, and bound filesystem/network/persistence input.

### 2) System Flow

```text
MainPage/Composer -> BuildProviderMessages -> Core policies + local stores -> OpenAI-compatible/provider adapter -> throttled UI response + persisted session
```

1. Native composer events enter `MainPage.Chat.cs`.
2. `BuildProviderMessages` adds mode, attachment, project, memory and persona context under explicit guards.
3. Core policy/validation classes bound content and classify routes before transport.
4. `OpenAiCompatibleProvider` streams deltas through the configured endpoint.
5. Native rendering updates the conversation and local stores; operation/activity records retain local evidence for agent paths.

### 3) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| Core models | Durable contracts and bounded data shapes | UI lifecycle | `native/ServerRock.Core/Models/` |
| Core infrastructure | File bounds, archives, stores, context assembly | XAML controls | `native/ServerRock.Core/Infrastructure/` |
| Core services | Provider, Git, workflow, skill, MCP and policy behavior | Direct visual rendering | `native/ServerRock.Core/Services/` |
| Native partials/views | User interaction, dialogs, focus and accessibility | Duplicated trust-boundary validation | `native/ServerRock.Native/MainPage.*.cs`, `*.xaml` |
| Agent host | Versioned JSON-lines seam for migration/runtime hosting | Silent privilege escalation | `services/agent-host/` |

### 4) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|---------------|
| Adapter/provider | `OpenAiCompatibleProvider`, `WebSearchProvider`, `GitHubApiClient` | Isolate external transports and error policies |
| Store/recovery | `JsonSettingsStore`, `ConversationStore`, `ProjectStore` | Atomic bounded local persistence with corrupt-file recovery |
| Policy/guard | `ChatAttachmentPolicy`, `ModelComparisonPolicy`, `WorkflowCapabilityGate` | Validate before side effects |
| Durable journal | `OperationService`, workflow services | Reconcile interrupted work without replaying side effects |

### 5) Known Architectural Risks

- The native shell remains a large partial-class surface with high recent churn; refactors must preserve event wiring and accessibility contracts.
- Electron/agent-host migration is not feature-parity complete; both surfaces must not be treated as interchangeable without live evidence.
- Linux packaged/runtime acceptance is `[TODO]` because the local WSL environment lacks `dotnet`.

### 6) Evidence

- `docs/ARCHITECTURE.md`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Core/Services/OpenAiCompatibleProvider.cs`
- `native/ServerRock.Core/Infrastructure/JsonSettingsStore.cs`
- `services/agent-host/README.md`
