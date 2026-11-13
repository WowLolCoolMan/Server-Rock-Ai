# Two-developer Windows/Linux handoff and implementation coordination

## Read-only preflight (both machines)

```
pwd
git rev-parse HEAD
git branch --show-current
git status --short
dotnet --info
```

Read the repository's `AGENTS.md` if present, `CLAUDE.md`, README, DESIGN, build manifest, current release and migration docs, and any existing `docs/GOAL_EXECUTION.md`. Never run `git clean`, `reset --hard`, `stash`, `checkout` over dirty files, Git object garbage collection, prune, or destructive cleanup against the user's checkout as a shortcut. Preserve scratch and untracked files exactly.

## Coordination protocol

1. One shared canonical plan and status ledger; use requirement IDs from `index/COMBINED_REQUIREMENTS_126.csv` **after verifying their current relevance**.
2. Maintain separate branches/worktrees, one owner for each changed file or explicit coordination; share interface contracts early.
3. Define DTO/IPC/contracts and schema versions before separately implementing UI and service sides. Use fake adapters and fixtures to allow parallel work.
4. Before merge, inspect whole diff, run core/UI tests, record actual Windows/Linux results and review user-facing changes. Do not merge green CI with red functional smoke.
5. Use ADRs for dependency/framework/storage/security choices, with evidence and rollback. No architecture migration without explicit owner authorization.
6. Cross-platform parity means meaningful behavior, not identical OS-native dialogs. Document feature exceptions (voice services, GPU backends, signed packaging) and do not hide them.

## Suggested commands (verify in current checkout)

```
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run --project native/SkaiDesktop.Core.Tests/SkaiDesktop.Core.Tests.csproj
npm run audit:native-ui
```

Run native UI and integration tests on actual Windows and actual Linux environments; a WSLg smoke alone does not establish real distro coverage. Keep local SDK/package updates optional and scoped to the project.

## Handoff template

```
Task ID:
Repository HEAD / branch / OS:
Touched files and ownership:
Intended behavior and approvals:
Actual behavior before/after:
Commands, test outcomes and captures:
API / schema / UI contract changes:
Privacy, licensing and security notes:
Known failures and uncertain outcomes:
Recovery and rollback:
Next task blocked by this change:
```
