# Desktop architecture and UI specification
## Architecture
Treat Tauri 2 + Rust as the target for a **measured prototype**, not a completed migration. Preserve working Electron frontend and Uno history; do not delete or reset the working tree. Reuse existing services through stable interfaces where sensible. Rust owns native file/process/installer boundaries; web frontend handles chat, marketplace, models, settings, active skills and status. Separate provider adapters and model runtimes from the UI.

## Feature-parity checklist
Cloud/local model selection and fallback, concurrent agent tasks, skill/MCP lifecycle, ComfyUI/Hugging Face integrations, chat persistence, background operations, permissions, error handling, keyboard navigation, Windows/Linux builds, updater and installer. Benchmark startup/memory and test packaging. Do not ship until parity tests pass.

## UI quality
Navigation: Chat, Agents, Models, Skill Marketplace, My Skills, MCP Integrations, Chat History, Downloads/Installations, Settings. Every async operation must show queued/running/completed/failed/cancelled states; progress, logs, retry and rollback. Prevent panels hiding critical actions. Use consistent dialogs, accessibility, responsive layout, keyboard focus and empty/error states. Add update badges, active-skill indicators, and ownership-aware removal previews.

## Security
Tauri capabilities protect frontend/native commands but do not sandbox arbitrary downloaded code by themselves. Use isolated execution, scoped permissions and per-tool approval policies.
