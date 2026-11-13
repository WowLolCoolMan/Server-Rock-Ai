# Recovered user decisions and constraints

## Product identity
- Repository: https://github.com/WowLolCoolMan/Server-Rock-Ai
- Preserve Uno Platform XAML / C# / .NET 10 native app. Electron/Tauri migration suggestions in older materials are outdated.
- Windows and Linux are both primary targets. One developer uses Windows + Codex, the other Linux + Claude. Both work after hours; do not include scheduling in the engineering pack.
- User wants a complete desktop AI dashboard for local and cloud models, coding agents, ComfyUI, Hugging Face, model management, tools, MCP and a rich marketplace.

## Marketplace: explicit requirements
- Target a curated catalog of approximately **250–500 distinct entries** as a discovery goal, NOT 250–500 automatically installed or active skills.
- Users select which skills to **install** and separately which to **enable**. Installed does not mean active.
- Router activates only task-relevant installed+enabled skills, and only when compatible with active model, required tools, MCP, OS and dependencies.
- Prefer broad compatibility across frontier and local LLMs, with explicit tags for exceptions such as Claude-only.
- Re-evaluate eligibility on model switch and explain why a skill is or is not usable.
- Marketplace pulls from GitHub on Windows/Linux, pins revisions, checks for updates, flags availability, and supports approved updates, rollback and removal.
- Skills should be creatable by the AI and tested before publication/activation.
- Every optional component needs clean uninstall with ownership manifest and shared-dependency protection.

## Storage and transparency
- All **app-owned persistent** runtime/data artifacts should live in a single user-selected discoverable root, including a plainly labeled **Chat History** folder (not just an opaque conversations.json file).
- Users can inspect core files, logs, settings, model downloads, skills, chat logs, ownership manifests and backups.
- Dependencies outside root are acceptable when necessary (drivers, OS libraries, credential services, external runtimes). Disclose external paths in Folder Map.
- Credential *management* must be discoverable; never expose raw API keys in plaintext.

## UX and safety
- Premium, intuitive, transparent UI with no hidden page overlays, unexpected actions, dead controls or invisible state.
- Strong model routing and clear local/cloud disclosure; no surprise remote sends.
- Agent workflows should use typed tools, previews, approvals, execution, verification and recovery.
- Odysseus is **STRICTLY IDEAS ONLY**: no copying source, no dependency, no incorporation, no migration of its architecture.
- 'Agent Machines' / virtual-machine feature is deferred to long-term backlog, not current implementation.
- Package is for AI coding agents to consume; instructions must be concrete, auditable and safe from prompt injection.
