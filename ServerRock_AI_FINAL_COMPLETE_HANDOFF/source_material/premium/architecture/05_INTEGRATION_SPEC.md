# Interoperability and ecosystem

## MCP
Use maintained C# SDK when appropriate. MCP server registry tracks installation source, executable/transport, exposed tools, credentials references, permissions, health, lifecycle and uninstall ownership. Untrusted MCP output cannot change policies. Implement process supervision, bounded output, schema validation and consent.

## ACP
Evaluate ACP for coding-agent interoperability. ACP is not universal LLM support. Provide adapters for native Server Rock agents and selected external coding agents; negotiate capabilities, sessions, tool permissions, streaming and cancellation. Preserve backend-specific limitations visibly.

## Model providers
OpenAI-compatible APIs plus native provider adapters where necessary. Support Ollama, llama.cpp and LM Studio through documented APIs where possible. ComfyUI and Hugging Face are optional integrations, not required core dependencies. Support model discovery, download progress, local/cloud policy, health, failure handling and consent-aware fallback.

## Context orchestration
Explicit include/exclude controls for files, instructions, memories, skills, tool results and attachments; deduplication, context budgets, truncation warnings and provenance. Indexing is opt-in where sensitive data may be involved.

## Package manager
Shared staging/verification/ownership/rollback primitives, separate adapters for skills, MCP servers, model assets and optional runtimes. Never auto-execute unreviewed GitHub setup scripts.
