# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands

- Primary test framework: executable .NET test harness with custom `Check` helpers; no xUnit/NUnit package was found.
- Assertion/mocking tools: `TestSupport`, temporary filesystem fixtures and stub HTTP handlers.
- Commands:

```bash
dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj --no-restore
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore
npm run audit:native-ui
npm run check:hygiene
```

### 2) Test Layout

- Tests live in `native/SkaiDesktop.Core.Tests/`, one focused `*Tests.cs` area per domain.
- `Program.cs` registers the suite in explicit order and prints pass evidence.
- Native visible-control contracts are checked by `scripts/audit-native-ui.cjs`; live Windows evidence is recorded separately in `docs/GOAL_EXECUTION.md`.

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|----------------|-------|
| Unit | yes | Core policies, models, planners, validators | Broad deterministic suite |
| Integration | partial | Stub HTTP, temp stores, Git/MCP/workflow fixtures | Real remote providers are not used by Core tests |
| E2E | partial | Native Windows inspection and artifact smoke scripts | Linux, clean-install and full accessibility evidence remain `[TODO]` |

### 4) Mocking and Isolation Strategy

- Network boundaries use stub handlers or local fake responses; filesystem tests use unique temp paths and clean them up.
- Tests avoid the real user profile and do not silently send prompts to cloud services.
- Common failure mode: concurrent `dotnet` builds can contend for output files; run Core and native builds serially.

### 5) Coverage and Quality Signals

- Coverage tool/threshold: `[TODO]` no numeric coverage gate is configured.
- Current evidence: Core suite, native build, UI audit, syntax, hygiene and release-config gates pass for the current `main` checkout when run serially.
- Known gaps: Linux runtime/accessibility, live popup focus bridge, clean-machine package lifecycle, signed artifacts and some migration-surface parity.

### 6) Evidence

- `native/SkaiDesktop.Core.Tests/Program.cs`
- `native/SkaiDesktop.Core.Tests/TestSupport.cs`
- `scripts/audit-native-ui.cjs`
- `.github/workflows/native-ci.yml`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
