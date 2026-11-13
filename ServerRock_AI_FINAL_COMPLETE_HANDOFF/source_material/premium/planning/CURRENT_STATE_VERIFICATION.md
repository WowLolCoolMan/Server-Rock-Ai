# Source-truth baseline and gap verification

Known from earlier source review (must reverify at current HEAD):
- Native Uno + Core + Core.Tests; `MainPage` partial classes.
- Chat streaming, recents persistence, model library and coding-agent foundations exist.
- `SkillCatalogService` has three static skill prompt templates, not a full marketplace.
- Plugin catalog contains static descriptors; live install lifecycle not confirmed.
- Packaged cross-platform smoke, signing and rollback evidence were listed as gaps.
- `docs/GOAL_EXECUTION.md` was not found at prior review; create if still absent.
These are **historical observations**, not claims about the current branch.

## Baseline procedure
1. Read AGENTS.md, README.md, docs/ARCHITECTURE.md, release and migration docs.
2. Record OS, dotnet SDK, Uno tooling, git HEAD, branch, git status, untracked files and build/test commands.
3. Preserve worktree; never reset, clean, stash or rewrite history without explicit user approval.
4. Build each project; run tests and native UI smoke where available; record raw commands and results.
5. Search implementation for persistence paths, secrets, HTTP clients, model routing, skills, agent commands, installer scripts, UI routes and TODOs.
6. Produce `docs/GOAL_EXECUTION.md` with matrix: requirement / present behavior / code owner / status / evidence / risks / next action.
7. Classify as implemented, partially implemented, missing, broken, or not verified. Prioritize regression protection before refactoring.
8. Inspect compiled app on both operating systems; do not substitute static XAML review for visual testing.

## Output
A reproducible evidence ledger and architecture map, not a speculative bug list.
