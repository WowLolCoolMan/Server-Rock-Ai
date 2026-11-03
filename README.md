# Server-Rock-Ai

**Server Rock: AI**

A local-first desktop workspace for chatting with local models and compatible AI endpoints. It uses a compact, keyboard-friendly layout inspired by modern chat desktop apps.

## Features

- Windows, macOS, and Linux Electron desktop packaging.
- Conversation history and preferences stored in the local browser profile.
- Model picker scans known local hosts (Ollama, LM Studio, Jan, LocalAI, llama.cpp, vLLM, KoboldCpp, and Text Generation WebUI), lists available models, and shows measured per-model latency and throughput after use. It also lists models from a configured compatible/cloud endpoint.
- Optional API key for endpoints that require one. Keys are stored in the local browser profile; avoid shared Windows accounts.
- Light and dark themes, attachment picker, and workspace chat history.
- Choose Chatbot or Coding Agent from the Codex-style mode switch. Coding Agent mode can create named projects and add local source folders with the native folder picker; folder paths are kept in the local app profile.
- Live response streaming with a Stop control for active provider, image, and local text-generation requests.
- Slash actions include `/fast`, `/plan`, `/goal`, `/status`, `/imagegen`, `/review`, `/summarize`, `/explain`, `/translate`, `/models`, `/clear`, and `/help`. Set a fast model in Settings for `/fast`; image generation requires a configured ComfyUI API workflow.
- Voice turn-taking with microphone speech recognition and spoken replies; click the mic again to interrupt audio and stop listening. Speech recognition availability and whether transcription is offline depend on the installed Electron/OS speech service.
- Attach an image for offline image captioning or OCR with a compatible downloaded `image-to-text` ONNX model.
- No analytics, telemetry, or background update checks. The app contacts a configured inference provider only after you send a prompt. Hugging Face is contacted only when you explicitly search the catalog or download a selected model.

## Run locally

Install Node.js 20 or newer. From this repository:

```sh
npm install
npm start
```

Configure a provider using the gear button. Ollama is commonly available at `http://localhost:11434/v1/chat/completions`; the server must be running and permit requests from the app. Select the downloaded model name.

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

The chat client uses the OpenAI-compatible `/chat/completions` streaming shape and supports the Anthropic Messages API. Hugging Face provider mode uses the Hub model catalog search filtered by task and the Router chat endpoint for compatible chat models. `/imagegen` can call HF Inference text-to-image for selected models; other HF tasks still require a matching endpoint/runtime. Catalog search sends a query and selected task to Hugging Face only when Search is clicked. Local servers should bind to loopback and allow the app origin. Cloud API providers receive the content you submit, under their own policies. An optional ChatGPT plan connection uses the supported Sign in with ChatGPT authorization flow and keeps its tokens in the Electron main process using OS-backed encryption. Claude.ai consumer sign-in is not available to third-party clients; use an Anthropic API key from the API Console (Claude subscriptions and API billing are separate). Never paste account cookies or session tokens here.

Local history and settings are stored in Electron's app profile using browser local storage. Clear app data through the operating system's app data controls to remove them. API keys are not encrypted in this prototype; do not use it on a shared Windows account. Offline chat/inference works with a local provider and already downloaded model. Hub search and downloads require a connection. A cloud endpoint, cloud model, or hosted image generation service sends the request data to that provider.

Image generation can use a ComfyUI API-format workflow pasted into Settings (replace `{{prompt}}` in text inputs), or Hugging Face Inference text-to-image when Hugging Face is the selected provider and task. Hub discovery is broad and task-filtered, but “all models” cannot run in one universal runtime: architecture, task, weights, dependencies, hardware, access gating, license, and inference provider determine compatibility. The built-in local downloader supports compatible ONNX exports. Transformers.js text generation streams locally; summarization, translation, text classification, and image-to-text captioning/OCR run locally as task pipelines. Downloads require an explicit button click and are capped at 10 GB/300 files. Speech and embedding inference still need a compatible local server or hosted endpoint. Models may be gated or have individual license restrictions.

## Project structure

- `electron/main.cjs`: desktop window and native menu.
- `index.html`, `style.css`, `app.js`: interface, styling, local history, and provider client.
- `.github/workflows/release.yml`: tagged release packaging.
