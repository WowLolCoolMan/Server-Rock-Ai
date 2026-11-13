# Skai Desktop

<div align="center">
  <img src="assets/branding/skaiwolficon.png" width="128" alt="Skai Desktop icon" />
  <h1>Skai Desktop</h1>
  <p><strong>A local-first desktop workspace for chat, coding, models, projects, and media.</strong></p>
</div>

Skai Desktop brings your models, code, files, tools, and conversations into one Windows desktop app. Use local models when you want privacy, connect compatible hosted providers when you need them, and keep project work in the same place as the conversation.

## Why Skai

Most AI apps are built around a chat box. Skai is built around the work around the chat.

| Skai Desktop | Typical AI chat app |
| --- | --- |
| Local models and compatible providers in one model library | One provider or a long list of disconnected integrations |
| Project-scoped context, terminal work, Git review, approvals, and undo | Copy and paste between chat, editor, and terminal |
| Local persistence with no background telemetry | Account-first history and opaque data flows |
| Bounded file access and explicit write approval | Broad tool access with limited review |
| Image workflows and coding work beside normal chat | Separate apps for separate tasks |

## What it can do

- Chat with Ollama, LM Studio, LocalAI, llama.cpp, vLLM, and OpenAI-compatible endpoints.
- Discover and manage local models without leaving the app.
- Work inside a project with bounded context, a guarded terminal, reviewable file edits, undo history, and GitHub workflows.
- Attach text, PDFs, and images with local safety checks and size limits.
- Search the web only when you turn it on.
- Generate images through ComfyUI-compatible workflows.
- Use voice dictation and optional spoken replies on supported Windows systems.
- Keep conversations, projects, drafts, personas, and settings on the device.

## Current status

The Electron + React + TypeScript desktop surface is now the approved replacement
target for the Uno/.NET shell. Migration is staged behind a persistent .NET
agent host and shared Core services; the Uno app remains a legacy compatibility
surface until each required journey has equivalent host-backed acceptance. It
must not be described as the finished replacement yet.

## Run it

Prerequisites: .NET 10 SDK and Node.js.

```sh
# Legacy Uno compatibility surface
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop

# Tests
dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj

# Replacement desktop shell
npm run desktop:install
npm run desktop:dev
```

## Architecture

```text
Electron + React + TypeScript
        │ typed, sandboxed local IPC
        ▼
.NET agent host
        │ sessions · providers · tools · persistence
        ▼
Ollama · LM Studio · OpenAI-compatible APIs · ComfyUI · GitHub
```

See [`docs/ELECTRON_MIGRATION_CUTOVER.md`](docs/ELECTRON_MIGRATION_CUTOVER.md)
for the approved replacement boundary and [`docs/CAPABILITY_MATRIX.md`](docs/CAPABILITY_MATRIX.md)
for support details. The Uno surface is retained only until parity gates pass.

## Privacy and guardrails

- No background analytics or telemetry.
- Cloud providers receive only content you explicitly send.
- Project reads are bounded and exclude common secret/dependency paths.
- File writes require approval and retain guarded undo history.
- Credentials use platform storage where supported; they are not kept in normal settings files.

## Repository map

```text
apps/desktop/             Electron + React desktop surface
services/agent-host/      Persistent .NET host and protocol boundary
native/ServerRock.Core    Provider, persistence, workflow, and safety logic
native/ServerRock.Native  Legacy Uno/XAML compatibility application during cutover
docs/                     Architecture, capability, release, and feature docs
assets/                   Product visuals and Percy mascot assets
```

Useful docs:

- [`docs/FUTURE_FEATURES.md`](docs/FUTURE_FEATURES.md) — product backlog.
- [`docs/MODEL_LIBRARY.md`](docs/MODEL_LIBRARY.md) — model discovery and runtime notes.
- [`docs/LOCAL_IMAGE_GENERATION.md`](docs/LOCAL_IMAGE_GENERATION.md) — ComfyUI setup.
- [`docs/GITHUB_INTEGRATION.md`](docs/GITHUB_INTEGRATION.md) — project GitHub workflows.
- [`services/agent-host/README.md`](services/agent-host/README.md) — host protocol.
- [`docs/ELECTRON_MIGRATION_CUTOVER.md`](docs/ELECTRON_MIGRATION_CUTOVER.md) — replacement and retirement gates.

## Contributing

Keep changes incremental and testable. Before opening a pull request:

```sh
npm run check:syntax
npm run check:hygiene
npm test
npm run desktop:typecheck
npm run desktop:build
```

MIT licensed. Third-party notices are in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).
