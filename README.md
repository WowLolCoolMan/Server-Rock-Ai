# Server-Rock-Ai

**Server Rock: AI**

A local-first desktop workspace for chatting with local models and compatible AI endpoints. It uses a compact, keyboard-friendly layout inspired by modern chat desktop apps.

## Native cross-platform app (migration in progress)

The repository includes a C# and XAML app in `native/ServerRock.Native`, built with Uno Platform, plus a UI-independent `native/ServerRock.Core` library for application models, provider transport, and settings storage. The UI uses a shared `net10.0-desktop` Skia target for Linux and desktop Windows plus a Windows App SDK target. Native Chat and Coding Agent have separate screens and recent-chat lists. Chat supports opt-in web search. Coding Agent reads bounded project context and proposes source-file replacements for review, approval, and guarded undo. Both use a configurable OpenAI-compatible endpoint with model discovery, streaming, cancellation, and local persistence.

This is an incremental migration, not feature parity. The native app saves conversations, selected text/code, PDF text, and image attachments, named projects, and selected folder paths locally. PDF text is extracted on-device and bounded before it is attached. Project files are read as context, then written only after explicit approval; common secret/dependency folders and symbolic links are excluded, edits have guarded undo, and the latest 20 approved changes are available in a local persistent undo history. Web search is opt-in and sends queries to DuckDuckGo and returned excerpts to the configured model endpoint. The Coding Agent now has a project-scoped interactive terminal and GitHub commit/push integration, but project-wide search/indexing, export/import, Electron history migration, audio/spreadsheet workflows, image generation, connectors, skills/automations, hosted-provider credentials, and production installers still need to be ported and verified. See `docs/NATIVE_MIGRATION.md` for the capability map. The Electron app remains the complete product and its packaged builds are still the supported distribution. Native controls for unfinished areas must describe their current status accurately. Do not remove the Electron app or user data until those workflows and migration have been verified.

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
- Live response streaming with a Stop control for active provider, image, and local text-generation requests.
- Slash actions include `/fast`, `/plan`, `/goal`, `/status`, `/imagegen`, `/review`, `/summarize`, `/explain`, `/translate`, `/models`, `/clear`, and `/help`. Set a fast model in Settings for `/fast`; image generation requires a configured ComfyUI API workflow.
- Voice turn-taking with microphone speech recognition and spoken replies; click the mic again to interrupt audio and stop listening. Speech recognition availability and whether transcription is offline depend on the installed Electron/OS speech service.
- Attach an image for offline captioning/OCR or local image-to-image processing with compatible downloaded ONNX models.
- No analytics, telemetry, or background update checks. The app contacts a configured inference provider only after you send a prompt. Hugging Face is contacted only when you explicitly search the catalog or download a selected model.

## Run locally

Install Node.js 20 or newer. From this repository:

```sh
npm install
npm start
```

Configure a provider using the gear button. Ollama is commonly available at `http://localhost:11434/v1/chat/completions`; GPT4All's local API is `http://127.0.0.1:4891/v1/chat/completions`. Start the local host first, scan for models in the model picker, and select an installed model.

## Build installers

```sh
npm run build:win   # NSIS installer and portable executable
npm run build:mac   # DMG and ZIP
npm run build:linux # AppImage, DEB, and tar.gz
```

GitHub Actions builds Windows x64, macOS Intel x64 and Apple Silicon arm64, and Linux x64 and arm64. Push a version tag such as `v0.2.0` to attach all platform files and a `SHA256SUMS.txt` checksum list to one GitHub Release. A manual workflow run builds downloadable artifacts without publishing a release. The website can link to the latest GitHub Release or list its attached assets. The site deployment source is not included here. Updates are currently user initiated: download and install the newer version; the app does not check for updates in the background.

The Windows NSIS wizard supports a per-user install, a selectable destination, and optional desktop/Start Menu shortcuts. The portable executable runs without installation. Windows' Apps & Features entry launches the included uninstaller. Uninstall preserves the user's Server Rock: AI profile by default; remove app data separately if you also want to erase local conversations and settings. Linux AppImage is a portable single file; DEB integrates with Debian/Ubuntu package tools, and tar.gz is a generic archive. macOS DMG installs by dragging the app to Applications; ZIP is an alternative archive. Builds are unsigned unless release signing certificates are configured, so Windows SmartScreen or macOS Gatekeeper may warn users.

## Free distribution

Push a version tag such as `v0.1.0` to build installers and attach them to a GitHub Release. Updates are currently user initiated: download and run the newer installer. The app does not check for updates in the background. Link the Windows installer and other platform builds from `www.server-rock.com`; the site repository/hosting configuration is not present in this workspace. The app contains no paid feature gates or telemetry.

## Providers and privacy

The chat client uses the OpenAI-compatible `/chat/completions` streaming shape and supports the Anthropic Messages API. Hugging Face provider mode searches the Hub by task. Chat and text generation use the compatible Router endpoint; other selected tasks call the task inference endpoint. Text-to-image and image-to-image accept Hugging Face inference, with image-to-image using an attached source image. Catalog search sends a query and selected task to Hugging Face only when Search is clicked. Local servers should bind to loopback and allow the app origin. Cloud API providers receive the content you submit, under their own policies. An optional ChatGPT plan connection uses the supported Sign in with ChatGPT authorization flow and keeps its tokens in the Electron main process using OS-backed encryption. Claude.ai consumer sign-in is not available to third-party clients; use an Anthropic API key from the API Console (Claude subscriptions and API billing are separate). Never paste account cookies or session tokens here.

Local history and non-secret settings are stored in Electron's app profile using browser local storage. Provider API keys and ChatGPT authorization tokens use OS-backed Electron safeStorage; on systems where encryption is unavailable, API-key saving is disabled. Clear app data through the operating system's app data controls to remove local conversations and settings. Offline chat/inference works with a local provider and already downloaded model. Hub search and downloads require a connection. A cloud endpoint, cloud model, or hosted image generation service sends the request data to that provider.

Image generation can use a ComfyUI API-format workflow pasted into Settings (replace `{{prompt}}` in text inputs), or Hugging Face Inference text-to-image when Hugging Face is the selected provider and task. Hub discovery is broad and task-filtered, but “all models” cannot run in one universal runtime: architecture, task, weights, dependencies, hardware, access gating, license, and inference provider determine compatibility. The built-in local downloader supports compatible ONNX exports. The task selector exposes compatible Transformers.js text, vision, and audio pipelines, including classification, question answering, embeddings, image captioning/editing, segmentation, detection, depth maps, background removal, speech recognition, audio classification, and text-to-speech. The app maps selected task names to local pipelines and requires compatible ONNX exports; not every Hub model or architecture can run in this runtime. Downloads require an explicit button click and are capped at 10 GB/300 files. Image/audio attachments are limited to 20 MB. Models that require Python-only dependencies or GPU-specific runtimes should use a matching local host such as ComfyUI or an OpenAI-compatible server. Models may be gated or have individual license restrictions.

## Project structure

- `electron/main.cjs`: desktop window and native menu.
- `index.html`, `style.css`, `app.js`: interface, styling, local history, and provider client.
- `.github/workflows/release.yml`: tagged release packaging.
