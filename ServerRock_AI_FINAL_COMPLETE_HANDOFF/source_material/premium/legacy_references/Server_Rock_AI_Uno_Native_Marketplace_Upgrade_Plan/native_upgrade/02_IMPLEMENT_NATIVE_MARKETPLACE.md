# Implementation blueprint: elevate the marketplace

## Reuse, do not mask
Keep `SkillCatalogService` as a compatibility adapter for the three built-ins, but introduce an independent Core domain: `SkillManifest`, `SkillPackage`, `SkillInstallRecord`, `SkillSource`, `SkillPermission`, `SkillCompatibility`, `SkillHealth`, `SkillExecutionTrace`. Build services under `SkaiDesktop.Core` and platform-specific adapters under `ServerRock.Native`. Avoid adding package installation logic to `MainPage.PluginsAndSkills.cs`.

Suggested interfaces (illustrative, not existing code): `ISkillCatalogProvider`, `ISkillRegistry`, `ISkillPackageInstaller`, `ISkillUpdateChecker`, `ISkillCompatibilityEvaluator`, `ISkillRouter`, `ISkillExecutionHost`, `ISkillPermissionBroker`, `ISkillHealthChecker`, `ISkillUninstaller`. Keep a dedicated MCP manager distinct from prompt-only skill packages. UI invokes services through application layer; persist state transactionally.

## Manifest
Schema version; stable package ID; title/summary/category/tags; publisher/repository URL/subdirectory; pinned commit or release; license; skill type (`instructions`, `tool`, `mcp`, `pack`); SKILL.md path; model/provider allowlist/denylist; required capabilities and context budget; OS/arch/runtime; dependencies; permissions; install hooks; health checks; installed file ownership; update channel; compatibility test evidence. Validate manifests and reject ambiguous/unsafe paths; don't execute install scripts without explicit approval and confinement.

## User flows
- Marketplace: browse curated and community sources, search/filter by task, provider, capability, platform, license, size, trust and installed status. Show package origin, verified status, version, permissions, dependencies, model compatibility and reasons for incompatibility. A repo is not automatically an installable skill.
- Install: preview permissions/dependencies/disk footprint; pin source; stage; verify integrity; install scoped files; health check; commit ownership ledger; optional enable. Failure must not report success.
- My Skills: enabled/disabled, pin/unpin, version, update available, last used, permissions, active status, logs, uninstall.
- Chat: Active Skills chip/panel with automatic/manual selection, activation reasons and explicit permission prompts. Explain when model changes deactivate skills.
- Updates: check GitHub release/tag or relevant subdirectory commits; cache and handle offline/rate limits; stage new version; show permission changes; verify; atomic swap; rollback. `Update All` handles partial failures transparently.
- Uninstall: stop processes, remove exclusively owned files and caches, preserve shared runtimes and user data unless separately approved, verify no stale registry entries; allow retry.

## Security and compatibility
Prompt instructions are untrusted input, never authority to bypass runtime permissions. Downloaded skills/MCP servers can execute code; require provenance, explicit permissions, isolation, timeouts, resource caps and logs. Windows/Linux require separately verified runtime paths and process cleanup.

## UX quality
Use a native Uno/XAML marketplace screen, not a giant blocking dialog. Clear loading/empty/offline/failed/queued/update states. Search, pagination/virtualization, accessible keyboard navigation, responsive detail panes, real progress, retry and cancellation. Keep UI fully native per current architecture unless user chooses otherwise.
