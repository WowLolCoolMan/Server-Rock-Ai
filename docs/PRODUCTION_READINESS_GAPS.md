# Skai Desktop — Production Readiness Gaps

This is the current release roadmap. It records what remains before Skai Desktop can be described as a production-ready native replacement and a signed public release. Stable local data and credential identifiers remain unchanged across the product rename.

## P0 — release blockers

- Maintain the native capability map and close the remaining native release gates; the legacy Electron runtime has been removed.
- Run the final smoke, bug-check, accessibility, and UI checklist against the exact packaged artifacts, not only development builds.
- Configure and verify code signing for every published desktop target, including certificate provenance, timestamping, notarization where applicable, and published checksums.
- Produce trusted native installers/uninstallers and verify clean install, upgrade, data retention, uninstall, and rollback behavior.
- Validate packaged runtime behavior on Windows 11, supported Windows 10 editions, Ubuntu, Debian, Fedora, and Arch; record architecture and runtime versions separately.
- Validate import of a user-provided legacy chat archive on Windows and Linux, including preview, cancel/no-write, restart, duplicate protection, edited-chat preservation, interruption recovery, and keyboard focus restoration.
- Establish a signed update channel with an explicit rollback strategy and verify the published artifact URL and digest from a clean machine.

## P1 — native parity and reliability

- Add native hosted-provider credential/token flows; currently the native app is strongest with local endpoints and GitHub.
- Finish native coverage for scanned-PDF OCR, audio input, spreadsheet/data analysis, and model capability/vision compatibility discovery.
- Verify Linux keyring setup and provide actionable installation guidance when Secret Service is unavailable.
- Exercise GitHub empty-permission, permission-denied, network-failure, clone-conflict, cancellation, and retry states in isolated profiles with real credentials.
- Verify the Claude Code bridge on supported OSes with the official CLI installed, including sign-in, reauthorization, cancellation, missing-CLI guidance, and no-key secret isolation.
- Make provider failures preserve actionable context without exposing secrets, and ensure every retry path restores sensible keyboard focus.

## P1 — interaction and accessibility acceptance

- Run live keyboard and Narrator/assistive-technology passes across navigation, dialogs, composer, model library, GitHub, terminal, import/export, and error recovery.
- Complete Linux/Skia and narrow-layout interaction smoke; static UI contracts are necessary but cannot prove runtime focus behavior.
- Visually verify the native Settings redesign in a running packaged build.
- Finish the companion/pet presentation: transparent rendering, activity bubble, positioning, and collision behavior across workspace sizes.
- Consolidate transient success/error feedback into a consistent status system while keeping actionable failures visible until dismissed.
- Keep all visual and interaction behavior native; no second UI stack is permitted.

## P2 — polish and product quality

- Improve model-library explanations for compatibility, task support, licensing, download size, import progress, and failure recovery.
- Add clearer per-provider capability messaging before a user attaches unsupported media.
- Improve voice availability messaging and recovery across OS speech-service differences.
- Verify settings, marketplace, GitHub, and companion layouts at wide, desktop, tablet, mobile, and small-mobile widths in packaged builds.
- Add user-facing diagnostics that are redacted, exportable, and easy to attach to a bug report without including credentials or private content.

## Exit criteria

The project is ready to publish only when every P0 item has attached evidence, all required P1 acceptance runs pass on packaged artifacts, signing and rollback are verified, and the release workflow's fail-closed gates are configured. Current automated coverage is strong, but passing tests alone does not close the platform, migration, signing, or live-runtime gaps above.

Authoritative detail and historical evidence:

- [Native migration status](NATIVE_MIGRATION.md)
- [Native release acceptance](NATIVE_RELEASE_ACCEPTANCE.md)
- [UI reference progress](UI_REFERENCE_PROGRESS.md)
- [Rolling updates](ROLLING_UPDATES.md)
