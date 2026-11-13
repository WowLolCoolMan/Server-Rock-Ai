# Skai built-in AirLLM runtime

Skai invokes `skai_airllm.py` as a local JSON-lines sidecar. The sidecar uses
AirLLM's layer-wise `AutoModel` API so the desktop app can run a Hugging Face
model whose full weights do not fit in the normal local runtime's memory.

Install once from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\services\airllm\install.ps1
```

On Linux/macOS use `bash services/airllm/install.sh`. The installer creates a
user-local environment and installs AirLLM plus its runtime dependencies.
Install the PyTorch wheel matching the machine's CUDA/CPU setup when needed.

AirLLM model IDs must be Hugging Face IDs such as `Qwen/Qwen3-32B` or a local
model directory. The first request may download and split the model into local
layer shards, so choose a cache directory with enough disk space in Skai.
