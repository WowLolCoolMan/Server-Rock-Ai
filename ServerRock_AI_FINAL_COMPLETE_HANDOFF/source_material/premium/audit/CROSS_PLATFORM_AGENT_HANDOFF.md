# Cross-platform agent collaboration handoff

Windows developer uses Codex; Linux developer uses Claude. Both must use the same versioned schema and API contracts, protected main branch, separate feature branches/worktrees, and reviewable PRs.

Shared acceptance: Core builds/tests on both OSes; UI smoke on both where available; path/casing/permissions verified; platform-only features clearly scoped. Neither agent may silently replace the other's files. Shared schema/IPC changes must be reviewed before dependent UI work.

Agent handoff fields: base commit, branch, changed files, requirement IDs, API/schema versions, test commands/results on each OS, screenshots/logs, migration effects, known blockers, pending approvals and rollback instructions.
