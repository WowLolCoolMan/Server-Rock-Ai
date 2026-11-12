# Server-Rock-Ai

**Server Rock: AI**

A local-first desktop workspace for chatting with local models and compatible AI endpoints. It uses a compact, keyboard-friendly layout inspired by modern chat desktop apps.

## Native cross-platform app (target stack)

The repository includes a C# and XAML app in `native/ServerRock.Native`, built with Uno Platform, plus a UI-independent `native/ServerRock.Core` library for application models, provider transport, and settings storage. The UI uses a shared `net10.0-desktop` Skia target for Linux and desktop Windows plus a Windows App SDK target. Native Chat and Coding Agent have separate screens and recent-chat lists. Chat supports opt-in web search. Coding Agent reads bounded project context and proposes source-file replacements for review, approval, and guarded undo. Both use a configurable OpenAI-compatible endpoint with model discovery, streaming, cancellation, and local persistence.

The native app saves conversations, selected text/code, PDF text, and image attachments, named projects, and selected folder paths locally. PDF text is extracted on-device and bounded before it is attached. Project files are read as context, then written only after explicit approval; common secret/dependency folders and symbolic links are excluded, edits have guarded undo, and the latest 20 approved changes are available in a local persistent undo history. Web search is opt-in and sends queries to DuckDuckGo and returned excerpts to the configured model endpoint. The Coding Agent has a project-scoped interactive terminal and GitHub commit/push integration. Native limitations and release gates are tracked in `docs/NATIVE_MIGRATION.md` and `docs/NATIVE_RELEASE_ACCEPTANCE.md`.

For a free, token-saving project check, open a project and use `/health` for a bounded offline health scan or `/map` for a compact architecture/dependency map. These commands inspect filenames and small local text samples, never upload the repository, and let you add only the compact report to a prompt when you want model help.

The requested Windows 8 compatibility conflicts with the current .NET 10 target: Microsoft lists Windows 8.1 as unsupported for current .NET releases. Windows 10 support is edition/build-specific in Microsoft's current support table. Treat Windows 8 and general consumer Windows 10 support as unresolved compatibility requirements; do not advertise them based only on a successful compile. Evaluate a legacy runtime/package strategy or explain the support limit before release.

Build the shared desktop target with the .NET 10 SDK:

```sh
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj
```

On Linux, install the system GTK 3 and graphics dependencies documented by [Uno Platform](https://platform.uno/docs/articles/get-started-with-linux.html). The Skia Desktop target supports Linux desktop and Windows; the WinAppSDK target is Windows-only. Distros and architectures must be tested on actual runners before claiming support. See the [Uno platform matrix](https://platform.uno/docs/articles/getting-started/requirements.html) and Microsoft's [.NET on Windows support matrix](https://learn.microsoft.com/en-us/dotnet/core/install/windows).

## Features

- Uno/.NET native desktop app for shared Skia desktop targets and Windows App SDK.
- Conversation history and preferences stored locally in your platform application-data folder.
- Model picker scans known local hosts (Ollama, GPT4All, LM Studio, Jan, LocalAI, llama.cpp, vLLM, KoboldCpp, and Text Generation WebUI), lists available models, and shows measured per-model latency and throughput after use. It also lists models from a configured compatible/cloud endpoint.
- The native Model Library scans standard local endpoints across common model runtimes, keeps same-named models at different hosts distinct, and offers a pinned subset in the composer hotswapper. It shows per-model local response speed and first-token measurements as samples accumulate. Hugging Face GGUF discovery/download uses LM Studio, and Ollama Library pulls use Ollama. See [Model Library source and runtime support](docs/MODEL_LIBRARY.md) for setup and compatibility limits.
- Native GitHub credentials use the platform credential mechanism; hosted-provider credential flows are intentionally not enabled until their native privacy and recovery UX is complete.
- Light and dark themes with selectable accent colors, attachment picker, and workspace chat history.
- Choose ChatBot or Coding Agent from the Codex-style mode switch. Project source folders are selected with the native folder picker; ChatBot can retrieve relevant local text passages from the selected project, and Coding Agent can inspect project files and save generated files back into that folder.
- Activity includes a terminal that starts in an authorized project folder. Commands run with the user's normal account permissions; live output, bounded input, cancellation, and resumable session snapshots stay in the desktop process, and secret-shaped environment variables are removed.
- Live response streaming with a Stop control for active provider, image, and local text-generation requests.
- Slash actions include `/fast`, `/plan`, `/goal`, `/status`, `/imagegen`, `/review`, `/summarize`, `/explain`, `/translate`, `/models`, `/clear`, and `/help`. Set a fast model in Settings for `/fast`; image generation requires a configured ComfyUI API workflow.
- Voice turn-taking is model-agnostic: the Windows build uses the system speech recognizer for dictation and speech synthesizer for spoken responses, so it works with every selected local, hosted, or OpenRouter-compatible model. Dictation is inserted as a reviewable draft before sending, and spoken output is opt-in.
- Attach an image for offline captioning/OCR or local image-to-image processing with compatible downloaded ONNX models.
- No analytics or telemetry. Core release checks are user initiated; the app contacts a configured inference provider only after you send a prompt. Hugging Face is contacted only when you choose its catalog in the Model Library (which opens on your installed models), search it, or download a selected model.

## Run locally

For product development, run the native Uno/.NET target first:

```sh
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj
```

Node is used only for lightweight repository checks; the product itself is built and run with the .NET 10 SDK. From this repository:

```sh
npm install
npm start
```

`npm start` launches the Uno/.NET native desktop app through the .NET 10 SDK. Set `SERVER_ROCK_NATIVE_FRAMEWORK` when you need a specific target, such as the Windows App SDK framework on Windows. `npm install` is intentionally dependency-free and exists only to provide a consistent Node-based check entrypoint.

Configure a provider using the gear button. Ollama is commonly available at `http://localhost:11434/v1/chat/completions`; GPT4All's local API is `http://127.0.0.1:4891/v1/chat/completions`. Start the local host first, scan for models in the model picker, and select an installed model.

## Build native release artifacts

Use the `Native desktop artifacts` or `Build native desktop release artifacts` GitHub Actions workflow to publish self-contained Windows and Linux bundles with SHA-256 sidecars. These are portable artifacts, not installers; signing, clean installation, upgrade, and rollback remain explicit release gates.

## Server Rock Core free distribution and updates

The app includes a user-initiated Core updater. In **Settings → Server Rock Core downloads and updates**, choose Stable or Beta and check for a release without signing in or buying a Server Rock entitlement. The website download and app update client do not send account credentials. A download becomes available only after Core publishes the installer and the required signed channel manifest; optional account-based features elsewhere in the app keep their own sign-in flow.

Native release bundles include a versioned Windows install/upgrade/rollback contract under `packaging/native`. It keeps profiles under `%LOCALAPPDATA%\ServerRock\AI`, retains the prior version for rollback, and never deletes user data unless the user explicitly confirms it. Tagged CI releases additionally require real Authenticode and Linux GPG signing secrets; missing signing material fails the release before publication.

Core signs a platform-specific release manifest with Ed25519. The app pins Core's public key, verifies the signed manifest and its exact Core-hosted artifact URL, then downloads to a private app-data folder and verifies SHA-256 before showing an operating-system installer prompt. The updater does not silently replace a running installation: Windows opens the per-user NSIS installer, macOS opens the DMG, and Linux launches the verified AppImage so the user can place it at their usual install path. Profiles and local conversation data remain separate from these installer steps. Rollback is offered only for the prior version explicitly named in the current signed channel manifest.

The tagged build workflow accepts `vX.Y.Z` stable and `vX.Y.Z-beta.N` prerelease tags. Configure the GitHub repository variables `SERVER_ROCK_AI_CORE_ORIGIN` and `SERVER_ROCK_UPDATE_PUBLIC_KEY_B64` before building a release; CI fails closed when either is absent. The public key must match Core's Ed25519 manifest signer. Never put Core's private signing key in this repository.

Core release publication is a separate owner action. Copy each finished platform artifact into Core's private `SERVER_ROCK_PRIVATE_INSTALLER_DIRECTORY` using `scripts/stage_server_rock_ai_release.py`, then publish its emitted manifest through Core's authenticated `/api/admin/releases` workflow. Core serves immutable versioned artifacts only after signature configuration and the staged SHA-256 match. For a later version, the manifest must name the previous published version as `rollback_version`. This repository cannot confirm the Core production directory, signing key, owner API credentials, Google OAuth setup, GitHub variables, or whether a release has been published; those must be checked in the actual environments before distribution is described as live.

The app has no automatic background check, native Windows/macOS code signing or macOS notarization in this updater slice, and no unattended self-replacement. Core's signed-manifest and SHA-256 checks detect artifact substitution when the Core signing key remains trusted; changing/rotating that key requires a coordinated app release. Stable website downloads appear only after a stable artifact is staged and published in Core. Beta is for desktop updater users; it is not listed as a stable website download. No paid AI entitlement or telemetry is added.

The manual **Native desktop artifacts** workflow now launches the exact published Windows and Linux binaries for a bounded startup smoke window before archiving them. This catches missing runtime files or immediate process crashes in the artifact itself; it does not replace installer, accessibility, or full workflow acceptance.

## Providers and privacy

The chat client uses the OpenAI-compatible `/chat/completions` streaming shape. Native connection setup includes OpenRouter at `https://openrouter.ai/api/v1` with the `openrouter/free` automatic free-model router. OpenRouter still requires the user's own API key and applies its free-tier rate limits; the native client keeps that key session-only and never writes it to settings. Local servers should bind to loopback and allow the app origin. Cloud endpoints receive the content you submit, under their own policies. No account cookies or browser session tokens are imported.

A Claude Code bridge is not part of the native app yet; the native Settings screen has no Claude Code option.

Native history and non-secret settings are stored in the user's platform application-data directory using bounded JSON stores. Native GitHub credentials use the platform credential mechanism. Clear app data through the operating system's app-data controls to remove native conversations and settings. Offline chat/inference works with a local provider and already downloaded model. Hub search and downloads require a connection. A cloud endpoint, cloud model, or hosted image generation service sends the request data to that provider.

Image generation can use a ComfyUI API-format workflow pasted into Settings (replace `{{prompt}}` in text inputs), or Hugging Face Inference text-to-image when Hugging Face is the selected provider and task. Hub discovery is broad and task-filtered, but “all models” cannot run in one universal runtime: architecture, task, weights, dependencies, hardware, access gating, license, and inference provider determine compatibility. The built-in local downloader supports compatible ONNX exports. The task selector exposes compatible Transformers.js text, vision, and audio pipelines, including classification, question answering, embeddings, image captioning/editing, segmentation, detection, depth maps, background removal, speech recognition, audio classification, and text-to-speech. The app maps selected task names to local pipelines and requires compatible ONNX exports; not every Hub model or architecture can run in this runtime. Downloads require an explicit button click and are capped at 10 GB/300 files. Image/audio attachments are limited to 20 MB. Models that require Python-only dependencies or GPU-specific runtimes should use a matching local host such as ComfyUI or an OpenAI-compatible server. Models may be gated or have individual license restrictions.

## Project structure

- `native/ServerRock.Native`: Uno/XAML desktop UI and platform shell.
- `native/ServerRock.Core`: provider adapters, persistence, model library, projects, workspace tools, and migration parsing.
- `.github/workflows/native-release.yml`: tagged native release packaging.
- `docs/PRODUCTION_READINESS_GAPS.md`: prioritized remaining migration, release, parity, accessibility, and polish work.
- `docs/FUTURE_FEATURES.md`: the broader future-feature backlog to review before choosing the next bounded implementation slice.
