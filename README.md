# Server-Rock-Ai

**Server Rock: AI**

A local-first desktop workspace for chatting with local models and compatible AI endpoints. It uses a compact, keyboard-friendly layout inspired by modern chat desktop apps.

## Features

- Windows, macOS, and Linux Electron desktop packaging.
- Conversation history and preferences stored in the local browser profile.
- OpenAI-compatible streaming chat endpoint configuration, including Ollama-compatible endpoints, LM Studio, and other local servers.
- Optional API key for endpoints that require one. Keys are stored in the local browser profile; avoid shared Windows accounts.
- Light and dark themes, attachment picker, and workspace chat history.
- Choose Chatbot or Coding agent mode. Coding agent mode can create named projects and add local source folders with the native folder picker; folder paths are kept in the local app profile.
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
npm run build:win
npm run build:mac
npm run build:linux
```

Build each platform on its supported OS. GitHub Actions is configured in `.github/workflows/release.yml`: push a version tag such as `v0.1.0` to build and attach installers to a GitHub Release. Updates are distributed through GitHub Releases; this starter intentionally does not silently download or install updates. Users download and install the release they choose.

The Windows NSIS wizard supports a per-user install, a selectable destination, and optional desktop/Start Menu shortcuts. Windows' Apps & Features entry launches the included uninstaller. Uninstall preserves the user's Server Rock: AI profile by default; remove app data separately if you also want to erase local conversations and settings. Linux AppImage is a portable single file. macOS DMG installs by dragging the app to Applications; remove it by deleting the app from Applications.

## Free distribution

Push a version tag such as `v0.1.0` to build installers and attach them to a GitHub Release. Updates are currently user initiated: download and run the newer installer. The app does not check for updates in the background. Link the Windows installer and other platform builds from `www.server-rock.com`; the site repository/hosting configuration is not present in this workspace. The app contains no paid feature gates or telemetry.

## Providers and privacy

The chat client uses the OpenAI-compatible `/chat/completions` streaming shape. Hugging Face provider mode uses the Hub model catalog search filtered by task and the Router chat endpoint for compatible chat models. `/imagegen` can call HF Inference text-to-image for selected models; other HF tasks still require a matching endpoint/runtime. Catalog search sends a query and selected task to Hugging Face only when Search is clicked. Local servers should bind to loopback and allow the app origin. Cloud API providers receive the content you submit, under their own policies. This app does not implement ChatGPT Plus or Claude account-cookie login; those consumer subscriptions do not provide a general-purpose local OAuth API for third-party clients. Use official API credentials or a local compatible bridge you trust. Never paste account cookies or session tokens here.

Local history and settings are stored in Electron's app profile using browser local storage. Clear app data through the operating system's app data controls to remove them. API keys are not encrypted in this prototype; do not use it on a shared Windows account. Offline chat/inference works with a local provider and already downloaded model. Hub search and downloads require a connection. A cloud endpoint, cloud model, or hosted image generation service sends the request data to that provider.

Image generation can use a ComfyUI API-format workflow pasted into Settings (replace `{{prompt}}` in text inputs), or Hugging Face Inference text-to-image when Hugging Face is the selected provider and task. Hub discovery is broad and task-filtered, but “all models” cannot run in one universal runtime: architecture, task, weights, dependencies, hardware, access gating, license, and inference provider determine compatibility. The built-in local downloader supports compatible ONNX exports. Transformers.js text generation streams locally; summarization, translation, text classification, and image-to-text captioning/OCR run locally as task pipelines. Downloads require an explicit button click and are capped at 10 GB/300 files. Speech and embedding inference still need a compatible local server or hosted endpoint. Models may be gated or have individual license restrictions.

## Project structure

- `electron/main.cjs`: desktop window and native menu.
- `index.html`, `style.css`, `app.js`: interface, styling, local history, and provider client.
- `.github/workflows/release.yml`: tagged release packaging.
