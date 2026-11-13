# Two-agent collaboration — Windows/Codex and Linux/Claude

## Coordination
Maintain one shared decision/contract register in repo and one canonical `docs/GOAL_EXECUTION.md`. Use separate feature branches and non-overlapping ownership. Code changes from each side require independent diff review, Core/contract tests, and OS-specific GUI evidence. No unilateral Git history rewrite, merged partial dirty-tree cleanup, or silently overridden decisions.

## Recommended file ownership lanes (coordinate actual decisions)
- **Windows/Codex lane:** UI shell/Uno/XAML, design tokens, navigation, XAML accessibility, WinAppSDK and Windows installer acceptance.
- **Linux/Claude lane:** core agent state machine/services, model and skill adapters, Linux PTY/keyring/Skia acceptance, contract/unit tests.
- **Shared gate:** API/interface changes, persistence schemas/migrations, tool permissions, cross-platform tests, release signing and architecture ADR.
Avoid hard coupling: publish interface contract/fixture in a small reviewable commit before implementing independent consumers.

## Handoff contract per PR
Goal and requirement IDs; baseline SHA; changed files; screenshots of wide/narrow and relevant overlay; test commands/results; Windows/Linux evidence separately; platform discrepancies; data and credential handling; behavior on cancel/error/restart; rollback; licensing/provenance; files conflicting with other branch; next required gate.

## Merge discipline
One small vertical slice, peer review, shared tests, and exact source status. If test cannot run on local platform, label unverified and ask other contributor to run. Record upstream dependency changes and lockfile diffs. Keep agent task branches isolated and never force-push shared main without owner approval.
