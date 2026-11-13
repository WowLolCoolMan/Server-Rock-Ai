# Server Rock AI: Native marketplace upgrade plan (10 November 2026)

**Goal:** turn the existing Uno/.NET static skills and plugins dialogs into a genuinely useful opt-in Agent Skill Marketplace. Do NOT reduce requirements to existing functionality. Preserve user choice about architecture and do not edit repository code without approval.

## Evidence inspected (GitHub default branch, repository WowLolCoolMan/Server-Rock-Ai)
- `README.md`, `docs/ARCHITECTURE.md`, `docs/NATIVE_MIGRATION.md`, `docs/PRODUCTION_READINESS_GAPS.md`
- `native/SkaiDesktop.Core/Services/SkillCatalogService.cs`: exactly three built-in static read-only workflow templates (`reverse-engineer`, `project-audit`, `release-readiness`), each with a prompt and permission list.
- `native/SkaiDesktop.Core/Services/PluginCatalogService.cs`: static integration directory, built-in vs unavailable status. `skills-pack` is listed as unavailable.
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`: Skills dialog inserts selected prompt into `PromptBox`; plugin directory explicitly avoids a fake Install button.
- `docs/ARCHITECTURE.md`: Uno/XAML UI, .NET 10 Core, direct MainPage service calls; no DI/view-model layer yet.

These are source/document observations, NOT proof of running UI or an exhaustive full-repository audit. Validate against current commit and packaged builds before claiming completed functionality.

## Critical correction to previous ZIP
The old `03_TAURI_MIGRATION_AND_DESKTOP_UI.md` assumed an Electron frontend to wrap. Current repository says Electron is removed and Uno/.NET is the shipped stack. **Do not execute the old Tauri migration plan.** Upgrade in native Uno/C# by default; retain Tauri as an explicitly user-approved alternative with a separate cost/parity analysis. This file supersedes the old recommendation.

## Delivery approach
Build in phases with tests and evidence, not cosmetic UI alone. Marketplace must install, enable, invoke, update, rollback and uninstall actual skills. Keep static built-ins functioning during migration. Existing plugin entries are not evidence of working connectors. Preserve existing chat IDs, archives, branches and backups.
