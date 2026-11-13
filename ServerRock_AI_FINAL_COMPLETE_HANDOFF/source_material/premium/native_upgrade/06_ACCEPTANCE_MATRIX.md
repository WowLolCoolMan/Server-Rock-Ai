# Acceptance matrix (must be demonstrated)

1. Browse 250+ curated entries without installing all; clearly label installable vs reference-only; search/filter performance tested.
2. Install a valid skill from pinned GitHub path and verify actual files and manifest state.
3. Reject a malicious/invalid manifest and show actionable error without partial installation.
4. Enable/disable/pin independently of installation; disabled skill never activates.
5. With Qwen active, Claude-only skill is ineligible; switch to Claude and re-evaluate without reinstall.
6. Task switch activates relevant compatible skills only; Active Skills explains why.
7. Tool-calling/MCP-required skill unavailable when tools absent; no false success.
8. Install, update, permission-diff review, rollback, uninstall and reinstall survive restarts.
9. Shared dependency stays when one of two consumers is uninstalled.
10. Windows/Linux package smoke, keyboard accessibility, UI focus, cancellation, offline mode and rate limits.
11. Chat History export/import preserves forks, archived state, attachments and stable IDs; no secret leakage.
12. Native Core regression: existing three built-ins, model picker, local endpoints, ComfyUI, coding agent, GitHub, guarded edits, chat persistence still work.

Evidence format: commit SHA, OS/runtime, build artifact checksum, skill package/version, selected model, steps, expected/actual, log, screenshot where useful, result, known limitations. No claim of completion until tested.
