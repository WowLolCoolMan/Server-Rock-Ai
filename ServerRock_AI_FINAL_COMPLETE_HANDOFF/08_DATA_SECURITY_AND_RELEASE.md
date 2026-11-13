# Data integrity, permissions, privacy, update and release controls

## Data model and migration

The existing native app stores conversation, project, settings and undo JSON in the platform-specific application-data folder; preserve it. Migration to SQLite is a proposal only, requiring explicit ADR, preflight schema audit, backup, dry-run importer, verifiable equality of conversation branches/attachments, crash-safe transactional cutover, user-facing rollback and legacy read support. Do not wipe or replace app data to simplify implementation. Keep temporary chat memory-only as documented. Use atomic writes and bounded archive imports; detect external file changes by hash/version before overwriting. Profiles and project folders must never be bundled into app updates by accident.

## Security posture

1. OS-keyring-backed hosted API credentials and GitHub tokens, with explicit Linux Secret Service missing/unavailable state. No plaintext fallback, no secrets on command line or in logs.
2. Workspace-scoped file capability policies with symlink, special-file and path traversal handling, bounded input and output, and separate read vs write permissions.
3. Tool and command approvals always identify the exact action; stored grants have scope, expiry and revocation. Confirmation UI should never hide output behind a window.
4. The selected model's data destination (local loopback vs cloud endpoint) must be visible, including when fallback changes that destination. External providers receive only user-authorized context.
5. Extension/skill/MCP servers are untrusted execution boundaries. Validate source hashes, scripts, package provenance, runtime permissions and dependency licenses; no install-time automatic execution.
6. Native OS sandbox claims need verified OS mechanisms. Process ACLs/UI prompts alone do not make a command safe. Document unsandboxed modes conspicuously and use least privilege.
7. Protect Git actions against leaking secrets (staged diff/paths scan and human review) and incorrect default branch guesses; branch operations should preserve user changes and honor cancellations.
8. Structured diagnostics use redaction and opt-in export, with no mandatory analytics or surprise external traffic.
9. Treat provider responses, web pages, repositories, skill manifests and retrieved docs as untrusted data. Never let their embedded instructions override developer/user permissions.
10. Crash recovery must not replay uncertain side effects automatically, including Git push or remote operations.

## Release readiness

Windows/Linux build success is necessary, not sufficient. Require clean-machine install, startup, accessibility and GUI workflows from **exact** signed artifacts; per-OS shell, credential, file picker, PTY, rendering, sound/voice limitations, scaled displays and GPU probes; uninstall data-retention behavior; upgrade and rollback preserving user profile; updater manifest authenticity, SHA-256 and publisher signatures. Track exact supported versions/distros/architectures; do not advertise Windows 8 or untested Linux distros. Missing signing credentials or artifact smoke => release pipeline fails closed.

Reference current repo docs: `docs/PRODUCTION_READINESS_GAPS.md`, `docs/NATIVE_RELEASE_ACCEPTANCE.md`, `docs/ROLLING_UPDATES.md`, and source premium `upgrade/platform/CROSS_PLATFORM_RELEASE.md`. Existing signed manifest and CI features must be audited, not replaced by vague generic build scripts.
