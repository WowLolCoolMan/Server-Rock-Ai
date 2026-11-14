# Skai Desktop

<div align="center">
  <img src="assets/branding/skaiwolficon.png" width="128" alt="Skai Desktop wolf icon" />
  <h1>Skai Desktop</h1>
  <p><strong>A local-first desktop workspace for chat, coding, projects, and local AI models.</strong></p>
  <p>Private by default. Built to keep useful work on your own computer.</p>
</div>

<p align="center">
  <img src="assets/branding/skai-desktop-hero.png" alt="Skai Desktop workspace illustration" />
</p>

Skai Desktop is an Electron + React desktop application with a persistent .NET Agent Host. It brings local model chat, project files, guarded terminal work, task review, GitHub workflows, settings, memory, and creative tools into one focused workspace—not a hosted chat wrapper or a collection of disconnected mock screens.

> **Status:** active development · current release line: `0.1.x` · Electron is the product surface under active development. Capability claims below describe the current synchronized `main` branch; unfinished ideas remain in the [future feature backlog](docs/FUTURE_FEATURES.md).

## The product boundary

Skai Desktop is **local-only** for AI runtime and application data:

- No cloud model execution, hosted provider routing, cloud sync, or remote fallback.
- Prompts and attachments stay on this computer and are sent only to a local runtime selected by the user.
- GitHub is an explicit project integration for repository context and guarded Git workflows; it is not an AI model provider.
- Local services such as Ollama, LM Studio, LocalAI, llama.cpp, vLLM, OpenAI-compatible loopback servers, and ComfyUI can be connected by the user.

## What Skai is today

| Surface | Current state |
| --- | --- |
| **Chat** | Persistent conversations, attachments, queued follow-ups, Markdown/code rendering, retry, fork, pin, archive, rename, folders, tags, split view, search, and local memory controls. |
| **Coding Workspace** | Project-scoped browsing, bounded previews, editing, reviewable diffs, guarded terminal commands, undo, task planning, and focused project context. |
| **Agent Workbench** | Durable local task plans, dependency-aware readiness, activity filters, cancellation, approvals, and reviewable task state. Autonomous dispatch is still in progress. |
| **Scheduled work** | Local one-time and repeating schedules with pause/resume/cancel and reviewed Run now controls. No unattended provider request is implied. |
| **Models** | Local provider discovery, model library, pinned models, capability checks, comparison, explicit unavailable-model recovery, and no silent model switching. |
| **Workspace shell** | Resizable sidebar, persistent navigation state, command palette, settings search, responsive layouts, in-app documentation, and Deep Sea/Deep Space appearance presets. |
| **Local integrations** | Guarded GitHub repository workflows, local ComfyUI-compatible image workflows, local attachments, and Percy, the optional Skai wolf companion. |

The [capability matrix](docs/CAPABILITY_MATRIX.md) is the source of truth for working, partial, unverified, and planned areas. The project favors small, tested vertical slices over static mock screens, with screenshots and interaction evidence recorded in [`docs/SKAI_PREMIUM_UI_PROGRESS.md`](docs/SKAI_PREMIUM_UI_PROGRESS.md).

## Why Skai

| Skai Desktop | Typical AI chat app |
| --- | --- |
| Local models in one discoverable library | One provider and disconnected tools |
| Project-scoped context, files, terminal, and task review | Copy and paste between chat, editor, and terminal |
| Persistent local conversations and drafts | Account-first history with opaque data flows |
| Bounded reads and explicit write approval | Broad tool access with limited review |
| Chat, coding, models, images, memory, and settings in one shell | Separate apps for separate steps |

## Architecture

```text
Electron + React + TypeScript
        │ typed, sandboxed local IPC
        ▼
.NET Agent Host
        │ sessions · local providers · tools · persistence
        ▼
Ollama · LM Studio · AirLLM · local OpenAI-compatible APIs · ComfyUI
```

The Electron renderer is the product UI and the only active desktop shell. The isolated preload bridge exposes only typed desktop operations. The persistent .NET Agent Host and shared Core services own local model requests, project boundaries, persistence, approvals, and guarded file/terminal operations. The older Uno/XAML surface is frozen compatibility code: it remains buildable until parity and rollback evidence are complete, but new product behavior must land in Electron and shared Core.

## Run locally

Prerequisites:

- Windows 10/11
- .NET 10 SDK
- Node.js 20+
- A local model runtime such as Ollama or LM Studio (optional for UI work)

```sh
# Install desktop dependencies
npm run desktop:install

# Start the Electron + React desktop app
npm run desktop:dev

# Or build the renderer and open the desktop shell
npm run desktop:build
npm run open:desktop

# Run the native Core checks
dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
```

## Verification

Before opening a pull request, run the checks that match your change:

```sh
npm run check:syntax
npm run check:hygiene
npm run audit:electron-runtime
npm run audit:local-only
npm test
npm run desktop:typecheck
npm run desktop:build
```

The local-only audit is intentional: it rejects hosted/cloud model routes from the shipped Electron and Agent Host runtime.

## Repository map

```text
apps/desktop/              Electron + React desktop surface
services/agent-host/       Persistent .NET host and protocol boundary
native/SkaiDesktop.Core/   Local provider, persistence, workflow, and safety logic
native/ServerRock.Native/  Legacy Uno/XAML compatibility surface during migration
assets/                    Skai branding and Percy mascot assets
docs/                      Architecture, capability, release, and feature documentation
```

Useful docs:

- [`docs/SKAI_PREMIUM_UI_PROGRESS.md`](docs/SKAI_PREMIUM_UI_PROGRESS.md) — visual migration ledger and evidence.
- [`docs/FUTURE_FEATURES.md`](docs/FUTURE_FEATURES.md) — future product backlog.
- [`docs/CAPABILITY_MATRIX.md`](docs/CAPABILITY_MATRIX.md) — implemented, partial, and planned capabilities.
- [`docs/ELECTRON_MIGRATION_CUTOVER.md`](docs/ELECTRON_MIGRATION_CUTOVER.md) — Electron replacement boundary and retirement gates.
- [`docs/GITHUB_INTEGRATION.md`](docs/GITHUB_INTEGRATION.md) — repository project workflows.
- [`services/agent-host/README.md`](services/agent-host/README.md) — Agent Host protocol notes.

## What is next

The next work is deliberately visible rather than implied: complete the local task execution path, deepen editor and diff workflows, finish remaining keyboard/focus and packaged acceptance checks, validate Linux support with a supported .NET environment, improve retrieval, and add more local integrations. See the capability matrix for evidence-backed status instead of assuming every item in the product backlog is shipped.

## Contributing

Keep changes incremental, local-first, and testable. Preserve the Electron → isolated IPC → .NET Agent Host boundary, avoid exposing secrets to the renderer, do not add cloud model routes, and include validation evidence for user-facing changes.

MIT licensed. Third-party notices are in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).
