# Engineering agent implementation loop — mandatory gates

## 0 Read-only truth capture
`pwd`, OS, architecture, `dotnet --info`, git HEAD/branch/status, scoped instructions (`AGENTS.md`, `CLAUDE.md`, `README.md`, `DESIGN.md`), repo docs, existing `docs/GOAL_EXECUTION.md`; inspect current native classes, available model endpoints/installed runtimes only with permission; record existing tests. Never clean/reset/stash/prune. Do not treat attached ZIP as live code status.

## 1 Plan one vertical slice
Choose requirement IDs and exact start/end UX journey (e.g. launch → open project → choose agent → run → review → commit). List impacted source files, data migrations, mock/stubs vs real integrations, permission effects, UI states, expected tests. Confirm existing behavior and design constraints. Avoid broad rewrites.

## 2 Implement genuinely
Use existing Uno/XAML and .NET Core; isolate domain from UI, reuse tokens/controls; add backend operation with cancellation/approval/provenance, observable state, no dead buttons; integrate to existing routes/sessions/storage/keyboard. No fake progress or partial functionality advertised as completed.

## 3 Verify
Build shared target, test core/service contracts, exercise real flow on actual desktop (Windows/Linux as supported), take screenshots before/after, test keyboard and narrow dimensions, simulate offline/invalid/cancel/crash, review code/security. Node UI lint cannot replace GUI test. If Linux GUI unavailable, record unverified and require companion agent's evidence.

## 4 Reviewable checkpoint
Summarize changed files, diff, build/test command and exact output, UI evidence path, data/security effects, errors and limitations, requirement IDs, rollback plan and status. Update `docs/GOAL_EXECUTION.md` with specific evidence; do not rewrite old sections. Request review by teammate before merging cross-platform UI changes.

## 5 Continue gated dependency
Next slice only after current core functionality passes appropriate checks. Maintain backward compatibility and preserve all user data. If high-risk operation is needed, seek explicit user approval; do not merely rely on the ZIP's broad mandate.

## Avoid
Design-only mockup claiming feature done; fabricated screenshots; 'working' due to method existing; dotnet compile claimed Linux runtime success; empty skill marketplace tiles; unnecessary framework rewrite; silent source copy; unsupervised destructive shell; versioned package with unsigned updater claimed secure; 100 unrelated edits in one PR; estimates/dates as acceptance gates.
