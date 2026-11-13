# Model routing, marketplace, cloud/local, and ComfyUI

## Models: decouple catalog, runtime, and provider

1. Catalog models from Hugging Face/ModelScope/Ollama and optionally vendor indexes. Catalog entries are metadata, not proof of local runnability.
2. Runtime adapters identify installed model, active server/endpoint, context, tool API support, vision/audio capability, quantization, memory/GPU footprint, license, backend status.
3. `IModelProvider` abstracts chat streaming, tool calls, embeddings, image generation, transcription and speech. Do not pretend all chat-compatible APIs support tools. Use capability probes.
4. Model router selects eligible providers only from the user's privacy and billing policy. A local-only task must not fallback to cloud. Show before first transfer if project files would leave the device.
5. Retries: exponential jitter on rate limits and transport failures; retry only idempotent inference requests; protect against duplicate external effects after tool execution.
6. Per-run budgets: max tokens, estimated provider cost, concurrency, time, VRAM memory, queue and cancellation; exact usage when available, estimated when not.

Keep existing `ModelLibraryService` GGUF digest verification and local discovery, but add download disk preflight, failure recovery, pinned revision/license metadata, and verified runtime import status. Avoid treating a successful HTTP catalog search as an installed model.

## Skill manifest contract (proposed v1)

```yaml
schema_version: 1
id: publisher.repository.skill_slug
name: Project Health Audit
version: 1.0.0
source:
  provider: github
  repository: publisher/repository
  ref: exact-commit-sha
  path: skills/project-health/SKILL.md
entrypoint: SKILL.md
triggers:
  - audit project
  - review code quality
required_capabilities:
  - workspace.read
optional_capabilities:
  - workspace.test
platforms: [windows, linux]
model_requirements:
  min_context_tokens: 8192
update_policy: notify_only
```

Namespaced IDs and publisher identity are distinct; never trust frontmatter `owner` to override installation owner. Skill usage needs a selection reason, confidence, and compatibility result. Index only short descriptions in model context; fetch full procedure when relevant, bounded by token budget. An agent may propose skill creation from repeated successful procedures, but a draft must be tested against no-skill baseline, reviewed and published only after policy permits it.

## Skill Marketplace actions

- Browse: search, category, compatibility, readme/provenance, required permissions, maintainer, license, last verified commit, installs and update status.
- Install: inspect manifest → clone/fetch to quarantined temp → enforce download/file count/decompression limits → verify commit/tree/content hashes → check path traversal/symlinks → show permissions → atomically install version → update local index.
- Use: relevance and capability filter → user opt-in policy → full SKILL.md load → agent tool selection → audit execution evidence → report result.
- Update: store upstream repo URL + pin SHA; poll GitHub releases/tags/branch head according to each skill's policy; compare installed SHA vs remote; show exact changed instructions/tools/permissions; never silently promote new privileges; rollback and offline cache.
- Remove/disable: de-index and block invocation, clear grants, preserve optional history; dispose running tools or require user confirmation.
- Windows/Linux: use built-in HTTP/Git implementation with validated paths, not OS-specific shell assumptions; package APIs are cross-platform .NET. Git CLI only fallback with explicit check, no arbitrary scripts during import.

## Plugins and MCP

Separate read-only skills (instructions), executable plugins, and remote MCP servers. Define independent trust/permission boundaries. Each connector has a manifest and tool schemas; credential store and revocation; endpoint allowlisting and redirection checks; telemetry-free local modes. The default UI must never imply a static catalog card is installed.

## ComfyUI

Build Images as a first-class workspace with: installed-model inventory, workflow templates, node dependencies, preflight missing custom nodes/weights, queue positions, progress polling, safe cancellation, history with prompt/model/seed/workflow provenance, output open/save and metadata filtering. Use bounded localhost HTTP and ensure malformed output URIs can't escape expected origin. Keep user-generated media separately from app binaries.

## Independent implementation requirement

Implement Server Rock's skill registry, audit lifecycle, marketplace, and provider adapters from native C# interfaces and explicit acceptance tests. The `inspiration/` directory contains user-facing ideas only, not technical implementation references.
