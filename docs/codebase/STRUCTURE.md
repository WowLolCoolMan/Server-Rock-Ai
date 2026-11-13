# Codebase Structure

## Core Sections (Required)

### 1) Top-Level Map

| Path | Purpose | Evidence |
|------|---------|----------|
| `native/ServerRock.Core/` | UI-independent models, persistence, providers, policies and workflow/runtime services | `native/ServerRock.Core/` |
| `native/ServerRock.Native/` | Uno/XAML desktop shell and feature partials | `native/ServerRock.Native/` |
| `native/ServerRock.Core.Tests/` | Executable Core/domain test harness | `native/ServerRock.Core.Tests/Program.cs` |
| `apps/desktop/` | Electron/React migration prototype | `apps/desktop/package.json`, `docs/ELECTRON_MIGRATION_STAGE1.md` |
| `services/agent-host/` | Persistent .NET agent-host protocol seam | `services/agent-host/README.md`, `services/agent-host/ServerRock.AgentHost.csproj` |
| `scripts/` | Build, audit, release and hygiene scripts | `package.json`, `scripts/` |
| `packaging/native/` | Transitional Windows/Linux packaging scripts and manifest | `packaging/native/README.md` |
| `docs/` | Architecture, evidence, capability and release documentation | `docs/` |

### 2) Entry Points

- Main native runtime: `native/ServerRock.Native/MainPage.xaml` and its `MainPage.*.cs` partials.
- Core test entry: `native/ServerRock.Core.Tests/Program.cs`.
- Migration renderer/host entry: `apps/desktop/src/` and `services/agent-host/Program.cs`.
- Script entry selection: root `package.json` scripts.

### 3) Module Boundaries

| Boundary | What belongs here | What must not be here |
|----------|-------------------|------------------------|
| `ServerRock.Core` | UI-independent policy, models, persistence, provider and workflow logic | XAML controls or platform-only visual state |
| `ServerRock.Native` | Native UI, dialogs, accessibility labels, UI event orchestration | Unbounded provider/persistence policy duplicated from Core |
| `Core.Tests` | Deterministic domain and integration-style fixture checks | Real user profile mutation or unapproved cloud calls |
| `apps/desktop` + `agent-host` | Incremental migration seam and typed local protocol | Replacing native parity without an owner decision |

### 4) Naming and Organization Rules

- C# types/files use PascalCase; native UI responsibilities are split into `MainPage.<Feature>.cs` partials.
- Services, models and infrastructure are grouped by responsibility under `native/ServerRock.Core/`.
- JavaScript scripts use descriptive kebab/camel-compatible filenames under `scripts/`; npm tasks are named `check:*`, `audit:*`, `build:*`, or `desktop:*`.

### 5) Evidence

- `README.md`
- `docs/ARCHITECTURE.md`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Core.Tests/Program.cs`
- Codebase scan command output from the required acquisition workflow
