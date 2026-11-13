# Verified public repository snapshot (READ before acting)

**Repository:** https://github.com/WowLolCoolMan/Server-Rock-Ai
**Reference branch:** `main`
**Git HEAD inspected:** `4ad39cfb89ddf97cd1ddd4533c4b879d8124ac95` (2026-11-07T18:56:44Z, subject: `Merge pull request #5 from WowLolCoolMan/native/polish`).
**Inspection date:** 2026-11-10. This is an external GitHub snapshot, not verification of a local working tree or currently running app.

## Confirmed repository facts
- `native/ServerRock.Native` is Uno.Sdk XAML native UI (`net10.0-desktop` Skia, `net10.0-windows10.0.26100` Windows App SDK target).
- `native/SkaiDesktop.Core` is a UI-independent .NET 10 class library; `native/SkaiDesktop.Core.Tests` is a Core test console runner.
- Current shell: `MainPage.xaml`, `SidebarView.xaml` and `MainPage.*.cs` feature partials. Many dialogs are assembled as `ContentDialog`s in C#.
- App theme tokens and shared styles: `native/ServerRock.Native/App.xaml`, `UiTheme.cs` and root `DESIGN.md`. **Current documented theme is light only**; dark mode is target, not already shipped.
- Provider streaming currently uses OpenAI-compatible HTTP endpoint abstraction. Local model library scans installed runtimes. GitHub workspace, project terminal, guarded coding proposals/undo, ComfyUI and chat/history exist at some level as described in README/ARCHITECTURE; verify operational behavior rather than treating docs as passing tests.
- `package.json` calls Node scripts solely to build/launch/check the **native .NET app**; Node/Electron is not the app runtime.
- Actual `docs/PRODUCTION_READINESS_GAPS.md` calls out release blockers: real packaged UI/accessibility smoke, signing, installers and upgrade/rollback, distro coverage, legacy import, hosted provider flows, Linux keyring/recovery and Claude bridge validation.
- `docs/UI_REFERENCE_GUIDE.md` indexes 21 product/interaction screenshots **already in repo** (see live repo for files). These are visual references, not product assets or functional evidence.

## Evidence limits
I did not run the repository in Windows or Linux, run .NET builds, reproduce bugs, inspect the user's local branches, or validate any cloud/model credentials. The requirements in this ZIP are proposals until code/test evidence confirms them. Keep the codebase's own current release/quality docs as a checklist and reconcile any drift.

## Strong warning
Earlier assistant response recommended Electron + React as a generic option. That does NOT represent the actual codebase and MUST NOT trigger an implementation rewrite. The existing native Uno implementation is the default. Optional framework study requires owner-approved ADR.

## Read-only inspection commands
```powershell
# Windows PowerShell (inspect only)
pwd; dotnet --info; git status --short; git rev-parse HEAD; git branch --show-current
```
```bash
# Linux (inspect only)
pwd; uname -a; dotnet --info; git status --short; git rev-parse HEAD; git branch --show-current
```
Read the repo before using any build commands; avoid modifying or cleaning unknown user changes.
