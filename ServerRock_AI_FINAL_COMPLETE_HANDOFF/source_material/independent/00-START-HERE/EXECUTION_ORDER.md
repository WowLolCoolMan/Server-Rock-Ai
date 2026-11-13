# Execution plan: fix behavior first, then make UI excellent

## Rules of execution

- Work in actual checked-out Server Rock workspace. Inspect `pwd`, OS, SDK, Git status, AGENTS.md and project docs first. No `git reset`, clean, stash, checkout of user work, or destructive cleanup.
- Create a feature branch if worktree supports it; prefer a separate worktree only after confirming existing changes can be preserved.
- Baseline build/test before changing code. Record failures without claiming they were introduced by your work.
- For each issue: inspect code and tests; write reproduction (or mark unconfirmed); implement one bounded fix; run regression/integration test; inspect diff; update checkpoint; only then proceed.
- **Ideas only**: no Odysseus clone, code, packages, source-to-source mapping, assets, styling, API linkage, port, or architectural transplant. Read `inspiration/READ_THIS_FIRST.md` and build an original Server Rock solution from its own requirements.

## Phase 0 — Baseline and emergency P0

1. SR-001: prove/refute CRLF root cause. Add string normalization to audit temporarily, then replace source matching with behavior tests. Run CI on Win and Ubuntu. Do NOT add magic source strings just to satisfy audit.
2. SR-002: repair Windows install launcher: full versioned directory launch path. Test clean install, start, close, update, rollback, uninstall on Windows 11. Include smoke check of exact shortcut target + actual app window.
3. SR-003: isolate reviewed Git changes from unrelated existing index. Add tests for already staged secrets, partial hunks, rename, conflict, spaced/unicode filenames, concurrent modification.
4. SR-004/005: define permission and isolation contract before adding any autonomous tool execution or third-party skills. Ensure program refuses unverified rights escalation.

## Phase 1 — UI architecture and correctness

5. SR-008/009/010/011/012/024/025/033: introduce navigation store, overlay host, UI event lifecycle, focus and notification manager. Convert heavyweight dialogs into routed pages; preserve drafts. Validate all resolution/theme/zoom combinations.
6. SR-028/036/037: virtualized chat, detail-pane UI, theme tokens, scroll preservation and contrast.

## Phase 2 — Actual agent

7. SR-006/007/026/027: typed tools, execution ledger, repository discovery/index, bounded tool loop, verification and task resume. No autonomous CLI effects unless capability grants permit.
8. SR-016/017/022/023/029: provider abstraction, selected-model preservation, hardware/capability filtering, cost/latency budgets, consentful fallback.

## Phase 3 — Marketplace and platform

9. SR-013/014/038: GitHub-backed skill lifecycle, progressive disclosure, tool permissions, update checks, rollback and runtime adapter integration.
10. SR-015/032: background scheduler and persistent image queue with UI progress cards.
11. SR-018/019/030/031/034/035/039/040: signed releases, secure updater, diagnostics/recovery, platform parity, tests and honest documentation.

## Release gate

- Build: Windows + Linux release artifacts; native app starts from CLEAN profile; no source-asset/test harness dependencies.
- CI: no critical checks skipped; SAST, secret scan, dependency review, signing/integrity.
- Data: migration, cancel, crash, backup/restore, rollback and uninstall preserve user data.
- Agent: task runner records intended vs actual actions and evidence; no prompt injection elevates scope; interrupted tasks can resume or fail honestly.
- UI: no hidden/intercepted controls; focus return; no nested dialogs; all settings editable at small window widths; style tokens and accessibility checked.
