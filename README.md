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

The Uno/.NET desktop app is the current functional product. The Electron + React + TypeScript desktop surface is being added incrementally around a persistent .NET agent host. The new shell already has typed IPC, runtime health, model discovery, chat completion, settings, and navigation; it is not yet a full replacement for the native app.

## Run it

Prerequisites: .NET 10 SDK and Node.js.

```sh
# Native app
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop

# Tests
dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj

# New desktop shell
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

The native application remains available during the migration. See [`docs/ELECTRON_MIGRATION_STAGE1.md`](docs/ELECTRON_MIGRATION_STAGE1.md) for the current desktop boundary and [`docs/CAPABILITY_MATRIX.md`](docs/CAPABILITY_MATRIX.md) for support details.

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
native/ServerRock.Native  Current Uno/XAML desktop application
docs/                     Architecture, capability, release, and feature docs
assets/                   Product visuals and Percy mascot assets
```

Useful docs:

- [`docs/FUTURE_FEATURES.md`](docs/FUTURE_FEATURES.md) — product backlog.
- [`docs/MODEL_LIBRARY.md`](docs/MODEL_LIBRARY.md) — model discovery and runtime notes.
- [`docs/LOCAL_IMAGE_GENERATION.md`](docs/LOCAL_IMAGE_GENERATION.md) — ComfyUI setup.
- [`docs/GITHUB_INTEGRATION.md`](docs/GITHUB_INTEGRATION.md) — project GitHub workflows.
- [`services/agent-host/README.md`](services/agent-host/README.md) — host protocol.

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
