# Server Rock: AI super-app research

Research date: 2026-11-03

The goal is to combine the strongest user-facing workflows from the listed projects behind one clean desktop interface. This means integrating local model servers and cloud APIs, then adding first-party workspace features (projects, knowledge, prompts, image/audio tools, and agents). It does not mean embedding every upstream app or promising that every model format runs in one runtime.

## Product capabilities to combine

| Reference | Useful capability | Server Rock approach |
| --- | --- | --- |
| Open WebUI | Model/workspace organization, tools and knowledge around one chat surface | Use the capability layout; keep independent provider and tool adapters. Its current license includes an additional branding condition, so no upstream source/assets are copied into this branded product. |
| LobeChat | Polished multi-provider interaction and multimodal workflows | Reproduce the workflow patterns in Server Rock's existing desktop shell; audit current repository and component licenses before any code reuse. |
| LibreChat | Broad provider switching, agents, code interpreter | Treat API/provider adapters as a capability target; MIT-licensed source portions may be reused only with copyright/license notices and dependency review. |
| Jan | Offline-first desktop UX | Connect to Jan's local OpenAI-compatible server; don't bundle its app/runtime. |
| GPT4All | Accessible local model setup and local API | Detect the GPT4All server on `127.0.0.1:4891` and use its OpenAI-compatible API. Individual model weights have their own terms. |
| AnythingLLM | Local documents, workspaces and retrieval | Build a local knowledge collection and retrieval layer in Server Rock; MIT source is a potential reuse option after source/dependency review. |
| Khoj | Personal knowledge and scheduled research | Add local knowledge/search and later scheduled workflows as optional modules, without making the first-run chat screen busy. |
| text-generation-webui | Runtime flexibility for local model backends | Detect its compatible API and let it handle model formats and hardware-specific backends. |
| LocalAI | OpenAI-compatible local API | Support its configured endpoint as an ordinary local provider. |
| NextChat | Simple cross-platform client | Keep the chat client responsive and endpoint configuration simple. |
| HuggingChat | Hugging Face ecosystem and model routing | Keep Hub discovery, model task metadata, and supported local/cloud inference explicit. Do not promise universal model compatibility. |

## Implementation boundary

Prefer wire-protocol and runtime integrations over copying whole apps. This keeps one UI, avoids multiple bundled databases and auth stacks, and lets users update their preferred runtimes separately. Reuse MIT/Apache/BSD code only after confirming the exact repository revision, license scope, and required notices; AGPL/GPL/custom branded terms need a separate compatibility review. Product names and marks are not automatically granted by a source-code license.

The model catalog and picker are a router-facing surface. A provider adapter advertises its endpoint, authentication scheme, model list, streaming format, and available capabilities. A model's file format/runtime determines whether it can run locally; the app can still route unsupported local architectures to a compatible host. Hosted account login is provider-specific: use documented OAuth or API credentials and never import browser cookies or desktop session tokens. Claude subscription access is not offered through third-party Claude.ai login; use Anthropic API credentials or supported cloud providers.

## Current foundation

- Host discovery includes Ollama, GPT4All, LM Studio, Jan, LocalAI, llama.cpp, vLLM, KoboldCpp, and text-generation-webui compatible endpoints.
- The UI has ChatBot and Coding Agent modes, local project folders, a searchable model selector, and local image/voice workflows.
- ChatGPT account authorization is not currently exposed by the native client. Other providers use configured API endpoints; hosted-provider credential flows remain gated until native OS-backed storage and recovery UX are complete.
- No telemetry or analytics are included. Model discovery, downloads, and hosted inference only contact their configured services when the user invokes them.

## Upstream references

- [Open WebUI features and documentation](https://docs.openwebui.com/)
- [Open WebUI license](https://github.com/open-webui/open-webui/blob/main/LICENSE)
- [LobeChat source/license](https://github.com/lobehub/lobe-chat)
- [LibreChat license](https://github.com/LibreChat-AI/LibreChat/blob/main/LICENSE)
- [Jan repository](https://github.com/menloresearch/jan)
- [GPT4All Local API](https://github.com/nomic-ai/gpt4all/wiki/Local-API-Server)
- [AnythingLLM license](https://github.com/Mintplex-Labs/anything-llm/blob/master/LICENSE)
- [Text Generation Web UI](https://github.com/oobabooga/text-generation-webui)
- [LocalAI](https://github.com/mudler/LocalAI)
- [NextChat](https://github.com/ChatGPTNextWeb/NextChat)
- [HuggingChat](https://github.com/huggingface/chat-ui)
- [Anthropic authentication guidance](https://platform.claude.com/docs/en/manage-claude/authentication)
