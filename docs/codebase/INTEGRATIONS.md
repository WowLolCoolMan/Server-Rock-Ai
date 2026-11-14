# Integrations

## Local model and media runtimes

- Ollama, LM Studio/OpenAI-compatible loopback servers, LocalAI, llama.cpp, and vLLM are named local model targets in product documentation and provider code.
- AirLLM is represented as an optional/partial provider path.
- ComfyUI is the current image workflow integration, using local queue/poll behavior and local output handling.
- OS-native speech/TTS is available as a local capability; a complete local STT/TTS model marketplace path is not finished.

## GitHub

- GitHub is an explicit project integration for repository status, guarded actions, branches, pull requests, and remote operations.
- Authentication and credential handling are local/policy-gated rather than a cloud model route.
- The current main Electron Marketplace does not yet expose the earlier remote GitHub skill discovery/install flow as a live product feature.

## Marketplace sources

- Core contains model library, authored/curated skill catalog, package installer, plugin catalog, and MCP-related clients.
- The current renderer `MarketplacePage.tsx` presents local categories and explicitly states that nothing is fetched from a hosted catalog; non-local categories are activation placeholders until backend wiring exists.
- No current Agent Host marketplace method family was found. This is the key contract gap preventing the UI from being a real discovery/install surface.
- Hugging Face/ModelScope-related provider logic exists below the UI, but the current main Electron route does not make it user-selectable with provider, size, quantization/ablation, license, and hardware filters. [TODO]

## MCP and tool integrations

- Core supports bounded stdio/HTTP-style MCP clients, OpenAPI descriptors, conservative tool classification, manifests, permissions, and audit concepts.
- Process supervision, notifications/resources, authenticated retrieval, and full UI lifecycle/health coverage remain incomplete according to `docs/CAPABILITY_MATRIX.md`.

## Persistence and secrets

- Local JSON-backed stores hold product state and are guarded by profile paths and recovery tests.
- Windows-protected credential storage is used for sensitive local credentials where implemented.
- No external database, hosted queue, telemetry vendor, or cloud synchronization path is part of the intended architecture.

## Reliability boundary

- Requests have bounds, cancellation, and timeout handling in several adapters.
- There is no uniform retry, backoff, circuit-breaker, provider health, or resumable-download contract across all integrations. [TODO]

## Evidence

- `README.md`
- `docs/CAPABILITY_MATRIX.md`
- `docs/MODEL_LIBRARY.md`
- `docs/LOCAL_IMAGE_GENERATION.md`
- `apps/desktop/src/features/catalog/MarketplacePage.tsx`
- `native/SkaiDesktop.Core/Services/ModelLibraryService.cs`
- `native/SkaiDesktop.Core/Services/SkillPackageInstaller.cs`
- `native/SkaiDesktop.Core/Services/AuthoredSkillCatalog.cs`
- `native/SkaiDesktop.Core/Integrations/`
