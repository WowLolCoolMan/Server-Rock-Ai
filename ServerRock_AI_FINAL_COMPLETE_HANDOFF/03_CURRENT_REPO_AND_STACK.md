# Codebase baseline, architectural decision, and recheck plan

## Provenance

Historical baseline: GitHub `WowLolCoolMan/Server-Rock-Ai`, `main`, SHA `4ad39cfb89ddf97cd1ddd4533c4b879d8124ac95` (committed 2026-11-07 UTC). This was inspected during creation of the earlier premium handoff. It is **not a live-worktree audit**. Developer must re-fetch HEAD and identify uncommitted and local-only changes on each contributor's machine.

The repository README, `docs/ARCHITECTURE.md`, `docs/NATIVE_MIGRATION.md`, `docs/PRODUCTION_READINESS_GAPS.md` and `DESIGN.md` show:

- `native/ServerRock.Native`: **Uno Platform** XAML desktop app; shared `net10.0-desktop` Skia runtime and Windows App SDK target. Platform-specific glue requires guarded implementations; Linux runtime acceptance is not equivalent to a cross-RID publish.
- `native/SkaiDesktop.Core`: UI-independent C# provider transport, workspace file operations, history/profile storage, GitHub, local models, skills catalog. Preserve this separation.
- `native/SkaiDesktop.Core.Tests`: core console test harness. Reuse and expand, including interaction tests and deterministic fake adapters.
- `native/ServerRock.Native/MainPage.*`: numerous feature partial classes and dialogs; **architectural hotspot** for state, UI, orchestration and navigation. Decompose incrementally by view models/services without changing current user-visible behavior unexpectedly.
- `native/ServerRock.Native/App.xaml` and `UiTheme.cs`: native design tokens; `DESIGN.md` governs consistent styling.
- `scripts/audit-native-ui.cjs`: static UI contract checks, not runtime GUI or focus proof. Windows CI historical string-match fragility needs re-verification, not an assumed still-open bug.
- `.github/workflows/native-*.yml`, `packaging/native`, `docs/NATIVE_RELEASE_ACCEPTANCE.md`: release/test framework with explicit missing signing, installer, live Linux distro and upgrade acceptance gates.
- Core currently contains bounded local file context, reviewed file changes, undo history, project terminal, OpenAI-compatible providers, model library discovery, opt-in web search, ComfyUI, GitHub integration and partial native skills catalog. Verify exact capability state before reimplementing.

## Architecture target (native first)

```text
Windows/Linux desktop shell (Uno / XAML)
  ├── App navigation, design tokens, keyboard/focus, docking, accessibility
  ├── Screen ViewModels using MVVM + immutable/observable state
  └── Scoped UI commands, typed progress, error and approval surfaces
                  │ service contracts / DI
                  ▼
SkaiDesktop.Core (UI-free)
  ├── Projects / Sessions / Conversations / Artifacts
  ├── AgentHost / Scheduler / Workflows / Permissions / EventJournal
  ├── ProviderRegistry / Router / CapabilityDiscovery / LocalHardware
  ├── Workspace / Git / Diff / Terminal / File / Undo / Recovery
  ├── MCP / ACP / Codex App Server adapters [capability-gated]
  ├── Skills catalog / manifests / package verification / updates
  └── Persistence / settings / redacted logs / migration / backup
                  │ interfaces
                  ▼
OS platform services (Windows | Linux)
  ├── PTY and process lifetime; credential store
  ├── native file dialogs, notifications, clipboard
  ├── installer/update verification, OS sandbox where supported
  └── GPU / memory / runtime discovery
```

## Keep implementation honest

- Prefer working local backend services and feature flags over mock integration cards.
- Introduce interfaces and DI only where needed to break coupling, enable tests or support multiple providers; avoid abstract interfaces with no runtime consumer.
- A long-running agent host can be an in-process service initially **only if** host lifecycle, cancellation and crash recovery are explicit; isolated child process is recommended when needed for untrusted tools, independent UI restarts and fault containment.
- Keep secrets in supported keyrings and support Linux keyring-unavailable states without plaintext fallback.
- JSON currently stores existing user profiles: migrations to SQLite are a **proposal**, not permission to delete/replace data. Implement migration with read-only preview, backups and version-aware rollback.
- Target OS support must be specified per Windows version, Linux distribution, architecture and packaging format with real artifacts and evidence.

## Initial commands (read-only first)

```sh
pwd
git status --short
git rev-parse HEAD
git branch --show-current
dotnet --info
```

```sh
# Build / test only after environment and working tree have been understood:
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
npm run audit:native-ui
```

Run UI on **both** target platforms where possible. The source pack contains fuller platform validation and release docs; use actual current checkout commands if they changed.
