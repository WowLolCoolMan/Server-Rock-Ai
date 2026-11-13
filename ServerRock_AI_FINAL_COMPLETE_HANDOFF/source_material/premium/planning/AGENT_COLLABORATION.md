# Windows Codex + Linux Claude collaboration

Two developers work in different operating systems with independent AI agents. The repository, tests and GitHub PRs are the source of truth; no agent has implicit authority to override the other's decisions.

## Ownership suggestions
- Windows/Codex: core service interfaces, provider/agent engine, marketplace backend, Windows integration and installer.
- Linux/Claude: Uno design system and UI, accessibility, Linux integration and packaging.
- Shared: contracts, schema changes, CI, threat model, release acceptance.

## Coordination rules
- Feature branches/worktrees; never have both agents change the same working tree.
- One PR per coherent vertical slice. Define acceptance and API contracts before coding.
- No direct pushes to protected main; no force-push or history rewriting.
- Both OS builds must pass before merge of shared Core/UI changes; platform-only changes require owner plus cross-platform smoke.
- Agent A reviews Agent B's diff for logic, security, regressions; Agent B reviews Agent A's diff for usability, platform assumptions and integration.
- Require a human approval for dependency introduction, license changes, credential handling, data migration and destructive operations.
- Document owner and merge order for shared schema/interfaces.
- Use GitHub Issues and PR templates; link evidence to requirement IDs.

## Conflict avoidance
Use CODEOWNERS, stable interfaces, short-lived branches, per-feature test fixtures, generated file ownership and a shared decisions log. Keep OS-specific paths in platform adapters, not scattered across views.
