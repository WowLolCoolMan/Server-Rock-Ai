# Open-source research and design inspiration

Research date: 2026-11-03

Reference: [open-webui/open-webui](https://github.com/open-webui/open-webui)

The project is a large self-hosted assistant platform. Its README highlights a provider-agnostic Ollama/OpenAI-compatible interface, model and agent selection, plugins/tools, multi-model conversation, message queueing, rich Markdown/LaTeX, voice, image workflows, and continuing releases. These are product and architecture ideas for Server Rock: AI: keep providers replaceable, keep local models first-class, make the model picker discoverable, make chats pleasant to read, and surface capabilities without making the first screen busy.

## What Server Rock: AI adopts

- A unified model picker across local runtimes and compatible hosted endpoints, with searchable models and measured local performance.
- Separate ChatBot and Coding Agent modes, with selected project folders as the code agent's workspace.
- A conversation-first desktop shell with navigation, recents, model controls, slash commands, and focused image and activity views.
- Offline Markdown rendering and copyable fenced code, using the separately licensed `marked` and `dompurify` packages (see `THIRD_PARTY_NOTICES.md`).
- Explicit Local versus Cloud status so data routing is visible at the point of use.

## License boundary

The upstream repository currently exposes a custom BSD-3-Clause-based license with an additional condition restricting removal or alteration of Open WebUI branding in broadly distributed deployments/distributions. Server Rock: AI has its own product identity and is intended for public downloads, so this repository does not copy Open WebUI source files, assets, logos, or branding. This note records research and inspiration only; upstream license terms remain with the upstream project. Re-check that license before any future code-level reuse.

Upstream references:

- [README and feature overview](https://github.com/open-webui/open-webui/blob/main/README.md)
- [Current upstream LICENSE](https://github.com/open-webui/open-webui/blob/main/LICENSE)
- [Provider API modules](https://github.com/open-webui/open-webui/tree/main/src/lib/apis)
- [Model selector](https://github.com/open-webui/open-webui/blob/main/src/lib/components/chat/ModelSelector.svelte)

## Computer parity target (2026-11-04)

Open WebUI's current Computer product page groups the target workflow into a real editor, filesystem browsing, a live terminal, and Git diff/commit/push review in one self-hosted workspace. Server Rock now covers the desktop half of that loop: authorized project folders, a resumable native terminal with bounded I/O, Coding Agent file review, and guarded GitHub push review. The remaining parity gaps are intentional follow-up work: remote/browser/mobile access to the same session, parallel agents/worktrees, and a richer unified-diff/selective-staging surface. The app should not claim those capabilities until they are implemented and verified.
