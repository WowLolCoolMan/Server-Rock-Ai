# Release gates and acceptance tests
- Marketplace: only explicitly installed/enabled skills route; incompatible provider-only skill never runs.
- Router: model switching recalculates eligibility; disabled skills stay disabled; task changes load only relevant skills.
- Installer: staged installs, hashes, dependency isolation, recoverable failures, GitHub rate-limit/offline states.
- Updater: changed subtree detection, pinned versions, permission-diff approval, rollback, update-all partial failures.
- Removal: process termination, owned-file cleanup, shared dependency retention, user-data preservation, reinstall success.
- Chat history: migrate all old chats, search, export/import, crash-safe saves, user-selectable folder, backup restore.
- Tauri: functional parity, Windows/Linux installers, background task visibility, accessible UI, no hidden controls.
- Engineering pack: compilation/tests/log evidence and code review; model compatibility assessed empirically.

## Evidence
For each test store OS, app version, model/provider, skill version, expected result, actual result, logs, screenshots where relevant and pass/fail. A proposed feature is not implemented until these tests pass.
