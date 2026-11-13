# Native UI, history, production quality

Retain Uno XAML, .NET 10 Core and existing working screens by default. Introduce native Marketplace and My Skills navigation, with a shared service layer instead of growing `MainPage` event-handler code. Use UI virtualization, keyboard/screen-reader labels, empty/error states, non-modal progress and resilient task cancellation. Test at small and large window sizes on Windows/Linux packaged builds.

Chat History: existing `ConversationStore` is canonical unless a tested migration is approved. Provide an accessible user-selectable 'Chat History' folder with atomic per-conversation JSON/Markdown exports, attachment references, archived/export/backups subfolders, one-click open, and searchable native UI. Preserve IDs, forks, branches, timestamps, archived state and import-origin markers; do not overwrite newer edits. Secret redaction and backup/restore verification required.

Production-Grade Engineering pack: optional modular skills for architecture, idiomatic code, API/dependency validation, testing, systematic debugging, static analysis, security, UI art direction, visual regression, documentation and release QA. Focus on measurable quality, not undetectable AI authorship. Minecraft skill must validate game version and mod loader and compile/run relevant tests.

Release blockers per repository docs: signing, exact packaged artifact smoke, clean install/upgrade/uninstall/rollback, migration tests, accessibility, Windows/Linux distro and architecture matrix, native hosted-provider credentials and other parity gaps. Don't advertise production readiness from a green build alone.
