# Native app migration plan

## Intended architecture

- **UI:** Uno Platform XAML. Use shared views/styles and progressive disclosure: chat, model selection, project context, and attachments stay obvious; provider configuration, generation controls, diagnostics, and tool permissions appear when relevant.
- **Application and domain logic:** C# on the current .NET LTS. Keep conversation, project, model/provider, and coding-agent behavior independent from UI controls.
- **Provider adapters:** C# `HttpClient` adapters for local OpenAI-compatible servers first, then Anthropic/Hugging Face and other compatible services. Keep hosted credentials out of view models and config files.
- **Platform services:** small C# interfaces with per-platform implementations for encrypted credential storage, file selection, process launch, shell execution, notifications, and packaging.
- **Developer tooling:** .NET SDK for the product. Node.js remains for the current Electron app, packaging, and browser UI checks until migration is complete. Python is optional for model/runtime tools where the selected upstream runtime requires it, not a required desktop dependency.
- **Migration:** keep the Electron product available until native workflows, user data migration, and release artifacts meet the acceptance checks below.

The initial boundary is implemented as `ServerRock.Core` (plain `net10.0` library) referenced by `ServerRock.Native`. The XAML view calls provider and settings services; it no longer owns HTTP request construction or settings file operations. Keep future application services in Core and isolate OS-specific capabilities behind interfaces/adapters.

## Implemented native slice

- Uno XAML desktop shell, compiled for the shared Skia desktop and Windows App SDK targets.
- Local OpenAI-compatible chat endpoint and model ID saved under the current user's application data directory.
- Model discovery from `/v1/models` (or `/models` when the endpoint already ends in `/v1`).
- Streaming `/chat/completions`, generation temperature, stop/cancel, and readable connection errors.
- Project instructions are stored in settings and included in chat context. The project name is currently session-only; native project persistence and file reading/writing are not implemented yet.
- Core service smoke checks exercise endpoint normalization, model discovery, SSE text and content-part deltas, request serialization, provider errors, and settings persistence. Run with `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj`.

No native response may be simulated. A backend feature that is not connected must be labelled as not available or still migrating.

## Feature parity and acceptance

| Area | Native status | Acceptance before Electron retirement |
|---|---|---|
| Local model connection | First vertical slice implemented | Discover, select, stream, cancel, reconnect, timeout, and recover from malformed/error responses against representative Ollama, LM Studio, Jan, GPT4All, and LocalAI servers |
| Chats and history | Not implemented | Durable conversations, rename/delete/search, new chat, context isolation, export/import, and migration from Electron local data |
| Projects | Single session project plus persisted instructions | Multiple persisted projects, folder picker, instructions, safe context retrieval, project switching, and clear active-project UI |
| Coding Agent | Not implemented | Read/write workspace with user review, diffs, safe path boundaries, explicit commit/push review, cancellation, command permissions, and GitHub OAuth/repository flow |
| Providers and credentials | Local endpoint only | Hosted provider settings and token flows; OS-backed secret storage; tests showing secrets never enter logs or ordinary settings |
| Attachments and multimodal | Not implemented | File/image/audio picker, size/type limits, model capability checks, honest unsupported-state UI |
| Advanced controls | Temperature persisted | Per-model capability-aware controls, validation, reset/defaults, understandable descriptions, settings persistence |
| Accessibility and responsive UI | Initial shell | Keyboard-only flows, screen-reader names, focus handling, scaling, narrow windows, high contrast, light/dark themes |
| Packaging and updates | Build targets only | Signed/reproducible Windows and Linux installers, clean install/uninstall, upgrade/data retention, release checksums, verified runtime dependencies |

## Platform compatibility gate

Build and run CI/manual smoke checks on each release candidate. Keep results separate by operating system, distribution, architecture, package format, and runtime version.

| Requested platform | Current evidence/status |
|---|---|
| Debian | Not yet run on Linux |
| Ubuntu | Not yet run on Linux |
| Arch Linux | Not yet run on Linux |
| Fedora | Not yet run on Linux |
| Windows 11 | Windows target compiles on the current development machine; runtime smoke test pending |
| Windows 10 | Must verify supported editions/builds and the actual UI runtime; current .NET 10 support is edition/build-specific |
| Windows 8 / 8.1 | Incompatible with the currently selected supported .NET runtime. Investigate a separately maintained legacy strategy or explicitly narrow the supported OS requirement; never infer runtime support from compilation |

The Linux runner must verify display server and graphics dependencies, install/launch/uninstall behavior, model-server connectivity, and writable user-data paths. Prefer publishing self-contained builds where practical, but self-contained publishing does not by itself make an unsupported OS/runtime combination supported.

## Smooth development loop

For each feature, identify the real user outcome and states first (empty, loading, ready, failed, cancelled, permission denied). Check official platform/provider documentation and existing upstream implementations before choosing a protocol. Implement a complete vertical slice through XAML, app/domain service, and platform boundary. Use explicit status and error messages, keyboard and accessibility checks, then run the affected checks and both native compile targets. Record commands, actual results, known gaps, and the next bounded slice. Preserve unrelated working-tree changes. Never commit, push, publish, or delete user data automatically.
