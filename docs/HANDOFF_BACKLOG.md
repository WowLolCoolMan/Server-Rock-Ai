# Live Handoff Backlog

Snapshot: `main` at the current checkout through Slice 211, including the
bounded Quick Open slice, native UI tokenization, release workflow audit, and
durable project-file tabs/resume action, Git conflict recovery guidance, and
the capability-neutral agent execution lifecycle, enforced release workflow
audit, and the shared native UI token audit/refinement.
This is an evidence-backed planning ledger, not a claim that the handoff is
complete.

## Source register audit

The handoff inputs were re-read from the extracted bundle and counted locally:

| Source | Verified count | Meaning |
| --- | ---: | --- |
| `index/COMBINED_REQUIREMENTS_126.csv` | 126 | 86 premium requirements plus 40 deduplicated remediation issues |
| `index/PREMIUM_REQUIREMENTS_86.json` | 86 | Canonical premium product requirements |
| `index/ISSUES_DEDUPED_40.json` | 40 | Consolidated uploaded issues |
| `index/PREMIUM_ACCEPTANCE_70.json` | 70 | Acceptance scenarios to reconcile and execute |
| `index/SKILL_CANDIDATES_250.json` | 250 | Research candidates only; none are treated as installed/validated |

All 126 CSV rows still carry the source status `requires_live_repo_recheck`.
The live status below is therefore conservative: `working` requires source plus
focused test evidence; `partial` means a real bounded slice exists but the
complete journey or platform acceptance is open; `unverified` means the
required platform/runtime evidence is missing; `blocked` is reserved for an
external dependency that prevents a meaningful next action.

## Requirement distribution by area

| Area | Rows | Live status | Evidence / next dependency |
| --- | ---: | --- | --- |
| Shell & UX | 15 | partial | `docs/CAPABILITY_MATRIX.md`; focus, persistent tabs and inspector remain |
| Conversations | 14 | partial | `native/ServerRock.Native/MainPage.Chat.cs`; cross-platform and multi-pane acceptance remain |
| Agent runtime | 14 | partial | `native/ServerRock.Core/Services/Operation*`; parallel isolation/background execution remain |
| Models, skills & tools | 16 | partial | Core model/skill/MCP services and tests; broader adapters/marketplace evidence remain |
| Projects & coding | 14 | partial | `native/ServerRock.Native/MainPage.CodingAgent*`; bounded Ctrl+P quick-open now exists, while full workbench/conflict/worktree journey remains; live first-project acceptance is picker-host blocked |
| Storage, platform & release | 13 | partial | stores, packaging and release validator; signed clean-machine lifecycle remains |
| UI | 8 | partial | native UI audit and screenshots; live narrow/focus matrix remains |
| Context | 2 | partial | memory/context planner/attachment policies; semantic continuity remains |
| Security | 2 | partial | permission gates and redaction; OS isolation and negative suite remain |
| Git | 2 | partial | GitHub service and selected-commit tests; conflict/push reconciliation remains |
| All other areas | 26 | partial/unverified | exact row mapping remains in the 126-row source register; platform-specific evidence is still required |

The area counts above sum to 126. The complete row-level IDs, priorities,
acceptance text and provenance remain in the canonical CSV; no research
candidate or historical prompt overrides that source.

## Dependency-ordered next slices

1. Release artifact acceptance: signed/unsigned fail-closed checks, clean
   install/upgrade/uninstall/rollback and exact artifact evidence.
2. Native overlay/focus and 560px acceptance: reproduce the Popup focus gap,
   keyboard activation and modal/drawer hit-testing with the supported harness.
3. Project workbench completion: persistent tabs/file tree/diff/terminal state,
   conflict recovery and worktree isolation.
4. Durable agent execution: parallel task artifacts, cancellation/reconcile,
   background visibility and explicit approval boundaries.
5. Skills/MCP/integration expansion: reviewed provenance, permissions,
   revocation, fixture tests and truthful unavailable states.
6. Linux parity and release evidence: choose the target distro, install the
   .NET 10 SDK, then rerun the same focused/native/package gates.

## Current blockers and owner decisions

- Linux runtime/package validation is unverified because the local WSL distro
  has no `dotnet` command; this is actionable once a supported SDK/runner is
  available, not a reason to claim completion.
- Signed release material and a clean-machine test environment are not present
  in the current workspace; signing and clean install evidence remain open.
- `[ASK USER]` Choose the supported Linux distribution/desktop environment.
- `[ASK USER]` Choose the migration cutover threshold for the Electron/agent-host
  surface versus the existing native app.

## Evidence

- `ServerRock_AI_FINAL_COMPLETE_HANDOFF/index/COMBINED_REQUIREMENTS_126.csv`
- `ServerRock_AI_FINAL_COMPLETE_HANDOFF/index/ISSUES_DEDUPED_40.json`
- `ServerRock_AI_FINAL_COMPLETE_HANDOFF/index/PREMIUM_ACCEPTANCE_70.json`
- `docs/CAPABILITY_MATRIX.md`
- `docs/codebase/CONCERNS.md`
- `docs/GOAL_EXECUTION.md`
