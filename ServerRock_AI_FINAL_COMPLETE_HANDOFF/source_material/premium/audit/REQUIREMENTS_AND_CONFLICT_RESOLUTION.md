# Consolidated decisions, conflict resolutions and unresolved choices

## Settled user requirements
1. Two-person development: Windows + Codex, Linux + Claude; separate branches, shared contracts and cross-platform checks.
2. Product is an AI desktop command center, not merely a chat wrapper: chat, coding, terminal, model routing, marketplace, tools and projects.
3. Users explicitly install and enable optional skills. Installed != enabled != selected != authorized to execute.
4. Skill catalog should grow to hundreds of curated entries; **catalog size is not installed count or quality evidence**.
5. Model-agnostic skills where feasible; use capability detection and explicit vendor-specific compatibility tags.
6. Skill updates from GitHub, pinned source revisions, update indicators, diff/permission review, rollback, and clean removal.
7. Optional installs of MCP servers and runtimes require similarly reversible lifecycle management.
8. One discoverable user-selected root for app-owned persistent data and visible core files. External drivers, system libraries, secure OS keyrings and user-selected projects may be outside it.
9. A clearly labeled **Chat History** folder, readable export, search, retention and backup; do not destroy existing conversations.
10. API keys easy to locate *in the credential manager*, never stored unencrypted in browsable files.
11. UI must be professional, predictable, accessible, and honest about progress, failures and side effects.
12. Anti-generic-AI engineering skills should improve idiomatic code, design quality, tests, and verification, not hide provenance or fake human authorship.
13. Odysseus is **ideas-only**, never direct code copying or incorporation.
14. Agent Machines / virtualization is future backlog, not current scope.
15. No schedule or time-to-complete requirements in the handoff.

## Conflicts resolved
- Previous 'Uno only' instructions are superseded **only to the extent of permitting a framework evaluation**. Uno remains current implementation until user approves a change.
- React ecosystem recommendations are conditional on selecting Electron or Tauri. Do not install Monaco/xterm/shadcn automatically in Uno.
- Existing three static skill templates do not constitute the requested marketplace.
- Separate Codex Hybrid Router prototype is not evidence of native Server Rock support.
- No source review proves production-readiness; run actual Windows/Linux packaged tests.
- Open-source donor projects require exact license/commit/component review. Do not claim all 30 are audited.
- 'All files in one folder' means app-owned persistent files and a discoverable map, not relocating external OS dependencies.
- 'Clean removal' means remove owned optional files and references, preserving user data and shared dependencies by default.
- Prompt injection is a threat-model and test requirement, not a solvable prompt-only guarantee.

## Explicit human decisions required
- Desktop UI architecture: Uno vs Electron+React+.NET vs Tauri+React+.NET after prototype evidence.
- Credential vault: portable passphrase-backed encryption vs OS-backed secret store, and portability tradeoffs.
- Default marketplace trust policy: curated sources, unverified source warnings, and executable skill permissions.
- Automatic update policy: notify by default; never silently increase privileges.
- Model routing consent: whether per-workspace approved remote egress may persist.
- Default backup, retention, and diagnostic collection policies.
