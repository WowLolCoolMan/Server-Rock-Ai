# Goal execution ledger

This log records evidence-backed slices for the long-running implementation
goal. It is not a claim that the full handoff package is implemented.

## Gate 0 — live capability audit

Completed at commit `3e34156` before the next implementation slice.

- Current branch: `main`, synchronized with `origin/main`.
- The user-provided `ServerRock_AI_FINAL_COMPLETE_HANDOFF/` directory is
  untracked input and was preserved without modification.
- The 126-row combined register was inspected; it contains 86 premium
  requirements and 40 uploaded remediation issues, all marked
  `requires_live_repo_recheck`.
- A conservative domain capability matrix and dependency order are recorded in
  [`CAPABILITY_MATRIX.md`](CAPABILITY_MATRIX.md).
- No handoff document was treated as proof of implementation or release.

The next slice is the durable operation/task state contract, because it is a
dependency for workflow execution, activity visibility, cancellation,
reconciliation, parallel agents and recovery.

## Baseline

- Repository: `WowLolCoolMan/Server-Rock-Ai`
- Branch: `main`
- Baseline commit: `3e34156` (`feat: add bounded chat tags`)
- Working tree: clean apart from the preserved untracked handoff directory
- Host: Windows `10.0.26200`, x64
- SDK: .NET `10.0.401`
- Architecture preserved: Uno Platform/XAML native app with C#/.NET 10 Core

## Slice 1 — persisted light/dark theme

Status: `implemented_unverified`

Requirement alignment: the handoff targets light/dark themes and the live README
previously identified dark mode as unavailable. This slice adds a persisted
light/dark choice to Settings → Appearance, applies the selected native theme
to the application root and standardized dialogs, and switches the shared
Server Rock design-token palette without changing provider or workspace data.

Changed files:

- `native/ServerRock.Core/Models/AppSettings.cs`
- `native/ServerRock.Core/Infrastructure/JsonSettingsStore.cs`
- `native/ServerRock.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/UiTheme.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `README.md`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed (`Native core checks passed.`).
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `git diff --check` — passed.

Not yet verified:

- Live packaged Windows visual acceptance for light/dark switching.
- Linux/Skia visual acceptance and accessibility behavior.
- Every non-standardized dialog inheriting the selected theme at runtime.

Known repository gate:

- `npm run audit:native-ui` currently fails on ten existing focus/recovery
  contract checks covering project cancellation/focus, terminal keyboard
  navigation, GitHub retry/cancellation state, and command-review focus. Those
  failures are outside this theme slice and remain open for a later bounded
  UI-reliability slice.

Privacy/security/licensing: no new dependencies, network calls, credentials,
or external code were added. Theme state remains a non-secret local setting.

Rollback: revert the slice commit; the new `ThemeMode` property is additive and
older settings files continue to load as light mode.

## Slice 2 — Windows line-ending-safe native UI audit

Status: `verified_windows` (static contract audit only)

The native UI audit reported ten missing multiline contracts. Inspection showed
the source already contained the required contracts, but the checker compared
LF-only strings against CRLF files. The audit now normalizes CRLF to LF for
XAML and C# inputs before matching, preserving the same contract definitions
while removing Windows false negatives.

Changed file:

- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`

This improves evidence quality only; it does not replace live Windows/Linux UI
acceptance or accessibility testing. No new dependencies, network calls,
credentials, or runtime behavior were added.

Next unblocked slice: inspect the handoff-to-repository feature map and choose
the smallest missing user-facing capability that can be implemented with a
real backend and focused tests.

## Slice 3 — pinned conversations

Status: `implemented_unverified`

The handoff calls for pinned chats alongside folders and tags. Saved
conversations now persist a `Pinned` flag, pinned conversations sort above
other conversations within their mode/list, and the chat actions menu exposes
Pin/Unpin with truthful status text and focus-preserving rerender behavior.

Evidence:

- Core persistence test covers the pinned state through save/reload.
- Native UI build and full Core suite were green before this additive model/UI
  change; the focused suite will be rerun before commit.
- No new dependency, network call, credential, or external code was added.

Not yet verified: live Windows/Linux visual and keyboard acceptance of the pin
menu and ordering. Tags remain a separate future slice.

## Slice 4 — bounded chat tags

Status: `implemented_unverified`

Saved chats now support up to eight local-only tags, each limited to 32
characters. The existing chat action menu can edit tags, normalizes duplicate
and empty entries, shows tags under the chat title, and persists them through
the existing validated conversation store/archive path. Invalid imported or
saved tag lists are rejected before replacing healthy history.

Evidence:

- Core persistence test covers two tags through save/reload.
- Conversation storage validates tag count, length, control characters, and
  case-insensitive duplicates before atomic write.
- No new dependency, network call, credential, or external code was added.

Not yet verified: live Windows/Linux visual, keyboard, and screen-reader
acceptance of the tag editor and tag rendering.

## Slice 5 — durable operation state and journal foundation

Status: `implemented_unverified`

`ServerRock.Core` now has a small UI-independent operation contract with stable
run IDs, bounded instructions, explicit state transitions, monotonic per-run
event sequences, atomic JSON persistence, and restart reconciliation. In-flight
runs are changed to `needs_reconciliation` after restart; the journal never
replays uncertain Git, shell, provider, or other side effects automatically.
This is the foundation for a later workflow/task UI and parallel agent host.

Changed files:

- `native/ServerRock.Core/Models/OperationRun.cs`
- `native/ServerRock.Core/Infrastructure/OperationJournal.cs`
- `native/ServerRock.Core.Tests/OperationJournalTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence, privacy, and rollback will be appended after the focused suite and
native build complete. No external code, network call, credential, or
destructive data migration was introduced.

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including operation creation, monotonic transitions, invalid-transition rejection, restart reconciliation, idempotence, and explicit cancellation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the journal stores local task metadata and bounded
user instructions only; it adds no network traffic, credentials, third-party
code, or automatic side-effect replay. Atomic writes preserve the previous
journal if the process fails during a write.

Rollback: remove the additive operation journal files and test registration;
existing conversation, project, and settings stores remain unchanged.

Next dependency: expose durable operation state through an activity surface,
then add workflow execution only after approval, cancellation, and
reconciliation are visible to the user.

## Slice 6 — durable operation service in the chat journey

Status: `implemented_unverified`

The chat send flow now uses `OperationService` to create a durable operation,
record validating/queued/running transitions, and record completion, provider
failure, timeout, or user cancellation. The UI remains a client of the Core
service; it does not write journal events or choose transition rules directly.

Changed files:

- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Core.Tests/OperationServiceTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including service start, completion, cancellation, and the existing Core suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: operation instructions remain local bounded
metadata and use the existing atomic journal; no new network call, credential,
third-party code, or automatic side-effect replay was introduced.

Rollback: remove the service, test, and chat lifecycle calls; the existing
chat provider and conversation persistence paths remain independent.

Not yet verified: live cancellation/timeout visuals and activity-history UI.
The next slice is the user-visible activity surface and restart reconciliation
status.

## Slice 7 — activity and restart reconciliation surface

Status: `implemented_unverified`

The existing scheduled-task navigation seam now opens an Activity & tasks
surface backed by the durable operation service. It lists recent operation
previews, statuses, timestamps, and sequence checkpoints. Startup and surface
open both reconcile interrupted runs to `needs_reconciliation`; the UI tells
the user that no provider, shell, Git, or file side effect was replayed.
Future scheduling remains explicitly unimplemented rather than being implied
by the label.

Changed files:

- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/SidebarView.xaml`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including journal restart-reconciliation coverage.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed before commit.

Privacy/security/licensing: the surface displays bounded local instruction
previews and operation metadata only. Reconciliation is a journal transition
and never retries external side effects. No network call, credential, or
third-party code was added.

Rollback: revert the Activity & tasks navigation and service load/reconcile
calls; the underlying journal and chat lifecycle remain independently
recoverable.

Not yet verified: live Windows keyboard/screen-reader rendering and packaged
Linux parity. The next dependency is a real user decision path for
`needs_reconciliation` runs, followed by explicit workflow/schedule models.

## Slice 8 — reviewed reconciliation decisions

Status: `implemented_unverified`

Activity & tasks now gives a user-visible `needs_reconciliation` run two
explicit outcomes: mark cancelled or mark failed. Both outcomes append a
bounded reason event and close the dialog without resuming or replaying any
provider, shell, Git, or file side effect. The Core service rejects decisions
for any other status, keeping the recovery path state-safe.

Changed files:

- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Core.Tests/OperationServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including restart reconciliation and reviewed decision coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after fixing the automation metadata import.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the decision records only local operation metadata
and a fixed bounded reason. It does not expose secrets, resume uncertain work,
or add network access, credentials, or third-party code.

Rollback: revert the reconciliation decision method, test, and two Activity
buttons; unresolved runs remain visible as `needs_reconciliation` in the
existing history surface.

Not yet verified: live keyboard/screen-reader use of the decision buttons and
packaged Linux parity. The next dependency is a versioned workflow/task model
with approvals, retries, cancellation, and schedule semantics.

## Slice 9 — versioned workflow definition foundation

Status: `implemented_unverified`

`WorkflowService` now persists versioned workflow definitions and validates the
typed DAG before saving. Validation enforces bounded IDs and labels, supported
node types, exactly one root, connectivity, acyclicity, bounded configuration,
and indirect secret references. Duplicate `(workflowId, version)` entries are
rejected so edits must create a new version. This slice is a definition and
validation boundary only; no workflow is executed or scheduled yet.

Changed files:

- `native/ServerRock.Core/Models/WorkflowDefinition.cs`
- `native/ServerRock.Core/Services/WorkflowService.cs`
- `native/ServerRock.Core.Tests/WorkflowServiceTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including valid DAG persistence, cycle rejection, and inline-secret rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: workflow configuration is local bounded JSON and
rejects secret-like keys rather than persisting credential material. Atomic
writes preserve the previous file on write failure. No execution, network
access, credential use, or third-party code was added.

Rollback: remove the workflow model, service, and test registration; existing
operation, conversation, project, and settings stores are independent.

Not yet verified: workflow editor, dry-run, step runner, approval inbox,
idempotent retries, background scheduling, or packaged Linux parity. The next
dependency is durable per-step run state pinned to a workflow version before
any tool/provider execution is connected.

## Slice 10 — durable workflow step runs

Status: `implemented_unverified`

`WorkflowRunService` now creates durable runs pinned to a workflow ID and
version, tracks each node through pending/executing/waiting-approval/complete/
failed/cancelled states, persists approval IDs and event sequences, allows
retries only for explicitly safe node types, preserves stable idempotency keys,
and reconciles in-flight runs after restart. Approval completion and final
verification are recorded as events. No run method dispatches tools, shell
commands, Git, or model providers yet.

Changed files:

- `native/ServerRock.Core/Models/WorkflowRun.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including durable approval pause/resume, safe retry attempt increment, cancellation, and restart reconciliation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: run state and event payloads are bounded local
metadata. Retry is conservative and refuses non-idempotent tool-like steps;
approval identity is validated and recorded. No external dispatch, network
call, credential, or third-party code was added.

Rollback: remove the workflow run model, service, and tests; versioned
definitions and all existing application stores remain independent.

Not yet verified: workflow editor/dry-run, tool adapter capability gates,
parallel resource limits, background scheduler/OS restart behavior, approval
inbox UI, and packaged Linux parity. The next dependency is an explicit
workflow-run activity surface before connecting any executor.

## Slice 11 — workflow runs in Activity & tasks

Status: `implemented_unverified`

Activity & tasks now loads durable workflow runs alongside chat operations,
showing workflow ID/version, run status, each step state, attempt number, and
waiting approvals. Waiting approval steps expose an accessible Approve action
that records a stable UI approval identity and refreshes truthfully by closing
the surface; it does not dispatch a tool, provider, shell, Git, or file action.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed in the preceding workflow-run slice, including durable approval transitions and restart reconciliation.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the UI displays local run metadata and bounded
step IDs only. Approval is explicit and auditable; no external action is
implicitly authorized or dispatched. No network call, credential, or
third-party code was added.

Rollback: revert the workflow service fields and Activity rendering; durable
workflow runs remain available to Core callers and existing chat activity is
unchanged.

Not yet verified: live UI approval keyboard/screen-reader behavior, workflow
editor/dry-run, tool adapter capability gates, parallel limits, scheduling,
and packaged Linux parity. The next dependency is a dry-run/execution-plan
view that can explain graph order without invoking tools.

## Slice 12 — dependency-ordered workflow plan

Status: `implemented_unverified`

Workflow definitions now produce a deterministic topological execution plan.
Each durable step run stores its node type, label, dependency IDs, stable
idempotency key, and attempt number in plan order. Activity renders the
dependency context for each step, making the current run plan inspectable
without invoking any tool or provider. Cycles remain rejected at definition
validation, and no bounded retry node is implied.

Changed files:

- `native/ServerRock.Core/Models/WorkflowDefinition.cs`
- `native/ServerRock.Core/Models/WorkflowRun.cs`
- `native/ServerRock.Core/Services/WorkflowService.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowServiceTests.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including dependency-order plan and durable plan-order assertions.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the plan contains bounded local node metadata and
dependency IDs only. It cannot execute work, interpolate secrets, or change
provider routing. No network call, credential, or third-party code was added.

Rollback: revert plan fields, topological planning, rendering, and tests;
durable workflow runs remain readable only through their prior state fields.

Not yet verified: live graph editor/dry-run controls, tool adapter capability
gates, parallel limits, scheduling, and packaged Linux parity. The next
dependency is a read-only dry-run action in the Activity surface, followed by
executor adapters.

## Slice 13 — read-only workflow dry-run preview

Status: `implemented_unverified`

Activity & tasks now provides a Preview plan action for workflow runs. The
preview explains persisted dependency order, node type/label, approval gates,
safe-retry policy, current state, and attempt number. It explicitly performs
no model, tool, shell, Git, network, or file operation and does not mutate the
run.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the preview reads only already-persisted local
workflow metadata and makes no authorization or provider decision. No network
call, credential, or third-party code was added.

Rollback: remove the Preview plan button and dialog; the existing inline
workflow state and Core run service remain available.

Not yet verified: live keyboard/screen-reader behavior, graph editing,
tool-adapter capability gates, parallel limits, scheduling, and packaged Linux
parity. The next dependency is a capability-gated executor adapter for one
safe read-only node type.

## Slice 14 — capability-gated condition executor

Status: `implemented_unverified`

The workflow runtime now supports one explicitly safe executor: a pure
condition node that compares a bounded local input field with a bounded
expected value. It records the boolean result and completes the step/run when
appropriate. The run service rejects tool-call and other unsupported node
types through the same entry point, preventing accidental expansion of the
execution surface.

Changed files:

- `native/ServerRock.Core/Models/WorkflowRun.cs`
- `native/ServerRock.Core/Services/ReadOnlyConditionExecutor.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including local condition evaluation, durable result completion, and unsupported-node rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: execution is in-process, local, deterministic,
bounded, and has no filesystem, shell, network, provider, Git, or credential
access. Tool-like nodes are refused rather than silently downgraded. No
third-party code was added.

Rollback: remove the condition executor, persisted config fields, and tests;
workflow definitions and non-executing run state remain intact.

Not yet verified: richer conditions/branch semantics, tool adapter grants,
parallel limits, scheduler/OS restart behavior, full approval inbox, and
packaged Linux parity. The next dependency is to expose condition results and
capability refusal in Activity, then add another safe adapter only with a
separate permission contract.

## Slice 15 — workflow executor observability

Status: `implemented_unverified`

Condition step runs now persist their boolean result. Activity shows that
result and labels the executor as the local condition executor; every other
node type is labeled executor unavailable rather than appearing runnable.
This makes capability refusal visible without widening permissions.

Changed files:

- `native/ServerRock.Core/Models/WorkflowRun.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including persisted true condition result and unsupported-node refusal.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local result metadata is persisted;
unsupported nodes remain non-executable. No network, provider, shell, Git,
filesystem, credential, or third-party code path was added.

Rollback: remove the result field and Activity capability/result text; the
condition executor and prior run state remain usable.

Not yet verified: richer branching, tool adapter grants, parallel limits,
scheduler/OS restart behavior, full approval inbox, and packaged Linux parity.
The next dependency is a separate permission contract for any adapter beyond
pure local conditions.

## Slice 16 — exact-scope workflow capability contract

Status: `implemented_unverified`

Added a UI-free capability contract for future external workflow adapters.
Requests and grants must match the exact run ID, node ID, and scope; requested
capabilities must be in the explicit allowlist; the grant must cover the full
request and include an approval identity. Pure condition nodes are forbidden
from requesting external capabilities. The gate authorizes metadata only and
does not execute anything.

Changed files:

- `native/ServerRock.Core/Models/WorkflowCapability.cs`
- `native/ServerRock.Core/Services/WorkflowCapabilityGate.cs`
- `native/ServerRock.Core.Tests/WorkflowCapabilityGateTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including exact-scope acceptance, cross-run refusal, and condition external-capability refusal.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: capability names and scopes are bounded and
allowlisted; approval identity is required and no credential values are
accepted. The contract adds no network, shell, filesystem, provider, Git, or
third-party code path.

Rollback: remove the capability model, gate, and test registration; current
condition-only execution and workflow persistence remain unchanged.

Not yet verified: adapter-specific grants, approval inbox queuing, parallel
resource limits, scheduling, and packaged Linux parity. The next dependency
is to connect this contract to a deliberately read-only workspace adapter
with a separate effect audit.

## Slice 17 — capability-gated read-only workspace adapter

Status: `implemented_unverified`

Added `ReadOnlyWorkspaceAdapter`, which requires an exact run/node/scope grant
for `workspace.read`, reads a bounded supported text file through the existing
workspace path, symlink, extension, and protected-name safeguards, and returns
a `WorkspaceReadEffect` with relative path, character count, and
`Mutated=false`. It cannot write files or run commands.

Changed files:

- `native/ServerRock.Core/Models/WorkspaceReadEffect.cs`
- `native/ServerRock.Core/Services/ReadOnlyWorkspaceAdapter.cs`
- `native/ServerRock.Core.Tests/ReadOnlyWorkspaceAdapterTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including approved read/effect reporting, cross-run grant refusal, and protected-file refusal.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the adapter is local-only, bounded, read-only,
scope-authorized, and reuses existing symlink/path/protected-file controls.
No network, shell, provider, Git, credential, or third-party code path was
added.

Rollback: remove the adapter, effect model, and tests; the capability gate and
existing Coding Agent file controls remain unchanged.

Not yet verified: adapter invocation from a workflow run, effect timeline UI,
write/shell/network adapters, parallel limits, scheduling, and packaged Linux
parity. The next dependency is wiring this adapter to a workflow step with a
durable effect event while preserving approval and cancellation boundaries.

## Slice 18 — durable workspace-read workflow effect

Status: `implemented_unverified`

`WorkflowRunService.ReadWorkspaceStep` now wires the exact-scope read grant to
an executing tool-call step and the read-only workspace adapter. Successful
reads complete the step and persist a `workspace.read_completed` event with
node ID, relative path, character count, and `mutated=false`; file contents and
absolute paths are not placed in the event payload. A completed final step
advances through verifying to completed.

Changed files:

- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including workflow adapter invocation and durable non-mutating effect event coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the adapter remains local, read-only, bounded,
path-safe, and grant-scoped. Effect events deliberately omit file contents
and absolute paths. No network, shell, provider, Git, credential, or
third-party code path was added.

Rollback: remove `ReadWorkspaceStep` and its test; the standalone adapter and
condition-only executor remain available without changing existing files.

Not yet verified: effect timeline UI, write/shell/network adapters, approval
inbox queuing, parallel limits, scheduling, and packaged Linux parity. The
next dependency is exposing this durable effect in Activity before any new
side-effecting adapter is considered.

## Slice 19 — workflow effect timeline in Activity

Status: `implemented_unverified`

Activity & tasks now renders a bounded effect timeline from the persisted
workflow event journal. It summarizes workspace reads (relative path,
characters, no mutation), condition outcomes, approvals, reconciliation,
completion, and cancellation, capped to the latest 12 events per run. Raw
file contents, absolute paths, and arbitrary payloads are not rendered.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the timeline is local-only, bounded, and uses an
allowlisted event-summary mapping. It deliberately omits raw event payloads
and file contents. No network, credential, shell, provider, Git, or
third-party code path was added.

Rollback: remove the event-summary rendering block; persisted workflow events
remain available to Core callers.

Not yet verified: live timeline keyboard/screen-reader behavior, approval
inbox queuing, write/shell/network adapters, parallel limits, scheduling, and
packaged Linux parity. The next dependency is a UI-visible approval inbox
model for concurrent workflow requests.

## Slice 20 — durable approval inbox

Status: `implemented_unverified`

`WorkflowRunService.LoadPendingApprovals` derives up to 100 pending approval
requests from durable runs, ordered by request time. Activity & tasks now
renders one bounded Pending approvals section before individual workflow
details; each accessible Approve action targets the exact run and node and
uses the existing approval event path. Concurrent requests do not require
nested modal dialogs.

Changed files:

- `native/ServerRock.Core/Models/WorkflowApprovalRequest.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including two concurrent approvals surfaced through one inbox.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the inbox shows bounded local workflow/node IDs
and labels only; approval remains exact-run/node scoped and auditable. No
network, credential, shell, provider, Git, or third-party code path was added.

Rollback: remove the pending-request model/query and inbox rendering; per-run
approval controls and durable run events remain available.

Not yet verified: live concurrent approval keyboard/screen-reader acceptance,
approval expiry/denial, write/shell/network adapters, parallel limits,
scheduling, and packaged Linux parity. The next dependency is durable approval
denial/expiry semantics before any broader adapter surface.

## Slice 21 — approval denial and expiry

Status: `implemented_unverified`

Workflow approvals now support explicit bounded-reason denial, which marks the
step denied, fails the run, and records `approval.denied`. Pending approvals
older than the 30-minute lifetime are marked expired, fail their run, and
record `approval.expired`. Activity expires stale requests before rendering
the inbox, and exposes accessible Approve and Deny actions for current
requests.

Changed files:

- `native/ServerRock.Core/Models/WorkflowRun.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including durable denial and 30-minute expiry event coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: denial reasons are bounded local metadata;
expired approvals cannot be acted on. No network, credentials, shell,
provider, Git, or third-party code path was added.

Rollback: remove denial/expiry transitions and inbox actions; existing approve
and reconciliation paths remain available.

Not yet verified: live keyboard/screen-reader approval acceptance, approval
identity display/expiry messaging, write/shell/network adapters, parallel
limits, scheduling, and packaged Linux parity. The next dependency is a
durable approval identity and visible expiry state in the effect timeline.

## Slice 22 — approval identity and expiry visibility

Status: `implemented_unverified`

Pending approval records now expose a deterministic expiry timestamp derived
from the 30-minute lifetime. The Activity inbox displays it, while event
summaries show shortened approval identities for grants and explicit outcomes
for denial and expiry. Denial reasons remain bounded; expired requests are
not actionable.

Changed files:

- `native/ServerRock.Core/Models/WorkflowApprovalRequest.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including expiry timestamp assertions, denial, and expiration events.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local identities, timestamps, and
denial metadata are displayed. No credentials or file contents are exposed;
no network, shell, provider, Git, or third-party code path was added.

Rollback: remove expiry metadata and event-summary additions; durable approval
denial/expiry transitions remain intact.

Not yet verified: live keyboard/screen-reader behavior, approval countdown
UX, write/shell/network adapters, parallel limits, scheduling, and packaged
Linux parity. The next dependency is broader branching semantics or another
adapter only after its separate permission contract.

## Slice 23 — bounded chat message queue

Status: `implemented_unverified`

Chat now accepts follow-up sends while a response is active. A bounded FIFO
queue holds up to eight validated requests, preserves text and attachments,
reports queue position, and automatically dispatches the next request only
after the active request reaches its cleanup path. Full queues are refused
without losing the current draft or response.

Changed files:

- `native/ServerRock.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Core.Tests/ChatMessageQueueTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including FIFO ordering, attachment preservation, capacity refusal, and drain behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: queued requests stay in local process memory and
use existing attachment consent/provider paths when dispatched. The queue is
bounded and does not add network routing or credential storage. No third-party
code was added.

Rollback: remove the queue service, test, field, and SendClick/finally hooks;
the existing single-request chat flow remains unchanged.

Not yet verified: live multi-send keyboard/accessibility behavior, queue
cancel/reorder controls, persistence across app restart, and packaged Linux
parity. The next dependency is a user-visible queue management action before
adding persistent or parallel message behavior.

## Slice 24 — chat queue management controls

Status: `implemented_unverified`

The active-response queue now exposes a Queue control in the runtime status
banner when items exist. Users can inspect bounded prompt previews and
attachment counts, remove individual queued items, or explicitly clear the
entire queue. FIFO dispatch remains unchanged for retained items; clearing the
queue does not stop or alter the active response.

Changed files:

- `native/ServerRock.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.Chat.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including queue FIFO/capacity/drain coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.
- Live Windows check: the built `Server Rock: AI` window exposed accessible names for the message composer, Send message, Activity & tasks, and the Queue control contract. End-to-end queue interaction was not claimed because the automation helper returned no click geometry for the Send action while multiple native windows were present.

Privacy/security/licensing: queue contents remain local process memory and the
management surface renders bounded previews only. Explicit clear is reversible
only by resubmitting; it never changes the active request. No network,
credential, provider, shell, Git, or third-party code was added.

Rollback: remove Queue control, management dialog, and queue mutation methods;
bounded automatic FIFO dispatch remains available.

Not yet verified: live keyboard/screen-reader queue interaction, reorder or
persisted queue behavior, and packaged Linux parity. The next dependency is
live accessibility validation before adding persistence or parallel dispatch.

## Slice 25 — durable chat operation provider provenance

Status: `implemented_unverified`

Chat operation records now persist the selected model provider, model ID, and a
bounded endpoint privacy classification (`local`, `remote`, or `unknown`). The
Activity & tasks surface shows this provenance without exposing endpoint URLs,
so a user can tell which configured route handled a request after the response
has completed or failed. Existing journals remain readable because missing
legacy fields normalize to `unknown` in memory.

Changed files:

- `native/ServerRock.Core/Models/OperationRun.cs`
- `native/ServerRock.Core/Infrastructure/OperationJournal.cs`
- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Core.Tests/OperationServiceTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including provider/model/local provenance persistence.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only bounded provider/model labels and a local or
remote classification are persisted and rendered. Endpoint URLs, API keys,
and prompt contents are not added to the Activity metadata. No third-party
code or new network path was added.

Rollback: remove the provenance fields, journal metadata, and Activity label;
the existing durable operation lifecycle remains usable with unknown metadata.

Not yet verified: provenance for non-chat workflow/provider attempts, live
Activity accessibility behavior, capability-aware routing, and packaged Linux
parity. The next dependency is provider capability/profile modeling before
implementing fallback or multi-provider routing.

## Slice 26 — conservative model capability profiles

Status: `implemented_unverified`

The core provider path now resolves a bounded capability profile for the
selected provider/model: provider label, model label, local/remote endpoint
privacy, streaming support, and vision support confidence. Unknown models stay
allowed so compatible OpenAI-style servers are not silently rejected. Model
identifiers with definitive non-vision markers such as embedding, reranker,
ASR, or TTS are rejected before an image-bearing request reaches the network.

Changed files:

- `native/ServerRock.Core/Models/ModelCapabilityProfile.cs`
- `native/ServerRock.Core/Infrastructure/ModelCapabilityResolver.cs`
- `native/ServerRock.Core/Services/OpenAiCompatibleProvider.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including local/remote profile facts, unknown-model tolerance, definitive non-vision detection, and pre-network image rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the resolver uses only the configured provider,
model, and endpoint locality; it stores no credentials and emits no endpoint
URL. Definitive rejection happens before request serialization/network access.
No third-party code was added.

Rollback: remove the profile model, resolver call, and focused tests; existing
provider validation and compatible unknown-model behavior remain available.

Not yet verified: user-editable capability overrides, provider capability
discovery, fallback consent, multi-provider routing, live accessibility, and
packaged Linux parity. The next dependency is an explicit profile/override
surface with durable validation before adding fallback behavior.

## Slice 27 — persisted vision capability override

Status: `implemented_unverified`

Settings now provide a local Vision capability profile choice: automatic
conservative detection, explicitly supports images, or explicitly does not
support images. The choice is normalized and persisted with the existing
settings store, exposed in the Advanced settings page with an accessible name
and help text, and honored by provider preflight. This lets a user correct a
model-name heuristic without silently changing endpoint routing or sending
metadata to a server.

Changed files:

- `native/ServerRock.Core/Models/AppSettings.cs`
- `native/ServerRock.Core/Infrastructure/JsonSettingsStore.cs`
- `native/ServerRock.Core/Infrastructure/ModelCapabilityResolver.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including settings round-trip and explicit capability override behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the override is a non-secret local preference;
automatic detection and the override run before request serialization. No
endpoint URL, credential, prompt, or third-party code is added.

Rollback: remove the override setting/control and resolver branch; automatic
conservative detection remains the default.

Not yet verified: provider capability discovery, user-editable profiles for
streaming/context limits, fallback consent, multi-provider routing, live
settings accessibility, and packaged Linux parity. The next dependency is
capability discovery/profile validation before selecting another route.

## Slice 28 — bounded provider model capability discovery

Status: `implemented_unverified`

The OpenAI-compatible model discovery path now has a profile-aware API that
parses bounded provider metadata without changing the existing model-ID API.
It recognizes explicit `supports_vision`, `capabilities`, and `modalities`
signals, limits each advertised label and list, drops malformed or oversized
model IDs, and preserves `unknown` when the provider does not declare vision
support. Existing connection and selection callers continue using the legacy
string list, while capability-aware callers can opt into descriptors.

Changed files:

- `native/ServerRock.Core/Models/ProviderModelDescriptor.cs`
- `native/ServerRock.Core/Services/OpenAiCompatibleProvider.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including explicit capability metadata, boolean vision flags, unknown metadata, and existing model-ID discovery.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only provider-returned bounded model labels and
capability strings are retained in memory; credentials and endpoint URLs are
not returned in descriptors. Malformed metadata is ignored rather than trusted.
No third-party code was added.

Rollback: retain `DiscoverModelsAsync` and remove the descriptor API/parser;
existing connection and model selection behavior remains unchanged.

Not yet verified: UI display of discovered capability evidence, provider
discovery beyond OpenAI-compatible `/models`, streaming/context-limit
discovery, fallback consent, multi-provider routing, live accessibility, and
packaged Linux parity. The next dependency is surfacing descriptor evidence in
model selection before routing decisions consume it.
