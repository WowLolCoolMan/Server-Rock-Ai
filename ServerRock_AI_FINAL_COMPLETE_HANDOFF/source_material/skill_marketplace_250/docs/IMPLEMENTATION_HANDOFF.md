# Codex / local agent implementation handoff for Server Rock: AI

Use the existing public repo https://github.com/WowLolCoolMan/Server-Rock-Ai **in a local authorized checkout**. The ZIP is a reference artifact. Do not assume the ZIP is the live codebase and do not copy generated docs over user files.

## Mission

Implement a first-class, cross-platform Agent Skill Marketplace in Server Rock AI while closing the product-readiness gaps in `docs/SERVER_ROCK_RELEASE_READINESS.md`. The marketplace discovers 250 metadata-indexed GitHub skill entries but does not bundle third-party code. User installs/updates selected skills from their source repos; updates are flagged using per-skill Git tree SHAs and only applied on explicit approval. The application must support Windows and Linux via the existing native Uno/.NET 10 solution, with capability-aware routing across local/cloud models, Hugging Face tasks and ComfyUI workflows. Do not falsely promise that any model supports every skill.

## Mandatory handoff safeguards

1. First inspect `git status --short`, `README.md`, `CLAUDE.md`, project guidance and `docs/ARCHITECTURE.md`, `docs/PRODUCTION_READINESS_GAPS.md`, `docs/NATIVE_RELEASE_ACCEPTANCE.md`. Record repo SHA and installed SDKs. Never run `git clean`, `git reset`, destructive checkout or delete untracked files.
2. Treat app's existing `SkaiDesktop.Core` library and Uno `ServerRock.Native` UI as primary. Do **not** introduce Electron/React as a second runtime. Keep Core independent of XAML.
3. Preserve current Coding Agent command/file review, state stores, credential protections, release updater, GitHub workspace features, model hotswapper and ComfyUI API contract.
4. Use `catalog_250.json` as a **seed metadata index**, not a trusted tool source. Review upstream SKILL.md, licenses, runtimes and permissions at install time. If an entry lacks SKILL.md, show an unsupported or reference-only status; do not lie.
5. Do not download 250 repos as a prerequisite for app install. Do not silently install, execute, update or permit third-party scripts.
6. Do not check user files into CI artifacts or send private project information to external services without consent.

## Stepwise implementation

### Milestone A: Baseline
- Compile native desktop, run Core tests, existing UI audit, and collect reported failures; distinguish unavailable OS/hardware tests.
- Create a reviewable diff checkpoint; no unrelated refactoring.

### Milestone B: Marketplace Core
- Create models, storage, catalog repository, public GitHub fetcher, staged installer, subtree SHA update checker, local modifications detector, rollback, dependency/license/permissions preflight.
- Port algorithmic reference in `marketplace.py` into C# .NET 10; do not ship Python runtime requirement.
- Build offline fixture tests with local fake git repos and HTTP mocks.

### Milestone C: Native Marketplace UX
- Replace static dialog with a native marketplace page offering Discover, Installed, Updates, My Skills and Activity.
- Include search, platform tags, tool permission and license disclosure, progress, install review, update diff, rollback and actionable errors.
- Focus and screen-reader behavior must work across popups, menus and narrow layout.

### Milestone D: Skill engine
- Parse SKILL.md and metadata; create model-aware router, context composer, per-request skill activation, permission-gated tool interface and trace log.
- Support zero-skill cases and explicit user selection; do not use tool-dependent skills on text-only models.
- Add four original skill templates from `original_skills` with review/test gating.

### Milestone E: Provider/task capabilities
- Add explicit model capability record and opt-in probes for tool calling, structured output, image handling, runtime readiness and context budgets.
- Integrate ComfyUI workflow dependency diagnostics and Hugging Face model format/license/hardware compatibility; do not collapse model repository with runnable model.

### Milestone F: Quality / release gates
- Implement findings R01–R50 per severity, starting with P0. Add documented tests and evidence for each.
- Run packaged Windows/Linux UI acceptance, signed release/installer upgrade/rollback and privacy-security tests on real systems.
- Update canonical docs with evidence. Never mark tasks completed based only on code presence or a passing static grep.

## Output format after each milestone

- Changed files and why
- API/model/schema migration details
- Tests executed and exact output
- OS/runtime where tested
- Screenshots or UI automation evidence where appropriate
- Remaining blockers, risks and rollback
- Narrow Git diff; never claim unverified passes

## Product acceptance definition

A user can install Server Rock on a supported Windows/Linux machine, run a local or cloud model, inspect the 250-entry catalog offline, explicitly fetch a source-verified skill, assess its permissions/dependencies, enable it for a task, understand why the model selected it, execute only approved tools, see verified results, check precise per-skill update flags, approve and roll back an update, and use Hugging Face/ComfyUI only when compatible with the selected task and runtime. Data privacy, focus/navigation, installer and update integrity are evidenced against shipping artifacts.
