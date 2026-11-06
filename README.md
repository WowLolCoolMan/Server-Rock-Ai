# Server-Rock-Ai

**Server Rock: AI**

A local-first desktop workspace for chatting with local models and compatible AI endpoints. It uses a compact, keyboard-friendly layout inspired by modern chat desktop apps.

## Native cross-platform app (target stack)

The repository includes a C# and XAML app in `native/ServerRock.Native`, built with Uno Platform, plus a UI-independent `native/ServerRock.Core` library for application models, provider transport, and settings storage. The UI uses a shared `net10.0-desktop` Skia target for Linux and desktop Windows plus a Windows App SDK target. Native Chat and Coding Agent have separate screens and recent-chat lists. Chat supports opt-in web search. Coding Agent reads bounded project context and proposes source-file replacements for review, approval, and guarded undo. Both use a configurable OpenAI-compatible endpoint with model discovery, streaming, cancellation, and local persistence.

This is an incremental migration, not feature parity. The native app saves conversations, selected text/code, PDF text, and image attachments, named projects, and selected folder paths locally. PDF text is extracted on-device and bounded before it is attached. Project files are read as context, then written only after explicit approval; common secret/dependency folders and symbolic links are excluded, edits have guarded undo, and the latest 20 approved changes are available in a local persistent undo history. Web search is opt-in and sends queries to DuckDuckGo and returned excerpts to the configured model endpoint. The Coding Agent now has a project-scoped interactive terminal and GitHub commit/push integration. The remaining migration gates are tracked in `docs/NATIVE_MIGRATION.md` and `docs/NATIVE_RELEASE_ACCEPTANCE.md`; unfinished areas must describe their current status accurately. Electron is retained only as a transitional compatibility and history-export path while those native gates are verified, and must not receive new product features.

The requested Windows 8 compatibility conflicts with the current .NET 10 target: Microsoft lists Windows 8.1 as unsupported for current .NET releases. Windows 10 support is edition/build-specific in Microsoft's current support table. Treat Windows 8 and general consumer Windows 10 support as unresolved compatibility requirements; do not advertise them based only on a successful compile. Evaluate a legacy runtime/package strategy or explain the support limit before release.

Build the shared desktop target with the .NET 10 SDK:

```sh
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj
```

On Linux, install the system GTK 3 and graphics dependencies documented by [Uno Platform](https://platform.uno/docs/articles/get-started-with-linux.html). The Skia Desktop target supports Linux desktop and Windows; the WinAppSDK target is Windows-only. Distros and architectures must be tested on actual runners before claiming support. See the [Uno platform matrix](https://platform.uno/docs/articles/getting-started/requirements.html) and Microsoft's [.NET on Windows support matrix](https://learn.microsoft.com/en-us/dotnet/core/install/windows).

## Features

- Windows, macOS, and Linux Electron desktop packaging.
- Conversation history and preferences stored in the local browser profile.
- Model picker scans known local hosts (Ollama, GPT4All, LM Studio, Jan, LocalAI, llama.cpp, vLLM, KoboldCpp, and Text Generation WebUI), lists available models, and shows measured per-model latency and throughput after use. It also lists models from a configured compatible/cloud endpoint.
- The native Model Library scans standard local endpoints across common model runtimes, keeps same-named models at different hosts distinct, and offers a pinned subset in the composer hotswapper. It shows per-model local response speed and first-token measurements as samples accumulate. Hugging Face GGUF discovery/download uses LM Studio, and Ollama Library pulls use Ollama. See [Model Library source and runtime support](docs/MODEL_LIBRARY.md) for setup and compatibility limits.
- Optional API keys use Electron's OS secure credential storage when available; credentials are never sent to Server Rock services. If secure storage is unavailable, the app refuses to save a key.
- Light and dark themes, attachment picker, and workspace chat history.
- Choose ChatBot or Coding Agent from the Codex-style mode switch. Project source folders are selected with the native folder picker; ChatBot can retrieve relevant local text passages from the selected project, and Coding Agent can inspect project files and save generated files back into that folder.
- Activity includes a terminal that starts in an authorized project folder. Commands run with the user's normal account permissions; live output, bounded input, cancellation, and resumable session snapshots stay in the desktop process, and secret-shaped environment variables are removed.
- Live response streaming with a Stop control for active provider, image, and local text-generation requests.
- Slash actions include `/fast`, `/plan`, `/goal`, `/status`, `/imagegen`, `/review`, `/summarize`, `/explain`, `/translate`, `/models`, `/clear`, and `/help`. Set a fast model in Settings for `/fast`; image generation requires a configured ComfyUI API workflow.
- Voice turn-taking with microphone speech recognition and spoken replies; click the mic again to interrupt audio and stop listening. Speech recognition depends on a speech service that Electron doesn't include, so in the desktop app the mic currently reports that voice input isn't available.
- Attach an image for offline captioning/OCR or local image-to-image processing with compatible downloaded ONNX models.
- No analytics or telemetry. Core release checks are user initiated; the app contacts a configured inference provider only after you send a prompt. Hugging Face is contacted only when you explicitly search the catalog or download a selected model.

## Run locally

For product development, run the native Uno/.NET target first:

```sh
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj
```

The Node/Electron commands below are retained only for legacy compatibility checks, migration export, and transitional packaging. `npm start` is native-first; use `npm run start:electron` only when you specifically need the transitional Electron client.

Install Node.js 22.12 or newer (required by the transitional Electron 45 path). From this repository:

```sh
npm install
npm start
```

`npm start` launches the Uno/.NET native desktop app through the .NET 10 SDK. Set `SERVER_ROCK_NATIVE_FRAMEWORK` when you need a specific target, such as the Windows App SDK framework on Windows. The transitional Electron client remains available with `npm run start:electron`; its install scripts and runtime are intentionally retained only for migration and compatibility work. `package.json` records, under `allowScripts`, that the install scripts of `onnxruntime-node` (optional CUDA GPU libraries; the app runs models on the CPU), `protobufjs` (a version warning), and `electron-winstaller` (only used for Squirrel installers, which this app doesn't build) are not needed. npm 11 and newer then skip them without a warning.

Configure a provider using the gear button. Ollama is commonly available at `http://localhost:11434/v1/chat/completions`; GPT4All's local API is `http://127.0.0.1:4891/v1/chat/completions`. Start the local host first, scan for models in the model picker, and select an installed model.

## Build legacy Electron installers

These commands package the transitional Electron compatibility build. They are not the target native release workflow.

```sh
npm run build:win   # NSIS installer and portable executable
npm run build:mac   # DMG and ZIP
npm run build:linux # AppImage, DEB, and tar.gz
```

GitHub Actions builds Windows x64, macOS Intel x64 and Apple Silicon arm64, and Linux x64 and arm64. Push a version tag such as `v0.2.0` to attach platform artifacts and a `SHA256SUMS.txt` checksum list to a GitHub Release. A manual workflow run builds artifacts without publishing a release. These repository release assets are build outputs for the owner; customer downloads and updates use the public Server Rock Core flow below, not direct GitHub release links. Updates are user initiated, with no background check.

The Windows NSIS wizard supports a per-user install, a selectable destination, and optional desktop/Start Menu shortcuts. The portable executable runs without installation. Windows' Apps & Features entry launches the included uninstaller. Uninstall preserves the user's Server Rock: AI profile by default; remove app data separately if you also want to erase local conversations and settings. Linux AppImage is a portable single file; DEB integrates with Debian/Ubuntu package tools, and tar.gz is a generic archive. macOS DMG installs by dragging the app to Applications; ZIP is an alternative archive. Builds are unsigned unless release signing certificates are configured, so Windows SmartScreen or macOS Gatekeeper may warn users.

## Server Rock Core free distribution and updates

The app includes a user-initiated Core updater. In **Settings → Server Rock Core downloads and updates**, choose Stable or Beta and check for a release without signing in or buying a Server Rock entitlement. The website download and app update client do not send account credentials. A download becomes available only after Core publishes the installer and the required signed channel manifest; optional account-based features elsewhere in the app keep their own sign-in flow.

Core signs a platform-specific release manifest with Ed25519. The app pins Core's public key, verifies the signed manifest and its exact Core-hosted artifact URL, then downloads to a private app-data folder and verifies SHA-256 before showing an operating-system installer prompt. The updater does not silently replace a running installation: Windows opens the per-user NSIS installer, macOS opens the DMG, and Linux launches the verified AppImage so the user can place it at their usual install path. Profiles and local conversation data remain separate from these installer steps. Rollback is offered only for the prior version explicitly named in the current signed channel manifest.

The tagged build workflow accepts `vX.Y.Z` stable and `vX.Y.Z-beta.N` prerelease tags. Configure the GitHub repository variables `SERVER_ROCK_AI_CORE_ORIGIN` and `SERVER_ROCK_UPDATE_PUBLIC_KEY_B64` before building a release; CI fails closed when either is absent. The public key must match Core's Ed25519 manifest signer. Never put Core's private signing key in this repository.

Core release publication is a separate owner action. Copy each finished platform artifact into Core's private `SERVER_ROCK_PRIVATE_INSTALLER_DIRECTORY` using `scripts/stage_server_rock_ai_release.py`, then publish its emitted manifest through Core's authenticated `/api/admin/releases` workflow. Core serves immutable versioned artifacts only after signature configuration and the staged SHA-256 match. For a later version, the manifest must name the previous published version as `rollback_version`. This repository cannot confirm the Core production directory, signing key, owner API credentials, Google OAuth setup, GitHub variables, or whether a release has been published; those must be checked in the actual environments before distribution is described as live.

The app has no automatic background check, native Windows/macOS code signing or macOS notarization in this updater slice, and no unattended self-replacement. Core's signed-manifest and SHA-256 checks detect artifact substitution when the Core signing key remains trusted; changing/rotating that key requires a coordinated app release. Stable website downloads appear only after a stable artifact is staged and published in Core. Beta is for desktop updater users; it is not listed as a stable website download. No paid AI entitlement or telemetry is added.

The manual **Native desktop artifacts** workflow now launches the exact published Windows and Linux binaries for a bounded startup smoke window before archiving them. This catches missing runtime files or immediate process crashes in the artifact itself; it does not replace installer, accessibility, or full workflow acceptance.

## Providers and privacy

The chat client uses the OpenAI-compatible `/chat/completions` streaming shape and supports the Anthropic Messages API. Hugging Face provider mode searches the Hub by task. Chat and text generation use the compatible Router endpoint; other selected tasks call the task inference endpoint. Text-to-image and image-to-image accept Hugging Face inference, with image-to-image using an attached source image. Catalog search sends a query and selected task to Hugging Face only when Search is clicked. Local servers should bind to loopback and allow the app origin. Cloud API providers receive the content you submit, under their own policies. An optional ChatGPT plan connection uses the supported Sign in with ChatGPT authorization flow and keeps its tokens in the Electron main process using OS-backed encryption. Claude Code is available through the official locally installed `claude` CLI bridge: sign in through Claude Code's own terminal flow, then select the Claude Code provider; Server Rock: AI does not request, store, or forward an Anthropic API key. Direct Claude.ai consumer OAuth is not exposed to third-party apps, so the bridge is intentionally CLI-based. Never paste account cookies or session tokens here.

To enable the Claude Code bridge, install the official CLI (`npm install -g @anthropic-ai/claude-code`), run `claude` once, and complete its Claude account sign-in. Return to Server Rock: AI, choose **Claude Code** in Settings, and use **Open Claude Code sign-in** whenever the CLI needs reauthorization. The bridge removes API-key-shaped environment variables before launching the CLI and never imports its credential files into the app.

Local history and non-secret settings are stored in Electron's app profile using browser local storage. Provider API keys and ChatGPT authorization tokens use OS-backed Electron safeStorage; on systems where encryption is unavailable, API-key saving is disabled. Clear app data through the operating system's app data controls to remove local conversations and settings. Offline chat/inference works with a local provider and already downloaded model. Hub search and downloads require a connection. A cloud endpoint, cloud model, or hosted image generation service sends the request data to that provider.

Image generation can use a ComfyUI API-format workflow pasted into Settings (replace `{{prompt}}` in text inputs), or Hugging Face Inference text-to-image when Hugging Face is the selected provider and task. Hub discovery is broad and task-filtered, but “all models” cannot run in one universal runtime: architecture, task, weights, dependencies, hardware, access gating, license, and inference provider determine compatibility. The built-in local downloader supports compatible ONNX exports. The task selector exposes compatible Transformers.js text, vision, and audio pipelines, including classification, question answering, embeddings, image captioning/editing, segmentation, detection, depth maps, background removal, speech recognition, audio classification, and text-to-speech. The app maps selected task names to local pipelines and requires compatible ONNX exports; not every Hub model or architecture can run in this runtime. Downloads require an explicit button click and are capped at 10 GB/300 files. Image/audio attachments are limited to 20 MB. Models that require Python-only dependencies or GPU-specific runtimes should use a matching local host such as ComfyUI or an OpenAI-compatible server. Models may be gated or have individual license restrictions.

## Project structure

- `electron/main.cjs`: desktop window and native menu.
- `index.html`, `style.css`, `app.js`: interface, styling, local history, and provider client.
- `.github/workflows/release.yml`: tagged release packaging.
- `docs/PRODUCTION_READINESS_GAPS.md`: prioritized remaining migration, release, parity, accessibility, and polish work.
