# Server Rock: AI — Future Features

This is the product backlog for future capabilities. It is intentionally broader
than the current release roadmap. Features should be implemented one bounded
vertical slice at a time, starting with the simplest useful item, and checked
off only after the feature works in the native app with the appropriate tests
and acceptance evidence.

## How to use this list

- Keep this document in sync with the repository and review it before starting
  a new feature.
- Choose the simplest unblocked feature that advances the product, then split
  it into a small, reviewable implementation slice.
- Record design, privacy, platform, and licensing constraints before adding a
  dependency or promising a capability.
- Move completed items to the implemented state only after the user-facing
  workflow and failure states are verified.

## Core chat

- [ ] Multi-model picker supporting any compatible provider.
- [ ] Side-by-side model comparison.
- [ ] File, image, and PDF upload with retrieval-augmented generation (RAG).
- [ ] Web search with citations.
- [ ] Persistent memory with a view/edit management UI.
- [ ] Message queueing while the AI is responding.
- [ ] Folders, tags, and pinned chats.
- [ ] Fork a chat from any message.
- [ ] Voice input/output, hands-free calls, and advanced voice mode.
- [ ] Image generation and editing.
- [ ] Context compaction.
- [ ] 1M+ token context support.
- [ ] Omni-modal input for text, images, audio, and video.
- [ ] Chat export and split view.
- [ ] NotebookLM-style mode grounded only in uploaded documents.

## Agents and tools

- [ ] Computer use with screen, keyboard, and mouse control.
- [ ] Built-in browser panel.
- [ ] Python sandbox execution.
- [ ] Parallel sub-agent delegation.
- [ ] Scheduled automations.
- [ ] Structured task lists.
- [ ] MCP tools over Streamable HTTP and stdio.
- [ ] OpenAPI tool auto-discovery.
- [ ] Git panel for stage, commit, push, and pull requests.
- [ ] Coding-agent model integrations, including Claude Code and Codex.
- [ ] AI planning mode: plan, execute, verify, iterate, retry, and pause for human decisions.
- [ ] Auto mode for unattended agent execution.
- [ ] Background tasks.
- [ ] Learning loop from past interactions.
- [ ] Dispatch tasks from a phone to the desktop.
- [ ] Nested sessions for subtasks.
- [ ] Parallel sessions with Git isolation.
- [ ] Coordinator, specialist, and verifier architecture with a shared living specification.
- [ ] Containerized parallel agents with Docker and IDE synchronization.
- [ ] Background computer use while the user works elsewhere.
- [ ] Autonomous open-weight model agent.
- [ ] Headless/daemon mode for CI and cloud deployment.
- [ ] Portable local harness with orchestrator, models, tools, and queue.
- [ ] Hybrid cloud/local compute split.
- [ ] Fully offline local-first agent mode.
- [ ] Remote device access for controlling a desktop elsewhere.

## Retrieval and knowledge

- [ ] Multiple vector database backends.
- [ ] Hybrid search with BM25, vector search, and reranking.
- [ ] Multiple document-extraction engines.
- [ ] Agentic retrieval that browses documentation.
- [ ] Full-text context injection.
- [ ] Nested file browser with preview.
- [ ] Projects that group chats, shared knowledge, and context.
- [ ] Saved environments for reusable folders, SSH hosts, and cloud contexts.
- [ ] File connectors that open, edit, and save files directly.
- [ ] Integrated file editor that writes approved changes back to disk.
- [ ] Visual diff review.
- [ ] Memory management UI for viewing and editing remembered information.

## Workspace and desktop experience

- [ ] Interactive artifacts for code, HTML, and diagrams.
- [ ] Tabs beside chat for files, terminals, and previews.
- [ ] In-browser terminal.
- [ ] Rich Markdown editor with in-place AI enhancement.
- [ ] Notes with context injection.
- [ ] Channels with model tagging and threads.
- [ ] System-wide hotkey and launcher integration.
- [ ] Screen capture and screen awareness.
- [ ] Live presence.
- [ ] End-to-end encrypted direct messages.
- [ ] Clipboard history with AI search.
- [ ] Unified screen, audio, and application context capture.
- [ ] Active-window context injection.
- [ ] Dictation that combines speech with automatic app context.
- [ ] File and app previews.
- [ ] Design handoff to a coding agent.
- [ ] Session bar showing context, changes, and background tasks.
- [ ] Side chats spawned with a keyboard shortcut.
- [ ] Detachable chats in their own windows.
- [ ] Drag-and-drop pane layouts.
- [ ] Custom themes.
- [ ] Cloud sync across devices.
- [ ] Built-in translator.
- [ ] Text snippets with AI expansion.
- [ ] Window management.
- [ ] Quicklinks for frequent-access bookmarks.
- [ ] In-app documentation.
- [ ] Developer mode.
- [ ] Global quick-AI fallback for non-command input.
- [ ] Native multi-session sidebar with drag-and-drop panes.

## Models, runtime, and extensibility

- [ ] Presets bundling prompts, tools, and knowledge.
- [ ] Dynamic variables such as `{{USER_NAME}}` and `{{DATE}}`.
- [ ] Lazy-loaded Markdown instruction-pack skills with `$` mentions.
- [ ] Local model runner support for Ollama and LM Studio.
- [ ] Offline-capable operation.
- [ ] OpenAI-compatible API and personal API keys.
- [ ] Plugin and pipeline framework.
- [ ] Community tool library.
- [ ] Third-party extensions that provide their own AI models.
- [ ] NPU/GPU/CPU auto-routing through Windows AI Foundry.
- [ ] On-device LoRA fine-tuning.
- [ ] Bring-your-own-key with credit-based pricing.
- [ ] React/TypeScript extension SDK.
- [ ] Team-shared commands, snippets, and private extension store.
- [ ] One-time purchase/no-subscription option.
- [ ] Model Council that merges answers from multiple models.
- [ ] Effort modes: Light, Standard, High, and Ultra.
- [ ] Adaptive reasoning depth.
- [ ] Speculative decoding with DFlash, DSpark, or MTP drafters.
- [ ] Tensor parallelism across GPUs.
- [ ] Continuous batching and parallel requests.
- [ ] KV-cache checkpointing for agentic workflows.
- [ ] Specialized inference engines such as Splash.
- [ ] Per-token cloud pricing.
- [ ] Open-weight Decisions API for classification and routing.

## Administration and teams

- [ ] Role-based access control and user groups.
- [ ] LDAP, SSO, OIDC, and SCIM provisioning.
- [ ] Analytics dashboards for tokens and per-user usage.
- [ ] Model arena and ELO leaderboards.
- [ ] Webhooks.
- [ ] Per-user isolated environments.
- [ ] Workspace-wide retention, sharing, and training policies.
- [ ] Centralized billing.
- [ ] Zapier automation.
- [ ] HIPAA workspaces.
- [ ] Audit logs.
- [ ] Organization-wide retention policies.
- [ ] Private extension store.

## Files, environments, and delivery

- [ ] Saved reusable environments for local and remote resources.
- [ ] PR monitoring.
- [ ] App previews.
- [ ] Design-to-code handoff.
- [ ] Shared living documents with team editing.
- [ ] Publish/share artifacts on the web.
- [ ] Publish web pages through a Sites-style workflow.
- [ ] Document, slide, and spreadsheet generation in chat.
- [ ] Presentation generation.
- [ ] On-canvas design editing with design-system import.
- [ ] Inline charts and 3D visuals.

## Content generation and meetings

- [ ] Deep research with autonomous multi-step web research and a report.
- [ ] Gems/personas saved as reusable AI configurations.
- [ ] Meeting notes with AI synthesis, including handwritten notes to structured summaries.
- [ ] Real-time transcription without a bot joining the call.
- [ ] AI-enhanced handwritten notes.
- [ ] Custom meeting templates.
- [ ] People and Companies views.
- [ ] Shared folders.
- [ ] Cross-meeting AI chat, such as “what did we agree last month?”.
- [ ] MCP access to meeting notes for other AI tools.
- [ ] Slack, Notion, Zapier, HubSpot, and Attio integrations.
- [ ] Apple Watch companion app.
- [ ] Multi-language meeting support.
- [ ] Meeting model-training opt-out.
- [ ] Personal voice training and voice cloning for TTS.
- [ ] System-wide dictation with AI post-processing.

## Ecosystem and platform

- [ ] Native Microsoft Office integration.
- [ ] Native Google Workspace, Calendar, Mail, and Drive integration.
- [ ] Floating always-accessible overlay bar.
- [ ] Phone call capability.
- [ ] iPhone/iPad companion app.
- [ ] Encrypted remote tunnels such as Tailscale or LM Link.
- [ ] Remote access to desktop models from mobile.
- [ ] OS-native sandboxing.
- [ ] One-time purchase option.
- [ ] MLX optimization for Apple Silicon.
- [ ] Windows AI Foundry routing.

## Reference

The backlog was collected from product research and the requested capability
set. AirLLM is one referenced project for large-context and model-runtime
exploration: <https://github.com/lyogavin/airllm>. Any future code reuse or
integration must go through the repository's licensing, security, privacy, and
platform review requirements.
