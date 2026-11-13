# Integration guide for Server Rock AI

## Classify entries before installing

These 50 are capability targets, not 50 downloadable SKILL.md packages. Some links point to SDKs, runtimes, frameworks, or reference projects. Never automatically clone and execute every entry.

## Marketplace manifest fields

Use id, title, summary, source_url, source_type (agent_skill / framework / runtime / reference), license, commit_sha, default_branch, version, supported_os, prerequisites, permissions, install_method, update_status, last_checked, verification_status.

## Safe GitHub update workflow

1. Fetch repository metadata and inspect its license and release history.
2. Detect whether a valid SKILL.md exists. If not, classify as a reference rather than an installable skill.
3. Pin an approved tag or commit; record its SHA and content hashes.
4. Install in a per-skill directory using staging, schema validation and atomic promotion.
5. For updates, fetch remote metadata and compare pinned commit/tag; display 'Update available' without automatically executing new code.
6. Preserve user changes and offer a diff/rollback before applying updates.
7. On Windows use Git for Windows or an embedded Git client; on Linux use system git or libgit2. Avoid shell interpolation of user-supplied URLs.
8. Require approval for shell, filesystem writes, network access, secrets and destructive operations.

## Routing logic

At request time, use a compact index of skill descriptions and required tools. Select up to 3-5 likely skills by intent, available runtime, prerequisites and permissions; load full SKILL.md instructions only after selection. Verify skill outputs and log provenance.

## Recommended implementation order

First: router, goal execution, verification, permissions, GitHub marketplace. Next: code analysis, debugging, tests, browser and desktop tools. Then: model fallback, memory, RAG and creative workflows.

## Quality gates

Validate install/uninstall, Windows/Linux parity, offline behavior, invalid skill manifests, broken GitHub URLs, revoked permissions, rollback, and UI state persistence.
