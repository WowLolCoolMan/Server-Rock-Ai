# Conventions

## TypeScript/React

- Components use PascalCase filenames and exported component names.
- Hooks use the `use...` prefix.
- CSS classes use kebab-case and are centralized primarily in `apps/desktop/src/styles.css`.
- TypeScript is strict and uses bundler module resolution with no emit.
- Imports are predominantly relative; no repository-wide path alias contract was found.

## C#

- Types, methods, and files use PascalCase; locals and parameters use camelCase.
- Nullable reference types and implicit usings are enabled in the primary projects.
- Services are generally named by domain responsibility and live under `native/SkaiDesktop.Core/Services`.
- Tests use `*Tests.cs` files and a lightweight executable-style harness rather than a conventional test framework in the main Core test project.

## Errors and logging

- Agent Host catches request failures and returns structured error responses rather than allowing the JSON-lines process to die for ordinary request errors.
- Core uses bounded exceptions and validator results for invalid paths, capabilities, permissions, and state transitions.
- Electron main logs host lifecycle and protocol information with prefixed console messages.
- There is no single structured logging, correlation-ID, retention, or redaction contract shared across Electron, Agent Host, and Core. [TODO]

## Tests and checks

- C# checks print explicit pass/fail output and are invoked through npm scripts.
- Node audits are small repository scripts for syntax, hygiene, runtime policy, local-only behavior, release workflow, and native UI contracts.
- There is no repository lint configuration, formatter configuration, coverage threshold, or renderer E2E test standard. [TODO]

## Evidence

- `apps/desktop/tsconfig.json`
- `apps/desktop/src/`
- `apps/desktop/src/styles.css`
- `native/SkaiDesktop.Core/SkaiDesktop.Core.csproj`
- `native/SkaiDesktop.Core.Tests/`
- `services/agent-host/Protocol/AgentHostLoop.cs`
- `apps/desktop/package.json`
