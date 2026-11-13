# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| Files | C# types use PascalCase; feature partials use `MainPage.<Feature>.cs` | `MainPage.ModelLibrary.cs` | `native/ServerRock.Native/` |
| Functions/methods | PascalCase for C# methods; private fields use `_camelCase` | `BuildProviderMessages`, `_settings` | `native/ServerRock.Native/MainPage.Chat.cs` |
| Types/interfaces | PascalCase; records are used for bounded immutable contracts | `ModelComparisonPlan` | `native/SkaiDesktop.Core/Services/ModelComparisonPolicy.cs` |
| Constants/env vars | C# constants use PascalCase; environment names are uppercase | `MaxPromptCharacters`, `RELEASE_SIGNING_READY` | `native/SkaiDesktop.Core/`, `scripts/validate-release-config.cjs` |

### 2) Formatting and Linting

- Formatter: `[TODO]` no repository formatter configuration was detected by the scan.
- Linter: `[TODO]` no root linter configuration was detected.
- Enforced gates: compiler/nullable checks, Node syntax, native UI contract audit, repository hygiene, and release metadata validation.
- Run commands: `npm run check:syntax`, `npm run check:hygiene`, `npm run audit:native-ui`, `npm run check:release-config`.

### 3) Import and Module Conventions

- C# files use explicit namespaces and standard `using` directives; implicit usings are enabled in `native/Directory.Build.props`.
- Core services are referenced through project boundaries; no generated-source or barrel-export convention was observed.
- UI partials share `MainPage` state but feature behavior should remain in the owning partial.

### 4) Error and Logging Conventions

- Core rejects invalid input with bounded `ArgumentException`/`InvalidDataException` or structured result types.
- Native surfaces expose truthful recovery text and log unexpected failures through `App.Log`.
- Sensitive values are redacted by diagnostics and kept out of ordinary settings; provider credentials use session/credential mechanisms rather than plaintext settings.

### 5) Testing Conventions

- Tests are named `*Tests.cs` under `native/SkaiDesktop.Core.Tests/` and are registered explicitly in `Program.cs`.
- Tests use deterministic temp paths, stub HTTP handlers and local fixtures; no coverage threshold was found.
- New behavior should add a focused Core check and a native audit contract when a visible control or route changes.

### 6) Evidence

- `native/Directory.Build.props`
- `native/SkaiDesktop.Core.Tests/Program.cs`
- `scripts/audit-native-ui.cjs`
- `scripts/check-repo-hygiene.cjs`
