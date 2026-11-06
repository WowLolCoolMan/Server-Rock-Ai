# Built-in local image generation

## Current gap

The native desktop client currently supports a user-configured local ComfyUI
workflow for image generation. It does not yet bundle a Stable Diffusion
runtime or provide a built-in local image-model downloader. Hosted inference is
kept separate and clearly labeled. The delivery plan below describes the native
runtime boundary that should replace the current workflow.

## Recommended approach

Bundle a small native inference engine with the desktop app and have the app own
model discovery, downloads, generation controls, saved images, and
progress/cancellation. The strongest candidate is `stable-diffusion.cpp`: its
upstream project describes a pure C/C++ implementation with no external runtime
dependencies, an MIT license, Windows/Linux/macOS support, CPU and several GPU
backends, and support for Stable Diffusion and other image model families. The
first supported lane should be its CPU build; add a Vulkan build as an optional
accelerated runtime after testing representative GPUs. This avoids bundling the
large Python/PyTorch stack required by Hugging Face Diffusers.

Users would not install or configure another app. We either bundle the CPU engine
in our installer, or deliver a signed, version-pinned engine package from our own
app release channel on first use. The app validates that package before launching
it. GPU backends still need a compatible system driver, just as other native
software does; CPU remains the universal fallback, with a clear slower-performance
message. This is “built in” from the user's perspective, while acknowledging that
the application redistributes a third-party open-source engine under its license.

The engine's format support is not identical to Hugging Face Diffusers' Python
library. Start with compatible standalone `.safetensors` and `.gguf` image weights.
Do not promise that every Hub repository works: standard Diffusers repositories
may contain component folders and architectures that need conversion or are not
supported by the native engine. Search results must identify the exact compatible
file/layout, and unsupported repositories should say why they cannot be installed.
Do not support pickle-based `.ckpt`/`.pt` downloads in the first release.

### User flow

1. The Images page offers **Generate locally** as the simple default. Hosted
   generation stays a separate, clearly labeled choice.
2. First use checks for the bundled or app-managed image runtime. If absent, the
   app explains the download size and supported hardware, then offers **Set up
   local image generation**. This downloads and verifies our signed runtime asset;
   users never open a terminal or install a separate tool.
3. **Browse image models** searches Hugging Face for formats and architectures
   supported by the bundled engine. Model cards show license, exact file format,
   size, task, and whether the app recognizes the model layout. The catalog never
   implies every Hub repository is runnable.
4. The user downloads a compatible model into the app's dedicated image-model
   store. The UI shows byte progress, cancel/retry, and enough disk-space guidance.
5. The model dropdown selects among installed image models. Advanced controls
   expose dimensions, steps, guidance, seed, and device choice; safe defaults keep
   the first generation simple.
6. Results are saved in the app's image history and remain available offline.

### Format and security boundary

- First release supports only model families and file layouts validated against
  the pinned native engine version. Prefer standalone safetensors or GGUF. It does
  not load `.ckpt`/pickle weights or execute custom code from a model repository.
- Model download allowlists repository files needed by standard pipelines,
  enforces file-count and total-size limits, writes temporary files before
  replacing completed files, and supports cancellation and retry.
- The UI displays the repository license and links to its model card before the
  user starts a large download. It does not claim to validate model safety or
  grant redistribution rights.
- Runtime installation is a user-initiated action with clear storage/network
  requirements. Hub authentication is only used for the user's gated model
  download; prompts and generated images stay local for local inference.
- Generation has explicit memory-aware limits, cancellation, bounded output, and
  clear errors for unsupported formats or insufficient memory.

## Delivery slices

1. **Runtime boundary:** managed worker lifecycle, readiness/device probe,
   cancellation, progress messages, output limits, and deterministic test seam.
2. **Model library:** Hugging Face image-pipeline search, format/license metadata,
   safe standard-Diffusers download, integrity/error recovery, and installed-model
   inventory.
3. **Image generation:** local text-to-image and image-to-image execution,
   progress/stop, image history, and no-ComfyUI first-run behavior.
4. **Product fit:** simple Images page, optional advanced controls, empty/loading/
   offline/error states, keyboard/accessibility review, and native service
   integration.
5. **Verification:** mocked worker and Hub tests, real small permissively licensed
   smoke model on CPU, GPU checks where hardware is available, desktop packaging,
   and Windows/Linux runtime matrix evidence.

Do not call the feature complete until a real small model has generated and
reloaded an image locally. Unit tests with mocked models only verify plumbing.

## Compatibility notes

The native engine documents CPU, CUDA, Vulkan, Metal, OpenCL, and SYCL backends,
with Linux, Windows, and macOS as supported platforms. Actual acceleration still
depends on the binary we build, the user's GPU, and its driver. CPU is the broad
fallback but may be slow. The feature must report the backend it detected and let
users choose a compatible model size before downloading. The project's Windows 8
target needs a separate compatibility decision; the app shell, native runtime,
driver support, and packaged binary all need evidence before we claim it works.

The upstream engine is under active development and says its CLI/API may change.
Pin a reviewed release/commit, wrap it in our own small stable adapter, test its
exact command and progress output, and verify the license of the full dependency
tree before shipping. Include attribution and license notices in our packages.

## Official references

- [stable-diffusion.cpp supported models, formats, platforms, and backends](https://github.com/leejet/stable-diffusion.cpp)
- [stable-diffusion.cpp build options](https://github.com/leejet/stable-diffusion.cpp/blob/master/docs/build.md)
- [stable-diffusion.cpp backend selection and memory placement](https://github.com/leejet/stable-diffusion.cpp/blob/master/docs/backend.md)
- [Hugging Face Diffusers model formats](https://github.com/huggingface/diffusers/blob/main/docs/source/en/using-diffusers/other-formats.md)
- [Diffusers installation and supported devices](https://huggingface.co/docs/diffusers/installation) (comparison baseline; not the proposed shipped runtime)
- [Transformers.js supported tasks and WebGPU behavior](https://huggingface.co/docs/transformers.js/)
- [Hugging Face model API](https://huggingface.co/docs/hub/api)
