# Engineering agent instructions — verifiable delivery

## Phase 0: evidence
Read AGENTS.md, README, architecture and release docs, and existing GOAL_EXECUTION.md. Record git HEAD/status, OS/SDKs, dependency lockfiles, tests and app startup evidence. Preserve all uncommitted/untracked work. Identify actual files implementing chat, models, skill catalog, plugins, storage and native UI. Reconcile every item against current code.

## Phase 1: contracts
Define operation journal, typed capability/permission APIs, event transport, cancellation, process ownership, storage paths, and provider adapters. Make a working vertical slice before broad refactoring.

## Phase 2: UX architecture decision
Prototype the same real user journey in current Uno and candidate Electron/Tauri as necessary: open project -> select model -> inspect context -> request code edit -> preview diff -> approve -> run test in terminal -> inspect trace -> recover. Measure integration, keyboard/accessibility, runtime resource use, packaging and failure modes on both OSes. Report tradeoffs. **Do not migrate without approval.**

## Phase 3: independently testable product slices
Navigation/design tokens; chat and history; models/routing; marketplace install lifecycle; MCP; agent runtime and tool execution; diagnostics and single-root storage; installer/updates. Implement UI+backend+state+error+tests together.

## Phase 4: release evidence
Build and smoke actual packages on Windows and Linux. Verify install, upgrade, offline, crash recovery, uninstall, accessibility, permissions, migrations, and sanitized diagnostics. No untested 'done' claims.

## Review protocol
Every PR: requirement IDs, exact files, expected behavior, test commands/results, before/after screenshots where relevant, security and migration effects, upstream provenance, rollback, cross-OS validation, unresolved risks. Review by opposite agent plus human for high-impact changes.

## Stop conditions
Stop and request human approval for destructive cleanup, schema changes that cannot be rolled back, secret migration, changing UI framework, license-incompatible code reuse, cloud upload of local data, privilege escalation, or unsupported dependencies.
