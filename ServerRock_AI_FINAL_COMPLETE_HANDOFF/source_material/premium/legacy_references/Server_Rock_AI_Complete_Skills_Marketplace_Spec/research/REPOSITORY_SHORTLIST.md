# GitHub repositories useful for Server Rock AI

These are research candidates, not automatically approved dependencies. Verify maintenance, licensing, platform support, and security before adopting. Prefer architectural ideas over copying code from unrelated projects.

1. **anthropics/skills** — Agent skills: Skill packaging and examples. https://github.com/anthropics/skills
2. **obra/superpowers** — Agent workflows: Debugging, TDD, planning, agent coordination. https://github.com/obra/superpowers
3. **agentskills/agentskills** — Agent skills discovery: Open agent skills format and ecosystem. https://github.com/agentskills/agentskills
4. **modelcontextprotocol/servers** — MCP servers: Reference MCP servers. https://github.com/modelcontextprotocol/servers
5. **modelcontextprotocol/typescript-sdk** — MCP SDK: Implement MCP clients and servers. https://github.com/modelcontextprotocol/typescript-sdk
6. **docker/mcp-gateway** — MCP gateway: Manage MCP tools in containers. https://github.com/docker/mcp-gateway
7. **microsoft/playwright-mcp** — Browser testing: Browser automation via MCP. https://github.com/microsoft/playwright-mcp
8. **github/github-mcp-server** — GitHub integration: GitHub repository and issue tools. https://github.com/github/github-mcp-server
9. **upstash/context7** — Docs retrieval: Version-specific documentation lookup. https://github.com/upstash/context7
10. **ollama/ollama** — Local models: Local model server and model lifecycle. https://github.com/ollama/ollama
11. **ggml-org/llama.cpp** — Local inference: Local inference engine and GGUF support. https://github.com/ggml-org/llama.cpp
12. **lmstudio-ai** — Desktop runtime: Local model ecosystem reference. https://github.com/lmstudio-ai
13. **open-webui/open-webui** — LLM UI: UI patterns for local/cloud model management. https://github.com/open-webui/open-webui
14. **lobehub/lobe-chat** — LLM UI: Conversation, plugin and provider UI reference. https://github.com/lobehub/lobe-chat
15. **langchain-ai/langgraph** — Agent runtime: Durable agent graphs and checkpoints. https://github.com/langchain-ai/langgraph
16. **microsoft/autogen** — Agent orchestration: Multi-agent architecture reference. https://github.com/microsoft/autogen
17. **crewAIInc/crewAI** — Agent orchestration: Role-based agent workflows. https://github.com/crewAIInc/crewAI
18. **run-llama/llama_index** — RAG: Document indexing and retrieval. https://github.com/run-llama/llama_index
19. **qdrant/qdrant** — Vector search: Vector database for knowledge retrieval. https://github.com/qdrant/qdrant
20. **langfuse/langfuse** — Observability: LLM traces, evaluation, usage monitoring. https://github.com/langfuse/langfuse
21. **promptfoo/promptfoo** — Prompt evaluation: Prompt and model regression testing. https://github.com/promptfoo/promptfoo
22. **comfyanonymous/ComfyUI** — ComfyUI: Node-based image workflows. https://github.com/comfyanonymous/ComfyUI
23. **huggingface/huggingface_hub** — Model downloads: Model discovery and downloads. https://github.com/huggingface/huggingface_hub
24. **tauri-apps/tauri** — Desktop framework: Cross-platform desktop app framework. https://github.com/tauri-apps/tauri
25. **electron/electron** — Desktop framework: Desktop packaging and runtime. https://github.com/electron/electron
26. **shadcn-ui/ui** — UI components: Accessible component design reference. https://github.com/shadcn-ui/ui
27. **radix-ui/primitives** — UI primitives: Accessible UI interactions. https://github.com/radix-ui/primitives
28. **pmndrs/zustand** — State management: Lightweight frontend state. https://github.com/pmndrs/zustand
29. **getsentry/sentry** — App telemetry: Crash reporting and debugging. https://github.com/getsentry/sentry
30. **CoplayDev/unity-mcp** — Unity integration: Unity Editor automation. https://github.com/CoplayDev/unity-mcp

## Development priorities
1. Reliable model routing, provider errors, and sticky fallback.
2. Skill discovery, permissions, provenance, and update checks.
3. Durable task execution, logs, cancellation, and recovery.
4. UI state consistency, background-job visibility, accessibility, and test coverage.
5. Local inference, GPU memory handling, model downloads, and ComfyUI integration.

Do not incorporate Odysseus code: use it only as a design/architecture reference when requested.
