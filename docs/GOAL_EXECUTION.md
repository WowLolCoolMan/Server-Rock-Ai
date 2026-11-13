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

Live validation note: Windows accessibility inspection confirmed the bounded
checkbox picker and its local-only disclosure. A manual coordinate journey
created a task but persisted an empty dependency list, so live dependency
selection is deliberately not claimed as verified. The implementation now
captures selected IDs at `ContentDialog.PrimaryButtonClick` before Uno closes
the dialog; a deterministic packaged/UI harness journey remains required.

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

## Slice 30 — durable queued follow-up messages

Status: `implemented_unverified`

Queued follow-up requests are now saved atomically in local app data and
restored at startup. The active response is never persisted or replayed: a
request is removed from the durable queue immediately before dispatch. Queue
mutations (enqueue, dequeue, remove, and clear) update the bounded store, and
restored requests remain available through the existing Queue management
surface for user review before sending.

Changed files:

- `native/ServerRock.Core/Infrastructure/ChatMessageQueueStore.cs`
- `native/ServerRock.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Core.Tests/ChatMessageQueueStoreTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including FIFO/attachment round-trip and persisted empty-queue behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: queued prompts and explicitly attached content
remain local app data, bounded by the existing eight-item and attachment
limits. Persistence adds no network or credential path. Corrupt queue data is
left untouched and reported; it is not overwritten with an empty queue.
No third-party code was added.

Rollback: remove queue-store load/save calls and the local queue store; the
in-memory FIFO behavior remains available.

Live Windows evidence: after stopping the native app while a response was still
active, the persisted follow-up remained in `queued-messages.json`; relaunching
the rebuilt app exposed `Restored 1 queued message; review the Queue control
before sending.` and an accessible `Manage queued messages` button. The startup
rendering path required an additional `UpdateQueueControl()` call, recorded in
the follow-up fix below. End-to-end completion of the active request and
automatic queued dispatch was not claimed because the local model response did
not complete during the bounded observation window.

Not yet verified: queue reordering, cross-device synchronization, durable
context continuity, packaged Linux parity, and live end-to-end send/dispatch
acceptance. The next dependency is queue reordering only after the restored
Queue surface receives a complete live send/dispatch journey.

## Slice 31 — restore queued-message controls on startup

Status: `implemented_unverified`

Startup now refreshes the Queue control after loading durable queued requests,
so restored items are immediately visible and actionable instead of remaining
hidden in memory. This closes the live restart visibility defect found during
the Slice 30 Windows check without auto-sending restored messages.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.
- Live Windows check: after relaunch, the accessibility tree exposed `Restored 1 queued message; review the Queue control before sending.` and `Manage queued messages` while the queue file still contained the bounded follow-up.

Privacy/security/licensing: startup restoration remains local-only, bounded,
and review-first. It does not replay an active request, transmit queue content,
or add credentials/network access. No third-party code was added.

Rollback: remove the startup `UpdateQueueControl()` call; durable data remains
readable but restored items would again require another queue mutation to make
the control visible.

Not yet verified: live completion/dispatch acceptance, queue reordering,
cross-device synchronization, durable context continuity, and packaged Linux
parity. The next dependency is a complete user-driven live queue journey.

## Slice 32 — live restored-queue dispatch evidence

Status: `implemented_unverified`

The live Windows journey was extended using the restored queued follow-up. A
first request was submitted while the restored message remained queued. The
first request reached a provider failure, and the queued follow-up then
appeared in the conversation and was automatically dispatched. The durable
queue file was empty after dispatch, proving the queued item was removed before
the next send. Both local provider calls returned an empty stream, so this
slice records queue ordering and failure-path dispatch only, not successful
model-response acceptance.

Evidence:

- Live Windows accessibility tree showed the restored queued message and
  `Manage queued messages` before the first send.
- Live Windows accessibility tree then showed the first user request, its
  `Connection error`, the restored `Queued follow-up validation message`, and
  the second `Connection error` after automatic dispatch.
- `C:\Users\basic\AppData\Roaming\ServerRock\AI\queued-messages.json` was
  `[]` after the dispatch path.
- The local `/v1/models` endpoint returned HTTP 200 before the journey, but
  the selected model closed both chat streams without content; provider
  availability is therefore not claimed.

Privacy/security/licensing: the journey used local model routing only. Queue
content stayed in local app data, no remote endpoint or credential was added,
and failure handling did not replay the request indefinitely. No third-party
code was added.

Rollback: no production behavior change was made in this evidence slice;
retain the existing queue and provider failure behavior.

Not yet verified: successful live provider response, queue reordering,
cross-device synchronization, durable context continuity, and packaged Linux
parity. The next dependency is provider/runtime health or a deterministic
local test endpoint before claiming successful end-to-end dispatch.

## Slice 33 — queued-message reorder controls

Status: `implemented_unverified`

The Queue management dialog now supports bounded up/down reordering alongside
remove and clear. Moves rebuild the same validated FIFO queue, persist through
the existing atomic queue store, and disable at the first/last boundary. The
active response is unaffected; only messages waiting for dispatch change order.

Changed files:

- `native/ServerRock.Core/Services/ChatMessageQueue.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Core.Tests/ChatMessageQueueTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including valid move ordering, invalid/no-op rejection, attachment preservation, persistence, and drain behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: reordering stays local and bounded; it neither
changes the active request nor adds network, credential, shell, Git, or
third-party behavior.

Rollback: remove the Move method and three Queue dialog controls; existing
FIFO, remove, clear, and durable queue behavior remains available.

Not yet verified: live reorder accessibility, richer search, durable context
continuity, successful live provider-response acceptance, and packaged Linux
parity. The next dependency is live reorder acceptance or a separate
conversation-search slice.

## Slice 34 — richer local conversation search

Status: `implemented_unverified`

Saved-chat search now uses a bounded Core service and searches title, project,
folder, tags, message text, attachment filenames, and web-source title/URL
metadata. Results remain local and are capped at 100 recent hits. The search
dialog displays archive/folder/tag context without exposing full message text,
and oversized or control-character queries are rejected safely.

Changed files:

- `native/ServerRock.Core/Services/ConversationSearchService.cs`
- `native/ServerRock.Native/MainPage.Composer.cs`
- `native/ServerRock.Core.Tests/ConversationSearchTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including tag, attachment, source metadata, recent ordering, and query-bound coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: search is local-only and does not send query text
or saved content to a provider. Results expose bounded labels and organization
metadata, not full message bodies. No third-party code was added.

Rollback: restore the inline title/message/project predicate and list binding;
saved conversations remain unchanged.

Not yet verified: live search dialog accessibility, durable context
continuity, successful live provider-response acceptance, and packaged Linux
parity. The next dependency is live search acceptance or context continuity.

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

## Slice 29 — visible selected-model capability evidence

Status: `implemented_unverified`

The live connection refresh now uses the profile-aware discovery API and
reports the selected model's provider-advertised vision state in the runtime
status: `vision advertised`, `non-vision metadata`, or `vision capability
unknown`. Existing model fallback and connection state behavior is unchanged;
the evidence is informational and does not silently route, upload, or alter
the user's model choice.

Changed files:

- `native/ServerRock.Native/MainPage.Connection.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including provider discovery and all existing connection/provider coverage.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the status uses only bounded metadata returned by
the configured provider. It does not expose credentials or endpoint URLs in
the capability label, and it does not enable fallback or additional routing.
No third-party code was added.

Rollback: restore the legacy model-ID discovery call and generic connected
status; model selection and connection checks remain functional.

Not yet verified: live runtime status accessibility, provider metadata support
beyond `/models`, streaming/context-limit discovery, fallback consent,
multi-provider routing, and packaged Linux parity. The next dependency is a
tested capability-aware selection decision, still requiring explicit consent
before any alternate route is used.

## Slice 35 — live conversation search journey

Status: `implemented_verified_windows`

The bounded local conversation search added in Slice 34 was exercised in the
live Windows native app. The search surface exposed an accessible dialog title,
query field, result list, and Open chat action. Searching for `FIRST_DONE`
returned the saved conversation `Reply with exactly: FIRST_DONE`; opening it
loaded the conversation and displayed the persisted `Queued follow-up
validation message`.

Evidence:

- Live Windows accessibility inspection — passed: `Search your workspace`,
  `Search chats in both workspaces`, the matching result, and `Open chat` were
  exposed; the opened conversation displayed both saved user messages.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (83 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: search remains local-only and bounded; the live
journey used saved conversation metadata/content and did not send a query to a
provider. No third-party code was added.

Rollback: revert `c13db14` and the associated documentation evidence; the
existing conversation list remains available without the richer search path.

Not yet verified: durable context continuity, successful live provider
response acceptance, Linux parity, and packaged-release acceptance.

## Slice 36 — accessible workflow approval countdown

Status: `implemented_verified_with_live_surface_boundary`

Workflow approvals now expose a bounded remaining-time label in the Activity &
tasks surface. The label reports sub-minute urgency, rounded minutes/hours, or
`Expired`; a five-second native timer refreshes it, marks the status as a
polite accessibility announcement, and disables Approve/Deny after expiry.
The pure timing policy is covered independently so UI rendering does not own
expiry arithmetic.

Changed files:

- `native/ServerRock.Core/Services/WorkflowApprovalTiming.cs`
- `native/ServerRock.Core.Tests/WorkflowApprovalTimingTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including sub-minute, minute, hour, and expired countdown cases.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (85 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed for the `Activity & tasks`
  dialog and existing reconciliation controls. The current local state had no
  pending approval, so the countdown text and disabled-after-expiry behavior
  were not claimed as live-rendered evidence.

Privacy/security/licensing: the countdown is derived from locally persisted
approval timestamps; it does not call a provider, execute a tool, or add a
credential. Expiry disables the visible actions and the existing durable
service remains the authority for approval state.

Rollback: remove the timing helper, timer, and two audit contracts; the prior
fixed expiry timestamp and durable expiry service remain available.

Not yet verified: a seeded live pending-approval countdown, workflow write/
shell/network adapters, richer branching, parallel limits, scheduling, full
audit/export, Linux parity, and packaged-release acceptance.

## Slice 37 — global command palette

Status: `implemented_verified_windows`

The native shell now opens a searchable command palette with `Ctrl+K`. It
filters existing routed actions by title and keywords, exposes the query and
results through accessibility names, restores composer focus when dismissed,
and executes the selected action only after the palette closes. The first live
journey filtered for `settings` and opened the existing Settings surface;
provider, file, Git, and model behavior remains owned by their existing routes.

Changed files:

- `native/ServerRock.Native/MainPage.CommandPalette.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- Live Windows accessibility inspection — passed: `Ctrl+K` exposed
  `Command palette`, `Search command palette actions`, `Command palette
  results`, and the full routed action list; filtering `settings` left
  `Connection & settings`, and running it opened the Settings dialog.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (89 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the palette searches a fixed local action catalog
and does not transmit query text, invoke a provider, or add a dependency.
Actions retain their existing consent and permission gates.

Rollback: remove `MainPage.CommandPalette.cs`, the `Ctrl+K` branch, and the
three audit contracts; all existing sidebar and slash-command routes remain.

Not yet verified: navigation back/forward history, persistent tabs/layout,
inspector state, live resize/focus acceptance at every target width, Linux
parity, and packaged-release acceptance.

## Slice 38 — live compatible local-provider response

Status: `implemented_verified_windows_with_model_compatibility_boundary`

The live Windows chat journey was tested against the configured local
OpenAI-compatible endpoint. The original selected model,
`huihui_ai/Qwen3.8-abliterated:latest`, was advertised by `/v1/models` but
closed the stream without returning content; the app rendered the distinct
`The model server closed the stream without returning a response.` failure.
Using the existing Settings route, a locally installed compatible model,
`wtwk-qwen35-4b:latest`, connected successfully and returned `LIVE_OK` in the
native conversation, including Copy and Retry actions. The original model
selection was restored after validation.

Evidence:

- Local runtime inventory — passed: `/api/tags` and `/v1/models` exposed five
  local models; no remote endpoint was used.
- Direct local OpenAI-compatible probe — passed for
  `wtwk-qwen35-4b:latest`, returning `RUNTIME_OK`.
- Live Windows app — passed for Settings model selection, connection status,
  prompt submission, assistant `LIVE_OK` rendering, and Copy/Retry actions.
- Live Windows app — failed for the original selected model with the truthful
  stream-closure error; this is retained as a model-specific compatibility
  limitation rather than treated as a generic provider success.
- The original `huihui_ai/Qwen3.8-abliterated:latest` selection was restored
  before ending the live validation.

Privacy/security/licensing: all probes and the successful response stayed on
the local Ollama endpoint. No prompt, response, credential, or context was
sent to a cloud provider, and no new dependency was added.

Rollback: no source rollback is required; the only runtime setting changed
during validation was restored to its original value.

Not yet verified: durable restart/reopen of this successful response, automatic
model compatibility recovery, explicit fallback consent, multi-provider
routing, Linux parity, and packaged-release acceptance.

## Slice 39 — explicit local model recovery after provider failure

Status: `implemented_verified_windows`

When a local model request fails, the conversation error now offers an
accessible `Choose another local model` action. The action is rendered only
for loopback/localhost endpoints, opens the existing installed-model library,
and explicitly does not switch models automatically. Remote failures retain
the existing Settings guidance, so this slice cannot silently route prompts or
context to another provider.

Changed files:

- `native/ServerRock.Native/MainPage.Messages.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (91 contracts).`
- `git diff --check` — passed.
- Live Windows failure journey — passed: the incompatible local model
  produced the truthful stream-closure message and accessible recovery button;
  activating it opened `Model Library`, whose accessible results listed five
  installed local models.

Privacy/security/licensing: the recovery action is gated by
`LocalEndpoint.IsLocal`; it reads the existing local model inventory and sends
no prompt or workspace context anywhere. No third-party code or dependency was
added.

Rollback: remove the optional message-content callback and recovery button;
the existing error bubble and Settings guidance remain unchanged.

Not yet verified: automatic compatibility detection, an explicit fallback
confirmation flow for switching, multi-provider routing, durable provider
identity across fallback attempts, Linux parity, and packaged-release
acceptance.

## Slice 40 — explicit fallback confirmation

Status: `implemented_verified_windows`

The local failure recovery path now carries an explicit confirmation boundary.
When the user selects another installed local model from the recovery library,
the app shows `Switch local model?`, names the current and proposed models, and
states that the change affects only the selected local model with no cloud
route or automatic retry. Choosing Keep current model leaves the preference
unchanged; only Switch model calls the existing selection path.

Changed files:

- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (94 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: failure recovery opened
  Model Library; selecting `wtwk-qwen35-4b:latest` exposed the accessible
  confirmation with current/target models and explicit no-cloud language;
  `Keep current model` preserved `huihui_ai/Qwen3.8-abliterated:latest`.

Privacy/security/licensing: switching remains local-only and user initiated.
The confirmation prevents an implicit model change after a provider failure;
no prompt, context, credentials, or third-party code was added.

Rollback: remove the optional confirmation parameter and dialog; the existing
local model library and explicit recovery chooser remain available.

Not yet verified: automatic compatibility detection, multi-route provider
selection, durable provider identity across fallback attempts, Linux parity,
and packaged-release acceptance.

## Slice 41 — model-specific provider failure classification

Status: `implemented_verified_windows`

Provider failures now have a bounded Core classification separating endpoint
unavailability, server rejection, image rejection, timeout, unknown errors,
and the model-compatibility case where a stream closes without content. The
native chat surface uses that classification to announce `Model compatibility
issue` for the failing model, preserve the local recovery chooser, and record a
model-compatibility operation failure reason without attempting an automatic
switch.

Changed files:

- `native/ServerRock.Core/Infrastructure/ProviderErrorText.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including model, endpoint, image, and timeout classification cases.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (96 contracts).`
- `git diff --check` — passed.
- Live Windows provider failure — passed: the original local model's closed
  stream exposed `Model compatibility issue · choose another local model or
  check Settings` and retained the accessible recovery action.

Privacy/security/licensing: classification uses only the local exception and
request context already in memory. It does not inspect or transmit prompt
content, does not select a route, and adds no dependency.

Rollback: remove `ProviderFailureKind` and restore the generic failure status;
existing error descriptions and explicit recovery remain available.

Not yet verified: proactive compatibility detection before a request,
multi-route provider selection, durable provider identity across fallback
attempts, Linux parity, and packaged-release acceptance.

## Slice 108 — preserve cancellation for approved workspace commands

Status: `implemented_verified_core`

The approved workspace-command path now has a regression check for user
cancellation in addition to its timeout check. Cancelling an in-flight command
must terminate the process tree, wait for the process to exit, and propagate
`OperationCanceledException` to the caller. This confirms the cancellation
boundary does not leave a background command running or silently convert a
user cancellation into a successful result.

Changed files:

- `native/ServerRock.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including `project command cancellation kills the process tree and propagates cancellation`.
- `git diff --check` — passed.

Privacy/security/licensing: this slice adds no provider request, credential,
network route, dependency, or new execution capability. It verifies the
existing explicit-approval command runner's local cancellation behavior only.
The command runner still is not an OS sandbox; that limitation remains
explicit in the capability matrix.

Rollback: remove the focused cancellation test and the matrix wording; the
existing timeout and process-tree cleanup implementation remains unchanged.

Not yet verified: OS-level sandboxing, richer tool transport, cross-platform
PTY acceptance, parallel-agent isolation, Linux parity, and packaged-release
acceptance.

## Slice 109 — live Activity & Tasks action surface rechecked

Status: `verified_windows_accessibility_inspection`

The current Windows desktop build was launched from the synchronized checkout
and Activity & Tasks was opened through the command palette. The live
accessibility tree exposed the bounded action surface and the existing local
evidence records: Export activity evidence, Search activity evidence, Schedule
task, completed scheduler work with the explicit no-provider-dispatch outcome,
provider-attempt run IDs and sequence numbers, and the two interrupted-operation
decisions (mark cancelled or failed).

Evidence:

- `npm run start:native` — launched the current `net10.0-desktop` build.
- Live Windows accessibility inspection — passed for command-palette routing
  and the Activity & Tasks action/evidence surface.
- The inspection did not mutate tasks, export data, send a provider request, or
  choose an interrupted-operation decision.

Privacy/security/licensing: inspection read existing local activity records and
bounded labels only. No prompt, credential, provider route, network request,
or third-party dependency was added.

Rollback: documentation-only; remove this evidence entry if a later live run
contradicts the observed surface.

Not yet verified: keyboard activation of every Activity action, seeded live
workflow-event search matching, full status-history keyboard/screen-reader
acceptance, OS session hosting, parallel-agent artifacts, Linux parity, and
packaged acceptance.

## Slice 110 — make Activity dialog entry keyboard predictable

Status: `implemented_verified_build`

The dynamically created Activity & Tasks dialog now has an explicit accessible
name and moves focus to the non-mutating Export evidence action when it opens.
This gives keyboard and assistive-technology users a deterministic entry point
while leaving task scheduling, run-now, approval, and reconciliation actions
explicitly user initiated.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (148 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: this is a local focus and accessibility change.
It adds no provider request, credential, network route, task mutation,
dependency, or third-party code.

Rollback: remove the dialog accessible-name/focus wiring and the two audit
contracts; the existing Activity actions remain available with their existing
explicit approval behavior.

Not yet verified: live keyboard activation of every Activity action, seeded
live workflow-event search matching, full status-history keyboard/screen-reader
acceptance, OS session hosting, parallel-agent artifacts, Linux parity, and
packaged acceptance.

## Slice 112 — make status history keyboard-readable

Status: `implemented_verified_build`

The actionable status-history dialog now has an explicit accessible name and a
focusable, named scroll region containing the bounded history entries. Opening
the dialog moves focus to that region so keyboard and screen-reader users land
on the status content instead of an unspecified dialog surface.

Changed files:

- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (153 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: status history remains local, bounded, and
read-only. This slice adds no provider request, credential, network route,
task mutation, dependency, or third-party code.

Rollback: remove the named scroll region, dialog focus handler, and their audit
contracts; the existing status-history dialog remains available.

Not yet verified: live Windows keyboard/screen-reader acceptance of status
history, live keyboard activation of every Activity action, seeded live
workflow-event search matching, OS session hosting, parallel-agent artifacts,
Linux parity, and packaged acceptance.

## Slice 113 — add bounded dependency-aware agent task artifacts

Status: `implemented_verified_core`

The Core layer now has a local-only `AgentTaskArtifactStore` for structured
agent task planning. It persists bounded task metadata, specialist roles,
parent operation IDs, dependency IDs, terminal result summaries, and bounded
artifact labels. Tasks can move from proposed to queued, a ready queued task
can be claimed for a future executor only after all dependencies are completed,
and terminal completion/failure/cancellation remains explicit. Search is local
and bounded. The store does not invoke a provider, shell, Git, file tool, MCP
server, sub-agent, or background process.

Changed files:

- `native/ServerRock.Core/Models/AgentTaskArtifact.cs`
- `native/ServerRock.Core/Services/AgentTaskArtifactStore.cs`
- `native/ServerRock.Core.Tests/AgentTaskArtifactTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including dependency gating, persistence/reload, search, and unknown-dependency rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (153 contracts).`
- `npm run check:syntax` — passed.
- `npm run check:hygiene` — passed: 326 tracked files.
- `npm run check:release-config` — passed.
- `npm run audit:future-features` — passed: 159 items, 11 sections, 0 checked.
- `git diff --check` — passed.

Privacy/security/licensing: task records contain only bounded local metadata;
they do not store credentials, prompt attachments, command output, file
contents, or provider responses. No new dependency, network route, or
execution capability was introduced.

Rollback: remove the task model/store/test and the Program registration, then
restore the prior Agent workbench matrix line. Existing operation journaling
and reviewed Coding Agent flows remain independent.

Not yet verified: task-artifact UI, actual sub-agent delegation, parallel
execution limits, process/container/worktree isolation, background execution,
cross-platform acceptance, and packaged release acceptance.

## Slice 114 — surface agent task artifacts in Activity & Tasks

Status: `implemented_verified_build`

Activity & Tasks now includes a read-only `AGENT TASK ARTIFACTS` section backed
by the bounded local task store. It shows status, specialist role, updated time,
title, dependency count, bounded result summaries, and artifact labels. Empty
state copy makes the no-dispatch boundary explicit. No button or timer executes
tasks, starts a model, invokes a tool, or grants a capability.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (154 contracts).`
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the Activity section reads bounded local metadata
only. It does not expose credentials, file contents, command output, prompts,
provider responses, or absolute paths, and adds no network or execution path.

Rollback: remove the task-store field, read-only Activity section, and audit
contract; the durable task store and existing Activity operation records remain
independent.

Not yet verified: live packaged Activity rendering with seeded task artifacts,
actual sub-agent delegation, parallel execution limits, process/container/
worktree isolation, background execution, cross-platform acceptance, and
packaged release acceptance.

## Slice 115 — add an explicit local agent-task planning journey

Status: `implemented_verified_build`

Activity & Tasks now offers a `Plan agent task` action. The dialog collects a
bounded title, instruction, and role, clearly states that saving is local-only,
and persists a proposed task artifact only after the user chooses `Save task
plan`. It focuses the title field on open, exposes stable accessible labels,
and closes the Activity dialog after a successful save so the user can reopen it
and review the new record. It never starts an agent, calls a model, invokes a
tool, or grants a capability.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (157 contracts).`
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the dialog writes only bounded local task metadata;
it does not accept credentials or file contents and adds no network, provider,
shell, Git, MCP, or background execution path.

Rollback: remove the planning button/dialog and its audit contracts; the
read-only task artifact section and Core store remain available.

Not yet verified: live packaged creation/review with seeded task artifacts,
actual sub-agent delegation, parallel execution limits, process/container/
worktree isolation, background execution, cross-platform acceptance, and
packaged release acceptance.

## Slice 116 — queue proposed agent tasks for a future executor

Status: `implemented_verified_build`

Proposed task artifacts in Activity & Tasks now expose an explicit `Queue for
future executor` action. The action transitions only the selected local record
from `proposed` to `queued`, then closes the review dialog and reports that no
agent or provider was started. It is disabled by absence for already queued or
terminal tasks, preserving the store's lifecycle rules.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (159 contracts).`
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: queueing stores a bounded local status change
only. It does not claim the task, call a provider, invoke a tool, access a
workspace, or create background work.

Rollback: remove the queue button/handler and its audit contracts; proposed
tasks remain viewable and can still be created locally.

Not yet verified: live packaged queue/review with seeded task artifacts, actual
sub-agent delegation, parallel execution limits, process/container/worktree
isolation, background execution, cross-platform acceptance, and packaged
release acceptance.

## Slice 117 — live Windows agent-task planning and queue journey

Status: `verified_windows_accessibility_inspection`

The current `net10.0-desktop` build was launched and Activity & Tasks was opened
through the command palette. The live accessibility tree exposed the Agent task
artifacts section and `Plan agent task` action. A disposable task plan was saved
with title `Disposable live agent task`, verifier role, and a local-only
instruction. Reopening Activity showed the persisted task as `proposed` with
the `Queue agent task for future executor` action. Activating it produced the
visible status `Agent task queued locally for a future executor. No agent or
provider was started.`

Evidence:

- `npm run start:native` — launched the current desktop build.
- Live Windows accessibility inspection — passed for Activity routing, task
  planning dialog fields/disclosure, proposed-task rendering, and explicit
  queue status.
- No model request, tool call, shell command, workspace edit, or background
  task was started during the journey.

Privacy/security/licensing: the journey wrote only the explicitly entered
disposable task metadata to the app's local task store. No credential, file
content, prompt attachment, provider response, or network destination was
introduced.

Rollback: remove the disposable live task from the local app profile if desired;
the code change is already covered by slices 113–116 and the live evidence is
documentation-only.

Not yet verified: packaged-artifact task journey, live task dependency picker,
actual sub-agent delegation, parallel execution limits, process/container/
worktree isolation, background execution, cross-platform acceptance, and
packaged release acceptance.

## Slice 118 — author bounded agent-task prerequisites

Status: `implemented_verified_build`

The local `Plan agent task` dialog now includes an optional prerequisite picker.
It is populated only from task artifacts already in the bounded local store and
saves selected artifact IDs as dependencies. The picker does not dispatch an
agent, claim work, call a provider, invoke a tool, or create background work;
dependency-aware claiming remains an executor concern.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the dependency-picker accessibility
  contract.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj`
  — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: prerequisite selection reads only bounded local
task metadata and writes only selected local task IDs. No task instruction,
file content, credential, provider, or network state leaves the application.

Rollback: remove the dependency picker and pass no `dependsOn` values when
creating plans; the existing local task planning and queue lifecycle remains.

Not yet verified: deterministic live multi-selection of prerequisites,
packaged-artifact acceptance, actual sub-agent delegation, parallel execution limits,
process/container/worktree isolation, background execution, cross-platform
acceptance, and packaged release acceptance.

## Slice 119 — render agent-task prerequisite titles

Status: `implemented_verified_build`

Activity now resolves each persisted prerequisite ID against the bounded local
task artifact set and renders the prerequisite title, falling back to a short
task ID when the referenced artifact is unavailable. The display is capped at
eight dependency labels and remains read-only; it does not claim, execute, or
dispatch tasks.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the dependency-label contract.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj`
  — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: only bounded local task titles and IDs already
stored on this device are rendered. No provider, network, credential, file,
or background execution path is involved.

Rollback: restore the previous count-only dependency label; task persistence
and executor gating are unchanged.

Not yet verified: deterministic live selection/persistence through the missing
Uno App MCP harness, packaged-artifact acceptance, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 120 — preview the next dependency-ready agent task

Status: `implemented_verified_build`

The Activity surface now offers `Preview next ready agent task`. It uses a
read-only dependency-aware store query, chooses the oldest queued task whose
prerequisites are completed, and leaves the task queued. Proposed tasks and
queued tasks with incomplete prerequisites are not reported as ready. The
surface explicitly states that previewing does not claim, start, dispatch, or
call a provider.

Changed files:

- `native/ServerRock.Core/Services/AgentTaskArtifactStore.cs`
- `native/ServerRock.Core.Tests/AgentTaskArtifactTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the readiness-preview contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj`
  — passed, including non-mutating readiness and dependency gating assertions.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: readiness preview reads only bounded local task
metadata. It does not mutate task status, access credentials, send network
requests, invoke tools, or create background work.

Rollback: remove `GetNextReady` and the Activity preview button; queue and
future-executor state remain unchanged.

Not yet verified: deterministic live activation of the preview control through
the unavailable Uno App MCP harness, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 121 — cancel proposed or queued agent-task plans

Status: `implemented_verified_build`

Activity now exposes `Cancel plan` for proposed and queued local task plans.
Cancellation is allowed only before execution, records a bounded local reason,
and prevents the task from appearing in the dependency-ready preview. Running
tasks still require the existing executor-owned completion/failure/cancel
path; this control does not stop processes or dispatch any work.

Changed files:

- `native/ServerRock.Core/Services/AgentTaskArtifactStore.cs`
- `native/ServerRock.Core.Tests/AgentTaskArtifactTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the cancellation contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj`
  — passed, including pre-execution cancellation and readiness exclusion.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: cancellation writes only a bounded local status
and reason. It does not access credentials, files, providers, network routes,
or process controls.

Rollback: remove `CancelPlanned` and the Activity cancel button; proposed and
queued plans remain viewable and queueable.

Not yet verified: deterministic live activation through the unavailable Uno
App MCP harness, packaged-artifact acceptance, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 122 — search bounded agent-task artifacts

Status: `implemented_verified_build`

Activity now offers `Search agent tasks`, a read-only dialog that searches the
bounded local task store by title, instruction, role, or status and renders up
to twenty matching plan summaries. The query is local-only, updates as typed,
and opening the dialog focuses the search field. No task status changes and no
agent, provider, tool, or background work is started.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`
- `docs/CAPABILITY_MATRIX.md`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:native-ui` — passed with the task-search accessibility
  contracts.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f
  net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj`
  — passed, including bounded local task search coverage.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: task search reads only bounded local metadata and
does not transmit query text or task content to any provider or network route.

Rollback: remove the search action/dialog and its audit contracts; the Activity
task cards and local task store remain available.

Not yet verified: deterministic live activation through the unavailable Uno
App MCP harness, packaged-artifact acceptance, actual sub-agent delegation,
parallel execution limits, process/container/worktree isolation, background
execution, cross-platform acceptance, and packaged release acceptance.

## Slice 123 — derive export filenames from product identity

The native chat, Activity evidence, and memory backup save dialogs now derive
their suggested filename stem from `ProductIdentity.ShortName`. This removes
the remaining hardcoded legacy product slug from those export workflows while
keeping the user-facing display identity centralized for the planned rebrand.

Evidence:

- `ProductIdentity.FileNameStem` strips punctuation and spaces and lowercases
  the current short name in one shared implementation.
- Chat archive, Activity evidence, and memory backup pickers all use that stem.
- The native core runner checks the current stem contract.
- No credentials, chat contents, or files are changed by this slice.

Privacy/security/licensing: this only changes local suggested filenames. It
does not alter archive contents, network behavior, or provider routing.

Rollback: remove `FileNameStem` and restore the three picker interpolations;
the export payloads and storage paths remain otherwise unchanged.

Not yet verified: live file-picker acceptance in a packaged Windows run and
Linux picker parity. The repository has no Uno App MCP surface available for
that UI automation path.

## Slice 124 — close the active-response message-queue backlog item

The future-features register now marks message queueing complete. This is
limited to the requirement's actual scope: while a response is active, a
bounded follow-up prompt can be accepted locally, reviewed through the Queue
control, and dispatched after the active request reaches its cleanup path.

Evidence already recorded in Slices 23, 24, 30–33, and 76:

- Core tests cover bounded capacity, attachment preservation, FIFO/reorder,
  remove/clear, persistence, restore, and drain behavior.
- Windows accessibility evidence shows `Stop generating response`, accepts a
  second prompt during the active request, exposes `Manage queued messages`,
  and dispatches the queued follow-up after the first request fails.
- The queue remains local and bounded; no provider request is made until the
  existing send lifecycle reaches its dispatch path.

Privacy/security/licensing: queued prompts and explicitly attached content
remain in the existing local bounded queue and use the existing attachment
consent/provider policy at dispatch. No new network, credential, shell, Git,
or third-party behavior is introduced.

Rollback: revert the checkbox and this evidence entry; the implemented queue
code remains unchanged and can be re-audited if the acceptance scope changes.

Limitations: successful live provider response, cross-device synchronization,
and Linux/package parity remain separate gaps and are not implied by this
backlog closure.

## Slice 125 — inspect live conversation organization controls

The exact Windows desktop build was launched and its navigation/sidebar
conversation surface was inspected without changing saved user data. The live
accessibility tree and rendered dialog exposed:

- folder creation plus chat archive import/export controls;
- per-chat Pin chat, Edit tags, Archive chat, and Fork from here actions;
- the tag editor's bounded comma-separated input, local-only disclosure, Save
  tags, and Cancel controls.

The action menu was opened and the tag editor was opened and cancelled. No
folder was created, no tag was saved, and no chat was pinned, archived, forked,
deleted, or exported. Companion visibility was briefly toggled by an imprecise
coordinate during navigation and restored to its prior hidden state; no pet
code or persisted pet setting was changed by the repository work.

Evidence:

- Live Windows accessibility state showed `Create conversation folder`,
  `Import saved chats`, `Export saved chats`, `Pin chat`, `Edit tags…`,
  `Archive chat`, and `Fork from here`.
- Live tag dialog showed `Chat tags` with the help text “Enter up to eight
  comma-separated tags. Each tag can contain up to 32 characters.” and the
  local-only disclosure.
- Existing Core persistence tests cover folder assignment, pins, tags,
  archived/fork metadata, and archive round trips.

Privacy/security/licensing: inspection was read-only for user conversation
data and made no provider, network, credential, shell, Git, or third-party
calls. The live check did not transmit or export content.

Rollback: documentation-only; no runtime or persisted data changes require
rollback.

Not yet verified: a complete disposable-data mutation/save/reload journey for
folder, pin, tags, archive, and fork actions, plus packaged/Linux accessibility
parity. The future-features item `Folders, tags, and pinned chats` therefore
remains unchecked.

## Slice 111 — make Activity search keyboard-ready

Status: `implemented_verified_build`

The Activity search dialog now has an explicit accessible name, its query field
has a stable accessible label, and opening the dialog focuses the query field.
Search remains read-only and refreshes bounded local operation and workflow
event results as the query changes.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `npm run audit:native-ui` — passed: `Native UI audit passed (150 contracts).`
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: Activity search reads only bounded local evidence
already present in the journal. Query text is not sent to a provider and no
task, file, credential, or network state is changed.

Rollback: remove the query/dialog labels and focus handler plus their audit
contracts; the existing read-only search behavior remains.

Not yet verified: live keyboard activation of every Activity action, seeded
live workflow-event search matching, full status-history keyboard/screen-reader
acceptance, OS session hosting, parallel-agent artifacts, Linux parity, and
packaged acceptance.

## Slice 96 — extract the main page header into a dedicated visual control

Status: `implemented_verified_build`

The giant page visual tree now delegates its header to `MainPageHeader`, a
dedicated UserControl. The extracted control owns navigation buttons, route
badges, branch controls, connection status, and the new-chat action. It
forwards semantic events to `MainPage` and exposes only the named controls
that existing feature partials need, preserving behavior while making the
layout independently maintainable. The pet-owned companion overlay was not
changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/MainPageHeader.xaml`
- `native/ServerRock.Native/MainPageHeader.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.
- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`

Privacy/security/licensing: this is a local visual decomposition only. It
changes no persistence, provider route, credential handling, prompt content,
or third-party dependency.

Rollback: restore the header Grid in `MainPage.xaml`, remove
`MainPageHeader.xaml` and `.xaml.cs`, remove the header event subscriptions and
compatibility accessors, and keep the lifecycle split intact.

Not yet verified: live packaged-release interaction after the visual split,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 97 — continue the main page visual split with the Coding Agent bar

Status: `implemented_verified_build`

Continuing the Slice 95/96 UI decomposition, the Coding Agent workspace bar is
now a dedicated `CodingWorkspaceBar` UserControl. It owns the project summary,
folder count, project/GitHub actions, terminal action, undo-history action, and
responsive toolbar visual tree. MainPage keeps the existing feature methods
through forwarded events and compatibility accessors, so project behavior and
responsive layout logic remain unchanged. The pet-owned companion overlay was
not changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml`
- `native/ServerRock.Native/CodingWorkspaceBar.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no project persistence, GitHub credential handling, command execution,
provider route, or third-party dependency.

Rollback: restore the Coding Agent workspace-bar Grid in `MainPage.xaml`,
remove `CodingWorkspaceBar.xaml` and `.xaml.cs`, and remove its lifecycle
subscriptions and accessors.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 101 — live Activity & Tasks acceptance evidence

Status: `verified_windows_accessibility_inspection`

The current Windows desktop build was launched from the synchronized checkout
and the existing Activity & Tasks journey was inspected without creating,
editing, cancelling, or deleting user data. The navigation surface exposed
`Open activity and tasks`; the dialog exposed `Activity & tasks`, `Export
activity evidence`, `Search activity evidence`, and `Schedule task`. It also
rendered completed and failed provider-attempt records with model/provider,
local-route, run identity, and sequence metadata, plus an interrupted
operation with separate accessible `Mark interrupted operation cancelled` and
`Mark interrupted operation failed` decisions. This closes the prior gap for
live rendering of provider-attempt events and reconciliation choices; it does
not claim that every action was activated.

Evidence:

- Windows native app launched from `scripts/start-native.cjs` and remained live.
- Computer-use accessibility tree exposed the Activity & Tasks popup and its
  accessible action labels and result rows.
- The rendered surface showed completed/failed entries and an interrupted run
  with both bounded decision buttons.

Privacy/security/licensing: inspection read only existing local activity
records. No provider request, export, schedule creation, credential use,
network transmission, or third-party dependency was introduced.

Rollback: documentation-only; remove this evidence entry and the corresponding
capability-matrix wording if the live surface is later invalidated.

Not yet verified: keyboard activation of every Activity action, seeded live
workflow-event matching, full status-history screen-reader acceptance, OS
session hosting, parallel-agent artifacts, Linux parity, and packaged
acceptance.

## Slice 102 — verify scheduled work surfaces for review without dispatch

Status: `verified_windows_accessibility_inspection`

The live Windows Activity & Tasks surface contained the completed scheduler
record `Review scheduler acceptance` with the explicit outcome `Surfaced in the
composer for review; no provider request was sent.` This verifies the intended
safe boundary between due scheduling and model dispatch: a due task can become
reviewable work without silently sending a provider request. The evidence came
from the existing local activity record; no new task was created and no task
was edited or deleted.

Evidence:

- The Activity & Tasks accessibility tree exposed the completed scheduler row,
  its one-time workspace metadata, and the no-dispatch outcome text.
- The same surface exposed the separate `Schedule task` action for future
  work, confirming scheduling remains an explicit UI path.

Privacy/security/licensing: this was read-only inspection of local scheduler
and activity data. No prompt was sent, no provider or cloud route was used,
and no dependency or third-party code was added.

Rollback: documentation-only; remove this evidence entry if a later live run
contradicts the recorded scheduler outcome.

Not yet verified: recurring-task creation and pause/resume in the current
build, reviewed workflow execution adapters, OS session hosting, automatic
result capture, timezone/backoff policy, parallel agents, Linux parity, and
packaged acceptance.

## Slice 103 — fresh Windows target build evidence

Status: `verified_windows_build`

After stopping only the disposable native development process launched for
live inspection, the synchronized checkout built the Windows App SDK target
successfully. This confirms that the recent visual-control decomposition and
the current handoff state compile for both the shared Skia desktop target and
the Windows target; it does not claim packaged installer or clean-machine
acceptance.

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — previously passed for the current slice.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-windows10.0.26100 --no-restore` — passed with 0 warnings and 0 errors; produced `bin/Debug/net10.0-windows10.0.26100/win-x64/ServerRock.Native.dll`.

Privacy/security/licensing: compilation only; no application data, provider,
credential, network, installer, or third-party dependency was accessed or
changed.

Rollback: documentation-only; remove this evidence entry if the build is not
reproducible from the recorded commit.

Not yet verified: signed installers, clean-machine install/upgrade/rollback/
uninstall, Linux artifact/build/run evidence from this commit, packaged UI
interaction, and release-feed publication.

## Slice 104 — current Linux validation boundary recorded

Status: `blocked_by_host_toolchain`

The current checkout was checked against the installed Ubuntu 24.04 WSL
environment. WSL launched the distro, but the validation command stopped before
the repository build because `dotnet` is not installed in that distro:

```text
wsl.exe -d Ubuntu-24.04 -- bash -lc "cd '/mnt/e/Server Rock Ai' && dotnet --version ..."
bash: line 1: dotnet: command not found
```

This is a host-toolchain blocker for fresh Linux evidence, not a code failure.
The Windows desktop and Windows App SDK builds remain independently verified.
The matrix continues to mark Linux parity `unverified` until a supported .NET
10 SDK is available in WSL or a Linux CI runner can execute the build, Core
checks, display smoke, and packaging checks.

Privacy/security/licensing: the attempt only read the repository and queried
the local WSL toolchain. No application data, provider, credentials, network
route, or dependency was changed.

Rollback: documentation-only; remove this boundary entry if a later Linux
environment produces current-checkout evidence.

Not yet verified: Linux build/run, accessibility, packaging, model-server
connectivity, writable profile paths, and exact supported distro scope.

## Slice 105 — repository hygiene, release configuration, and rebrand gate

Status: `verified_windows_repository_gates`

The current synchronized repository passed the release/security hygiene gates.
The runtime display identity search also confirmed that the product name is
centralized in `native/ServerRock.Core/Models/ProductIdentity.cs` rather than
duplicated through feature UI code, preserving the ability to rebrand the app
without editing every surface. Release packaging metadata remains an explicit
manifest/configuration input, not a hidden runtime fallback.

Evidence:

- `npm run check:hygiene` — passed: `Repository hygiene check passed (325 tracked files).`
- `npm run check:release-config` — passed: `Release configuration is internally consistent.`
- `npm run check:syntax` — passed.
- Source search across `native`, `scripts`, and `packaging` found runtime
  display identity in `ProductIdentity` and build/package identity inputs, with
  no scattered feature-level hardcoded display-name literals.

Privacy/security/licensing: the hygiene gate rejects tracked secrets and
generated artifacts; the release validator does not access credentials unless
signing mode is explicitly enabled. The identity check changes no data flow,
provider routing, persistence, or dependency set.

Rollback: documentation-only; remove this evidence entry if the gate outputs or
identity ownership changes.

Not yet verified: signed release publication, clean-machine install/upgrade/
rollback/uninstall, OS isolation, keyring parity, full negative security suite,
and Linux parity.

## Slice 106 — future-features backlog baseline verified

Status: `verified_repository_backlog`

The requested future-features list is present and tracked at
`docs/FUTURE_FEATURES.md`. It currently contains 159 unchecked feature items
across Core chat, Agents and tools, Retrieval and knowledge, Workspace and
desktop experience, Models/runtime/extensibility, Administration and teams,
Files/environments/delivery, Content/meetings, and Ecosystem/platform. The
document explicitly requires each item to be implemented as a bounded vertical
slice with privacy, platform, licensing, failure-state, and acceptance
evidence; it also preserves the AirLLM link as a research reference rather
than an automatic dependency or implementation instruction.

Evidence:

- Repository inspection: `docs/FUTURE_FEATURES.md` exists on synchronized
  `main` and contains 159 unchecked `- [ ]` feature entries.
- The backlog includes the requested future areas such as multi-model
  selection/comparison, RAG, computer use, MCP, workflows, persistent tabs,
  memory, integrations, meetings, release/runtime options, and platform
  differentiators.

Privacy/security/licensing: this is a local documentation baseline. No feature
was silently enabled, no external project was copied, and no dependency,
credential, provider route, or user data was changed.

Rollback: documentation-only; remove this evidence entry if the backlog source
is replaced by a newer approved product register.

Not yet verified: each future item remains independently tracked as incomplete
until its implementation and acceptance evidence are recorded in a later
slice.

## Slice 107 — future-features backlog integrity audit

Status: `implemented_verified_repository_tooling`

Added a deterministic repository audit for `docs/FUTURE_FEATURES.md`. It
requires the core backlog sections, validates checkbox syntax, rejects empty
or duplicate feature labels, and reports the item/section/checked counts. The
audit is available as `npm run audit:future-features` and its script is included
in `npm run check:syntax`, so future backlog edits fail visibly before they
become the planning source for another implementation slice.

Changed files:

- `scripts/audit-future-features.cjs`
- `package.json`
- `docs/GOAL_EXECUTION.md`

Evidence:

- `npm run audit:future-features` — passed: `159 items, 11 sections, 0 checked`.
- `npm run check:syntax` — passed, including the new audit script.
- `npm run check:hygiene` — passed: `325 tracked files`.
- `git diff --check` — passed.

Privacy/security/licensing: this is local documentation validation only. It
does not execute feature content, access user data, add dependencies, or alter
runtime/provider behavior.

Rollback: remove `scripts/audit-future-features.cjs`, its package scripts, and
this evidence entry; the future-features document remains readable manually.

Not yet verified: implementation and acceptance of the individual future
features, which remain deliberately unchecked.

## Slice 100 — extract the composer input surface from MainPage.xaml

Status: `implemented_verified_build`

The rest of the composer visual tree is now a dedicated `ComposerInputView`
UserControl. It owns the attachment/status area, queue and status-history
actions, slash-command hints, prompt editor, voice controls, stop/send actions,
and privacy footnote. It hosts the already extracted `ComposerToolbarView` and
forwards existing events and control accessors, preserving chat, queue,
attachments, commands, voice, and focus behavior. The pet-owned companion
overlay was not changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/ComposerInputView.xaml`
- `native/ServerRock.Native/ComposerInputView.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no attachment policy, prompt content, provider request, voice data
handling, persistence, credential handling, or third-party dependency.

Rollback: restore the composer StackPanel and children in `MainPage.xaml`,
remove `ComposerInputView.xaml` and `.xaml.cs`, and remove its compatibility
accessors. The separate toolbar extraction can remain or be reverted
independently.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 99 — extract the composer toolbar from MainPage.xaml

Status: `implemented_verified_build`

The composer toolbar is now a dedicated `ComposerToolbarView` UserControl. It
owns the model picker, tools disclosure, optional web/temporary/project-file
toggles, message-edit cancellation, smart rewrite, image studio, and advanced
controls. It forwards the existing feature actions and responsive size-change
hook to MainPage, preserving the current compact/expanded layout behavior.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/ComposerToolbarView.xaml`
- `native/ServerRock.Native/ComposerToolbarView.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no model routing, web-search behavior, local-file consent, persistence,
credential handling, or third-party dependency.

Rollback: restore the composer-toolbar Grid in `MainPage.xaml`, remove
`ComposerToolbarView.xaml` and `.xaml.cs`, and remove its lifecycle
subscriptions and compatibility accessors.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 98 — extract the conversation surface from MainPage.xaml

Status: `implemented_verified_build`

The remaining conversation viewport above the composer is now a dedicated
`ConversationSurface` UserControl. It owns the welcome state, connection setup
action, prompt suggestions, and scrollable message host. MainPage retains the
existing chat/message behavior through forwarded setup and suggestion events
and bounded compatibility accessors for the scroll and message children. The
composer and pet-owned companion overlay were not changed.

Changed files:

- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Lifecycle.cs`
- `native/ServerRock.Native/ConversationSurface.xaml`
- `native/ServerRock.Native/ConversationSurface.xaml.cs`
- `docs/ARCHITECTURE.md`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts).`
- `npm run check:syntax` — passed.

Privacy/security/licensing: this is a local visual decomposition only. It
changes no conversation persistence, provider request, credential handling,
or third-party dependency.

Rollback: restore the welcome/conversation Grid and its children in
`MainPage.xaml`, remove `ConversationSurface.xaml` and `.xaml.cs`, and remove
the lifecycle subscriptions and accessors.

Not yet verified: live packaged-release interaction after this extraction,
narrow-window visual QA, Linux parity, and the remaining handoff slices.

## Slice 81 — deterministic date/time variables in reviewed skill prompts

Status: `implemented_verified_core`

Built-in and installed Markdown skill instructions now expand the bounded,
deterministic variables `{{DATE}}`, `{{TIME}}`, and `{{DATETIME}}` before the
prompt is placed in the composer for user review. Unknown variables remain
unchanged, and the prompt is still visible and editable before it can be sent.
Expansion is limited to local date/time values; it does not read account data,
environment variables, files, or network state.

Changed files:

- `native/ServerRock.Core/Services/PromptVariableExpander.cs`
- `native/ServerRock.Core.Tests/PromptVariableTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including deterministic expansion, case-insensitive names, unknown-variable preservation, and control-character rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (131 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: only the local clock is consulted. No username,
credentials, files, provider metadata, network data, dependency, or
third-party code is introduced. The existing explicit skill enablement and
user-review-before-send boundary remains in place.

Rollback: remove `PromptVariableExpander`, its Core test and runner entry, the
two skill-loading expansion calls, and the UI audit contract. Skill loading
then returns the original Markdown instructions unchanged.

Not yet verified: live Windows skill insertion with a variable-bearing
installed package, user-configurable variables such as `{{USER_NAME}}`, and
cross-platform/package acceptance.

## Slice 82 — explicit opt-in user-name prompt variable

Status: `implemented_verified_core`

The reviewed skill prompt path now supports `{{USER_NAME}}` only when the user
has entered an explicit local `Prompt variable name (optional)` setting. An
unset value leaves the token visible for review, and the app never reads the
Windows account name automatically. The setting is normalized through the
existing version-tolerant local settings store and is only substituted into a
prompt the user can inspect and edit before sending.

Changed files:

- `native/ServerRock.Core/Models/AppSettings.cs`
- `native/ServerRock.Core/Infrastructure/JsonSettingsStore.cs`
- `native/ServerRock.Core/Services/PromptVariableExpander.cs`
- `native/ServerRock.Core.Tests/PromptVariableTests.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including explicit-name substitution, unset-token preservation, and unsafe-value rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (133 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the value is user-entered, bounded, stored in
local non-secret settings, and never inferred from the operating system. No
new network request, credential, dependency, or third-party code is involved.

Rollback: remove `PromptUserName`, its settings normalization and UI field, and
the optional expander argument; existing date/time variables and reviewed
skill loading can remain, or the full variable expander can be removed with
Slice 81's rollback.

Not yet verified: live Windows persistence and insertion of a variable-bearing
installed package, cross-platform/package acceptance, and additional
user-configurable variables beyond `{{USER_NAME}}`.

## Slice 83 — durable provider-attempt provenance

Status: `implemented_verified_core`

Chat operation runs now append bounded provider-attempt evidence for started,
completed, failed, and user-cancelled requests. Each event records the
provider, model, privacy classification, outcome, and an optional bounded
reason, while intentionally excluding endpoint URLs, API keys, prompts, and
response content. This makes the selected route and failure outcome auditable
without changing provider routing or adding automatic fallback behavior.

Changed files:

- `native/ServerRock.Core/Infrastructure/OperationJournal.cs`
- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Core.Tests/OperationServiceTests.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including durable started/failed provider-attempt events and bounded provenance fields.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (134 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: event payloads are local-only, bounded, and
redacted by design; no endpoint URL, credential, prompt, response, dependency,
or third-party code is recorded. The existing explicit model-selection and
no-automatic-fallback boundary remains unchanged.

Rollback: remove `OperationJournal.AppendEvent`,
`OperationService.RecordProviderAttempt`, the Chat lifecycle calls, its test,
and the UI audit contract. Existing status-transition journal evidence remains
available.

Not yet verified: live Windows Activity rendering of provider-attempt events,
workflow/provider attempts outside chat, and cross-platform/package acceptance.

## Slice 84 — bounded OpenAPI 3 operation discovery

Status: `implemented_verified_core`

Core now parses OpenAPI 3.x JSON documents into bounded, reviewable operation
descriptors. It discovers path/method names, summaries, parameter schemas, and
JSON request-body schemas; classifies safe read methods versus mutating
methods; and attaches the corresponding MCP read/write capability requirement.
The slice is discovery-only: it does not fetch a document, resolve `$ref`
values, create credentials, execute an operation, or grant permissions.

Changed files:

- `native/ServerRock.Core/Models/OpenApiDiscoveredOperation.cs`
- `native/ServerRock.Core/Services/OpenApiToolDiscovery.cs`
- `native/ServerRock.Core.Tests/OpenApiToolDiscoveryTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including read/write classification, bounded schema preservation, unsupported-version rejection, and credential-bearing source-identity rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (134 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the parser consumes caller-provided JSON only and
performs no network or code execution. Discovered metadata is untrusted and
does not authorize MCP calls; future execution must reuse explicit grants and
the existing redirect, credential, capability, and payload boundaries. No
dependency or third-party code was added.

Rollback: remove the operation model, parser, tests, and runner entry. Existing
MCP discovery and permission gates remain unchanged.

Not yet verified: UI presentation, authenticated document retrieval, `$ref`
resolution, explicit per-operation grants, execution adapters, live
integration acceptance, Linux parity, and packaged acceptance.

## Slice 85 — visible local OpenAPI review journey

Status: `implemented_verified_windows_with_execution_boundary`

The Plugins surface now exposes a reachable `Review local OpenAPI schema`
action. It opens a local JSON-only file picker, reads the selected document
through the bounded UTF-8 reader, parses it with the Core discovery service,
and presents the resulting operations in a dedicated review dialog with method,
path, capability, mutating status, and input-schema details. The review dialog
does not fetch a server, execute an operation, create credentials, or grant
MCP permission.

Changed files:

- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after the dev process was closed.
- `npm run audit:native-ui` — passed: `Native UI audit passed (137 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility/surface inspection — passed: the Plugins dialog visibly exposed `Review local OpenAPI schema`; activating it opened the native picker filtered to `JSON files`. The picker was cancelled without selecting a file, and the dev app was closed afterward. No user data, settings, pet state, credential, or network route changed.

Privacy/security/licensing: only a user-selected local JSON file is read,
within the existing 1,000,000-character parser bound and UTF-8 validation. The
schema remains untrusted; no operation can run from this surface. No
dependency, network request, credential, or third-party code was added.

Rollback: remove the OpenAPI review button, picker/review dialog, and UI audit
contracts. Core schema discovery remains available for a future integration
surface.

Not yet verified: selecting and rendering a real user-provided OpenAPI file,
authenticated document retrieval, explicit per-operation
grants, execution adapters, live integration acceptance, Linux parity, and
packaged acceptance.

## Slice 86 — bounded local OpenAPI component references

Status: `implemented_verified_core`

Core OpenAPI discovery now resolves bounded local component schema references
from `#/components/schemas/...` while building reviewable input schemas. It
rejects external, malformed, missing, or excessively nested references before
returning a descriptor; it does not retrieve remote schemas or execute an
operation.

Changed files:

- `native/ServerRock.Core/Services/OpenApiToolDiscovery.cs`
- `native/ServerRock.Core.Tests/OpenApiToolDiscoveryTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed: `Native core checks passed.`
- `git diff --check` — passed.

Privacy/security/licensing: resolution is limited to the in-document
`components.schemas` object, with a maximum reference depth of 8 and bounded
component names. External references fail closed, so this slice adds no
network access, credentials, dependency, or implicit authorization.

Rollback: remove the local reference resolver and its focused tests; direct
inline schemas remain supported.

Not yet verified: successful selection/rendering of a real user-provided file,
authenticated retrieval, explicit per-operation grants, execution adapters,
live integration acceptance, Linux parity, and packaged acceptance.

## Slice 87 — manifest-driven release launchers

Status: `implemented_verified_script_level`

The native packaging layer now reads display and launcher metadata from
`release-manifest.json`. Windows Start Menu shortcuts and optional launches
target the executable inside the versioned `current` directory instead of a
copied top-level EXE shim, preserving adjacent runtime files. Rollback no
longer recreates a stale shim, and Linux uses the same manifest metadata for
its install folder, command launcher, and desktop entry. The Windows setup
default now resolves the archive root from the packaging script directory,
matching the native artifact workflow.

Changed files:

- `packaging/native/release-manifest.json`
- `packaging/native/install.ps1`
- `packaging/native/setup.ps1`
- `packaging/native/rollback.ps1`
- `packaging/native/uninstall.ps1`
- `packaging/native/setup.sh`
- `packaging/native/README.md`

Evidence:

- PowerShell parser validation — passed for all four `.ps1` scripts.
- `npm run audit:native-ui` — passed: `Native UI audit passed (137 contracts).`
- `git diff --check` — passed.
- Linux shell syntax validation was not available on this Windows host because
  WSL could not start; no Linux acceptance claim is made.

Privacy/security/licensing: the change only affects installation paths,
launcher targets, and display metadata. The existing user-data location is
preserved, package contents are still copied into a versioned directory, and
no credentials, network route, dependency, or third-party code was added.

Rollback: restore the prior packaging scripts and release manifest. Existing
explicit `-InstallRoot` overrides remain supported for recovery.

Not yet verified: clean-machine Windows install/upgrade/rollback/uninstall,
unsigned-package rejection, Linux packaging, signed artifacts, and packaged
release acceptance.

## Slice 88 — bounded actionable status history

Status: `implemented_verified_core_static_ui_and_windows_surface`

The runtime status banner now keeps a bounded in-memory history of actionable
messages. Dismissing a banner no longer loses the failure context: the banner
exposes an accessible `Status history` action that shows the newest messages
first in a scrollable review dialog. Informational messages continue to use
the existing short-lived banner behavior, and consecutive duplicate failures
are coalesced.

Changed files:

- `native/ServerRock.Core/Services/StatusMessageHistory.cs`
- `native/ServerRock.Core.Tests/StatusMessageHistoryTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including bounded ordering, duplicate suppression, blank-message handling, and oversized-message truncation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (140 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility/screenshot inspection — passed at the surface
  boundary: after an actionable `Unknown command. Type / to see available
  commands.` status, the app exposed `View status history` with its help text
  and retained the status banner. Dialog activation was not claimed because
  this run's native geometry bridge did not provide a reliable click target.

Privacy/security/licensing: history is process-local and bounded to 50
messages of at most 2,000 characters each. It is not persisted, sent to a
provider, or written into conversation history; no dependency or third-party
code was added.

Rollback: remove `StatusMessageHistory`, the history button/dialog, and its
audit contracts; the existing banner and dismissal behavior remain available.

Not yet verified: live Windows keyboard/screen-reader activation of the new
history dialog, cross-platform accessibility behavior, persistent export, and
full NotificationCenter severity/action semantics.

## Slice 89 — explicit bounded memory injection

Status: `implemented_verified_core_and_native_build`

The existing local memory editor now has a separate global consent switch for
including enabled memory in future model requests. The switch is off by
default and is saved only when the user chooses Done in the Memory dialog.
When enabled, prompt assembly includes only enabled `global` notes and notes
scoped to the active project, labels them as untrusted reference material, and
caps the complete context section at 6,000 characters. Disabled, unrelated,
or unavailable memory is not injected; malformed memory fails closed for
context while the prompt continues without it and exposes a status message.

Changed files:

- `native/ServerRock.Core/Models/AppSettings.cs`
- `native/ServerRock.Core/Services/MemoryPromptContext.cs`
- `native/ServerRock.Core.Tests/MemoryPromptContextTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including explicit opt-in, scope filtering, disabled-entry filtering, reference labeling, and the 6,000-character bound.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (141 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: injection is disabled by default and is not
learned from chats. Enabling it can send approved local notes to the selected
endpoint, including a remote endpoint; the Memory dialog discloses this before
the switch is used. Context is bounded and marked as untrusted reference
material. No dependency or third-party code was added.

Rollback: remove the global setting, `MemoryPromptContext`, prompt assembly
call, and consent control; local memory editing and backup remain available.

Not yet verified: live Windows toggle/save/send acceptance, remote-endpoint
consent wording during an actual injected request, cross-platform UI behavior,
semantic retrieval, and larger-context support.

## Slice 90 — reviewed scheduled-task run-now control

Status: `implemented_verified_core_and_native_build`

Activity & tasks now exposes a Run now action for scheduled tasks. The action
only moves the durable task's due time to the current instant, claims it through
the existing scheduler boundary, and surfaces the prompt in the composer for
human review. It does not send a provider request, bypass model availability,
remote-data consent, or the normal Send control. Paused, completed, failed, and
already-running tasks remain ineligible for this action.

Changed files:

- `native/ServerRock.Core/Services/ScheduledTaskStore.cs`
- `native/ServerRock.Core.Tests/ScheduledTaskTests.cs`
- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the durable run-now and claim path.
- native build — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (142 contracts)`, including the accessible run-now control.
- `git diff --check` — passed after the final patch.

Privacy/security/licensing: the action is local-only state manipulation and
does not dispatch network, model, shell, file, or Git work. The existing review
and send boundaries remain in force. No dependency or third-party code was
added.

Rollback: remove `ScheduledTaskStore.RunNow`, the activity control, and its
tests/audit contract; scheduled creation, pause/resume, and automatic due
processing remain available.

Not yet verified: live Windows activation of the new control, cross-platform UI
behavior, OS-level background execution, and durable task synchronization across
multiple app instances.

## Slice 91 — bounded workspace navigation history

Status: `implemented_verified_core_and_native_build`

The native shell now has a bounded, branch-aware navigation history for the Chat
and Coding Agent workspace routes. Back and Forward controls are visible in the
header, expose accessible names/help text, disable when no route is available,
and restore the existing workspace switching path so temporary-chat safeguards,
draft isolation, and project context rules still apply. A new route after going
back truncates forward history; history is capped at 32 entries and is session
state only, so it does not alter conversations or user files.

Changed files:

- `native/ServerRock.Core/Services/NavigationHistory.cs`
- `native/ServerRock.Core.Tests/NavigationHistoryTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.xaml`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including bounded history, back/forward movement, and forward-branch truncation.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed after adding the Back/Forward contracts.
- `git diff --check` — passed.

Privacy/security/licensing: route history contains only bounded local route
identifiers; it does not persist prompts, model data, credentials, or files. No
dependency or third-party code was added.

Rollback: remove `NavigationHistory`, the header controls, route hooks, tests,
and audit contracts; existing direct workspace switching remains available.

Not yet verified: live Windows activation of both buttons, persistent tabs or
layout state, dialog/overlay route history, Linux/Skia interaction, and narrow
viewport focus acceptance.

## Slice 92 — durable workspace-route history

Status: `implemented_verified_core_and_native_build`

The bounded Chat/Coding Agent route history from Slice 91 now survives settings
save/reload. Restore is defensive: invalid or control-character route values
are skipped, the list remains bounded to 32 entries, and an empty/corrupt list
falls back to the current Chat route. Back/Forward still operate only on the
existing workspace-switch path, so temporary-chat confirmation, draft isolation,
and project context safeguards remain active. No prompt, conversation, file,
credential, endpoint, or model data is stored in this route history.

Changed files:

- `native/ServerRock.Core/Services/NavigationHistory.cs`
- `native/ServerRock.Core/Models/AppSettings.cs`
- `native/ServerRock.Core.Tests/NavigationHistoryTests.cs`
- `native/ServerRock.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including route restoration, invalid-route recovery, settings round-trip, and bounded history.
- native build — passed with 0 warnings/errors.
- UI audit — passed with the existing 144 native contracts.
- `git diff --check` — passed.

Privacy/security/licensing: only bounded route identifiers are persisted in the
existing local settings file. Invalid persisted values fail closed. No
dependency or third-party code was added.

Rollback: remove `WorkspaceRouteHistory` persistence and restore calls; Slice 91
can continue using session-only navigation history.

Not yet verified: persistent tabs/layout geometry, dialog/overlay routes, live
restart Back/Forward activation, Linux/Skia interaction, and narrow viewport
focus acceptance.

## Slice 93 — bounded side-by-side pinned-model comparison

Status: `implemented_verified_core_and_native_build`

The model quick-switch menu now offers a comparison journey when at least two
pinned compatible models exist. The user selects two to four models and enters
one plain-text prompt. The policy deduplicates selections, bounds the prompt,
honors local-only endpoint policy, and rejects invalid model connections before
dispatch. Comparison requests exclude attachments, saved memory, project files,
and web results. Requests run in parallel, remote endpoints require explicit
consent, and each model renders its own response or classified failure so one
failure does not erase partial results. API keys remain session-only.

Changed files:

- `native/ServerRock.Core/Services/ModelComparisonPolicy.cs`
- `native/ServerRock.Core.Tests/ModelComparisonPolicyTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`
- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including deduplication, minimum selection, local-only routing, and prompt bounds.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts)`.
- `git diff --check` — passed.

Privacy/security/licensing: comparison is opt-in, plain-text only, and keeps
remote routing visible. No cloud request is made silently; no credentials are
written to settings. Output is bounded before rendering. No dependency or
third-party code was added.

Rollback: remove the comparison policy, menu action, consent set, result dialog,
tests, and audit contracts; existing model switching remains unchanged.

Not yet verified: live Windows comparison with two healthy pinned models, live
remote-consent acceptance, provider-specific model capabilities, durable result
history, and Linux/packaged accessibility behavior.

## Slice 94 — exact Windows Release artifact smoke correction

Status: `implemented_verified_windows_artifact_smoke`

The exact current Release `win-x64` self-contained artifact was published and
smoke-tested. The artifact contains 496 files and 320,042,070 bytes. The bounded
startup smoke passed for eight seconds, and the strict startup-plus-top-level-
window smoke passed for eight seconds. During verification, the window check
initially failed because the smoke harness always launched the child with
`windowsHide: true`; the harness now keeps the child visible when
`NATIVE_SMOKE_REQUIRE_WINDOW=1`, then the strict check passed against the same
published executable.

Changed files:

- `scripts/smoke-native-artifact.cjs`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- `dotnet publish native/ServerRock.Native/ServerRock.Native.csproj -c Release -f net10.0-desktop -r win-x64 -p:SelfContained=true --output <temporary artifact>` — passed; 496 files, 320,042,070 bytes.
- `npm run smoke:native:artifact -- <exact ServerRock.Native.exe>` with `NATIVE_SMOKE_SECONDS=8` — passed.
- The same command with `NATIVE_SMOKE_REQUIRE_WINDOW=1` — passed after the harness fix.
- `node --check scripts/smoke-native-artifact.cjs` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: the smoke ran the published app with a disposable
temporary output directory and did not claim installer, signing, cloud, or
Linux acceptance. No user profile data was targeted and no dependency was added.

Rollback: revert the `windowsHide` conditional and the evidence-only document
updates; the ordinary startup smoke remains available.

Not yet verified: signed release publication, clean-machine install/upgrade/
uninstall/rollback, exact Linux artifact interaction, and full accessibility or
workflow acceptance from the packaged artifact.

## Slice 95 — disposable installer journey and MainPage infrastructure split

Status: `implemented_verified_windows_scripts_and_native_build`

The versioned Windows packaging scripts were exercised against a disposable
Release artifact and disposable application-data roots. `install.ps1 -WhatIf`
showed the expected target without mutation; version `1.0.0` installed, version
`1.0.1` upgraded it into `current` while retaining `previous`, rollback restored
`1.0.0`, and uninstall removed only the install root. A profile file under the
disposable `%LOCALAPPDATA%\\ServerRock\\AI` retained an identical hash after
upgrade, rollback, and uninstall. This does not claim clean-machine or signed
installer acceptance.

As part of the same release-readiness slice, the cross-cutting status history,
guarded UI action wrapper, and settings persistence were moved from the large
`MainPage.xaml.cs` state/lifecycle file into `MainPage.Infrastructure.cs`.
This is a partial-class decomposition only: event names, persistence behavior,
status semantics, and feature APIs are unchanged. The pets partial remains
untouched.

Changed files:

- `packaging/native/install.ps1` — exercised, no source change
- `packaging/native/rollback.ps1` — exercised, no source change
- `packaging/native/uninstall.ps1` — exercised, no source change
- `native/ServerRock.Native/MainPage.Infrastructure.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `docs/ARCHITECTURE.md`
- `docs/NATIVE_RELEASE_ACCEPTANCE.md`
- `docs/CAPABILITY_MATRIX.md`

Evidence:

- Exact Release artifact: 496 files, 320,042,070 bytes; startup and strict
  startup/window smoke passed for 8 seconds.
- Disposable installer journey: WhatIf preview, install, versioned upgrade,
  rollback, profile hash retention, and uninstall all passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings/errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (146 contracts)`.
- `npm run check:syntax` — passed.
- `git diff --check` — passed.

Privacy/security/licensing: all installer paths were disposable and did not
target the real profile. Uninstall was run without `-RemoveUserData`; explicit
data deletion remains confirmation-gated. The MainPage split adds no dependency
or runtime behavior and does not move secrets or user data.

Rollback: restore the moved methods to `MainPage.xaml.cs` and remove
`MainPage.Infrastructure.cs`; packaging scripts remain unchanged. The temporary
test roots were separate from the repository and user profile.

Not yet verified: clean-machine installer behavior, shortcut launch acceptance,
signing, interrupted replacement recovery, exact Linux packaging, and live
keyboard/screen-reader interaction for the decomposed page.

## Slice 76 — live queued-follow-up journey

Status: `implemented_verified_windows_failure_path`

The existing native queue was exercised end to end on Windows without changing
the queue implementation. A second prompt was entered while a local response
was active; the composer accepted it, the queue manager became accessible, and
the bounded queue status reported one waiting item. The first local provider
request then failed, after which the queued follow-up appeared in the
conversation and the automatic dispatch path produced its own provider failure.
This verifies ordering and failure-path dispatch, not successful model output.

Evidence:

- Live Windows accessibility inspection exposed `Stop generating response`
  while the first request was active.
- A second prompt was accepted while the first request was active; the tree
  exposed `Manage queued messages` with the review/remove/clear help text and
  `Queued message 1 of 8 · it will send when the current response finishes.`
- After the first local request failed, the tree exposed the queued follow-up
  text followed by a second `Connection error`; the queue control was gone,
  showing the queued item had been removed before dispatch.
- No remote endpoint, credential, file attachment, shell, Git action, or pet
  functionality was used or changed.

Privacy/security/licensing: test prompts stayed in the local conversation and
queue store. The journey used the already configured local endpoint and did not
introduce network routing or third-party code.

Rollback: no production code change was made in this evidence slice; retain
the existing bounded queue behavior and its failure handling.

Not yet verified: successful live provider response, live queue reorder/remove/
clear button activation, cross-device synchronization, durable context
continuity, packaged Linux parity, and approval-inbox live acceptance. The
next dependency remains a deterministic local test endpoint or healthy local
model for successful dispatch evidence.

## Slice 77 — persist live approval expiry

Status: `implemented_verified_core`

The Activity approval countdown now persists expired approvals on the live
timer tick instead of waiting for the Activity surface to be reopened. Expired
buttons remain disabled, the status reports that no dispatch occurred, and the
existing service idempotence prevents repeated timer ticks from appending
duplicate expiry events.

Changed files:

- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the regression that resolves an unrelated pending fixture approval, expires the target approval, invokes expiry again, and verifies one durable `approval.expired` event.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: expiry is local durable workflow state. It does
not dispatch a provider, tool, shell, file, Git, or network action and adds no
credentials, dependency, or third-party code.

Rollback: remove the timer call and status update; the existing open-time
expiry behavior and approval controls remain as the weaker fallback.

Not yet verified: live Windows seeded approval expiry/countdown acceptance,
successful workflow execution, richer branching, parallel limits, OS session
hosting, Linux parity, and packaged acceptance.

## Slice 78 — enforce workflow dependency readiness

Status: `implemented_verified_core`

`WorkflowRunService.BeginStep` now refuses to start a pending step until every
declared dependency is complete or skipped. This closes a state-machine hole
that could otherwise bypass the persisted DAG order, and it provides the safe
readiness boundary needed before parallel branches or richer adapters are
introduced. Unsupported node types remain refused by their existing executor
gates.

Changed files:

- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including refusal to begin the approval step before its `test` dependency and the complete existing workflow, queue, MCP, memory, skill, and release checks.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this is a local state-machine guard. It prevents
out-of-order execution and adds no provider, network, shell, file, Git,
credential, dependency, or third-party path.

Rollback: remove the dependency check and regression assertion; the existing
topological plan and conservative unsupported-node refusal remain available.

Not yet verified: live workflow run acceptance, richer branching semantics,
parallel resource limits, automatic scheduling, side-effecting adapters,
Linux parity, and packaged acceptance.

## Slice 79 — searchable workflow-event evidence

Status: `implemented_verified_with_live_surface_boundary`

Activity search now searches both the durable operation journal and the
workflow-run event journal. Workflow results are bounded to the latest 100
events, match event type/actor/run/workflow identity or bounded event metadata,
and render through the existing allowlisted event-summary mapping rather than
exposing raw payloads. Operation and workflow results have separate accessible
list labels and the search remains non-mutating.

Changed files:

- `native/ServerRock.Core/Models/WorkflowEventSearchHit.cs`
- `native/ServerRock.Core/Services/WorkflowRunService.cs`
- `native/ServerRock.Core.Tests/WorkflowRunServiceTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including local approval-event search and the full existing Core suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (129 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Activity opened `Search
  activity`; the dialog exposed the search field, `Operation activity search
  results`, separate `Workflow event search results`, and the local-only
  `nothing was changed` status. This run had no seeded workflow event, so no
  live event-result match was claimed.

Privacy/security/licensing: search stays on local journals and does not send
queries or event data to a provider. The UI shows only bounded allowlisted
workflow summaries; no dependency or third-party code was added.

Rollback: remove `WorkflowEventSearchHit`, `SearchEvents`, the workflow result
list, its test, and the two UI contracts; operation search remains available.

Not yet verified: seeded live Windows workflow-event result matching, OS session hosting,
parallel agent artifacts, richer workflow branching, Linux parity, and
packaged acceptance.

## Slice 80 — enforce local-only policy on pinned route selection

Status: `implemented_verified_with_live_surface_boundary`

Selecting a pinned model route from the composer hotswapper now reuses the same
endpoint/model validator used by Settings. When local-only mode is enabled, a
pinned remote endpoint is rejected before model loading, settings mutation, or
connection refresh; the user receives an explicit status explaining the
blocked switch. Local route selection remains available, and no automatic
fallback or cloud route is introduced.

Changed files:

- `native/ServerRock.Native/MainPage.ModelLibrary.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including local-only route validation and the full existing Core suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (130 contracts).`
- `git diff --check` — passed.
- Live Windows boundary check — local-only mode was enabled and then restored
  through Host configuration. The composer hotswapper exposed `No pinned models
  yet`, so there was no pinned remote route available to exercise the rejection
  message without adding or changing a provider configuration. No model,
  endpoint, credential, pet setting, or cloud route was changed.

Privacy/security/licensing: the guard prevents an explicit local-only setting
from being bypassed through a previously pinned remote route. It adds no
network request, credential handling, provider fallback, dependency, or
third-party code.

Rollback: remove the selection validation and its audit/test assertion; the
Settings save guard remains, but pinned-route selection would lose the same
local-only boundary.

Not yet verified: the live Windows pinned-route rejection message itself (no
pinned route was available), capability-aware
multi-route selection, durable provider identity across fallback attempts,
Linux parity, and packaged acceptance.

## Slice 49 — serialize reviewed workspace writes

Status: `implemented_verified_core`

`WorkspaceFileEditor.Apply` now gates the complete reviewed-write transaction
by normalized target path. The hash comparison, previous-content capture,
temporary write, replacement, and receipt creation occur under the same
per-path gate, so concurrent Coding Agent/editor writes in this process cannot
both pass the same review hash. A regression harness races two reviewed writes
and verifies exactly one succeeds while the other is rejected as stale.

Changed files:

- `native/ServerRock.Core/Infrastructure/WorkspaceFileEditor.cs`
- `native/ServerRock.Core.Tests/LocalStorageAndWorkspaceTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including `workspace edits serialize reviewed writes and reject the losing concurrent hash`.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (108 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this prevents competing in-process reviewed
writes from silently overwriting one another; it does not claim OS-level file
locking against an external editor that ignores the app's transaction gate.
No file contents, credentials, network route, dependency, or third-party code
were added.

Rollback: remove the per-target gate and restore the prior `Apply` body; the
existing expected-hash check and undo receipts remain available.

Not yet verified: cross-process/external-editor race behavior, multi-tab
editing, Linux parity, packaged-release acceptance, and full live editor
accessibility.

## Slice 50 — durable one-shot scheduler state

Status: `implemented_verified_core`

Added a bounded `ScheduledTaskStore` and `ScheduledTask` model for one-shot
tasks. Scheduling persists a future UTC due time and workspace mode; claiming
atomically records `running` before any future executor is allowed to act;
completed and failed outcomes are durable; paused tasks do not claim and can
only resume explicitly. A claimed task is never replayed after restart, which
is the safe default until a reviewed execution adapter is wired into the
Activity & tasks UI.

Changed files:

- `native/ServerRock.Core/Models/ScheduledTask.cs`
- `native/ServerRock.Core/Services/ScheduledTaskStore.cs`
- `native/ServerRock.Core.Tests/ScheduledTaskTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including `scheduled task persistence, claim, no-replay, outcome, pause, and resume`.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (108 contracts).` No UI files changed.
- `git diff --check` — passed.

Privacy/security/licensing: the store persists only the user-authored prompt,
workspace mode, timestamps, bounded status, and bounded outcome. It does not
execute prompts, contact a provider, grant tools, or persist credentials. No
dependency or third-party code was added.

Rollback: remove the scheduler model/store/test registration and leave the
existing Activity & tasks placeholder in place; conversation, settings, and
operation stores are unaffected.

Not yet verified: Activity UI creation/list controls, OS session process host,
reviewed prompt execution, timezone selection, recurring tasks, backoff,
cross-process locking, Linux parity, and packaged-release acceptance.

## Slice 51 — Activity scheduling controls and reviewed prompt surfacing

Status: `implemented_verified_windows`

Activity & tasks now exposes a one-time scheduling action with accessible
prompt/date/time/workspace controls, persisted task listing, and explicit
pause/resume actions. A dispatcher timer claims due tasks exactly once. If a
response is active, the prompt is persisted into the existing bounded queue;
otherwise it is placed in the composer for review and focus is restored there.
No scheduled path bypasses model availability, remote-data consent, or the
normal Send action.

Changed files:

- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including scheduler persistence/no-replay tests.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (111 contracts).`
- `git diff --check` — passed.
- Live Windows native run — passed for opening Activity & tasks, scheduling a
  prompt with date/time/workspace controls, seeing the persisted `scheduled`
  record, and changing it to `paused` then `scheduled` through the accessible
  Pause/Resume controls. The short run did not wait long enough to prove the
  due-timer composer surfacing path.

Privacy/security/licensing: scheduling stores only the authored prompt,
workspace choice, timestamps, status, and bounded outcome. Due work is
surfaced for review or queued behind an active response; it does not silently
send a cloud request, persist credentials, or grant tools. No dependency or
third-party code was added.

Rollback: remove `MainPage.ScheduledTasks.cs`, the Activity scheduling controls,
timer, and audit contracts; the durable scheduler store remains harmless and
can be removed separately without touching chat or operation history.

Not yet verified: live Windows due-surface interaction, OS
session process hosting, automatic provider execution/result capture, timezone
and recurring schedules, backoff, cross-process locking, Linux parity, and
packaged-release acceptance.

## Slice 52 — staged, provenance-aware Markdown skill packages

Status: `implemented_verified_core`

Added `SkillPackageInstaller` as a fail-closed package boundary. A package
must provide a validated manifest and root `SKILL.md`; only bounded UTF-8
Markdown files are accepted, while scripts, binaries, symlinks, reparse
points, traversal paths, oversized files, and oversized packages are rejected.
The installer records the pinned source reference and SHA-256 package digest,
activates through a staging directory, preserves the prior version, and can
roll back to the previous activation. Package content is treated as
instructions only and is never executed by this installer.

Changed files:

- `native/ServerRock.Core/Models/InstalledSkill.cs`
- `native/ServerRock.Core/Services/SkillPackageInstaller.cs`
- `native/ServerRock.Core.Tests/SkillPackageInstallerTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including pinned provenance, digest, atomic replacement, rollback, and executable-content rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after stopping the validation app that held the output DLL.
- `npm run audit:native-ui` — passed: `Native UI audit passed (111 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: no network fetch, script execution, provider call,
credential storage, or tool grant is performed. The source reference is
recorded as provenance, while package content remains local and bounded. No
dependency or third-party code was added.

Rollback: remove the installer and test registration; the existing built-in
skill catalog remains unchanged. Installed package directories are not touched
by unrelated conversation, settings, or project stores.

Not yet verified: GitHub download/ref pinning, staged-install UI, explicit
permission review and enablement, publisher/signature trust, update diff,
cross-process activation locking, Linux parity, and packaged-release
acceptance.

## Slice 53 — installed skills in the reviewed Skills journey

Status: `implemented_verified_core`

The Skills dialog now lists installed local packages separately from built-in
read-only templates. Users can choose a local package directory, review an
explicit non-execution warning, inspect publisher/source/digest/permissions,
load bounded Markdown instructions into the composer for review, and roll back
the selected installed package. The installer remains the only activation
boundary; no package script or binary can be executed through this journey.

Changed files:

- `native/ServerRock.Core/Services/SkillPackageInstaller.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including package digest, activation, rollback, and executable rejection tests.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (114 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: package content remains local and bounded; only
Markdown is loaded into the composer, where existing review, model, and remote
consent gates still apply. No network fetch, credential persistence, tool
grant, script execution, dependency, or third-party code was added.

Rollback: remove the installed-skill controls and `ReadInstructions` bridge;
the built-in skill catalog and safe Core installer remain independently
reversible.

Not yet verified: live Windows package-picker/install/rollback acceptance,
GitHub transport and cryptographic ref verification, explicit permission
enablement, publisher trust, cross-process activation locking, Linux parity,
and packaged-release acceptance.

## Slice 54 — throttled streaming render and scroll preservation

Status: `implemented_verified_core`

Assistant streaming now accumulates provider chunks under a small response
lock and renders through a 50 ms dispatcher timer instead of rebuilding the
native text surface for every chunk. The stream captures whether the user was
already at the bottom; only that case follows the response, so a user who has
scrolled upward is not repeatedly pulled back down. A final render is performed
and the timer is stopped on both success and provider failure/cancellation.

Changed files:

- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (116 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this changes only local rendering cadence and
scroll behavior; provider payloads, persistence, credentials, and routing are
unchanged. No dependency or third-party code was added.

Rollback: restore the direct per-chunk output update and unconditional bottom
scroll in `StreamChatAsync`; provider transport and conversation persistence
remain unchanged.

Not yet verified: a packaged long-stream performance run, Linux/Skia timing,
screen-reader streaming announcements, and full live keyboard/accessibility
acceptance.

## Slice 55 — explicit MCP tool permission gate

Status: `implemented_verified_core`

The Core layer now treats MCP server manifests as untrusted metadata and
requires a separate local grant matching the server identity, tool name,
connection, and approval identity. Grants are limited to an explicit read or
write capability; read-only test tools can be authorized, while a mutating
tool is denied unless write approval is present. Input schemas are bounded and
must parse as JSON objects before any future executor is allowed to proceed.
This slice deliberately does not claim an MCP transport, server lifecycle, or
UI integration.

Changed files:

- `native/ServerRock.Core/Models/McpTool.cs`
- `native/ServerRock.Core/Services/McpToolPermissionGate.cs`
- `native/ServerRock.Core.Tests/McpToolPermissionGateTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including explicit read-only authorization, denial of an unapproved mutating tool, and invalid-schema rejection.
- `git diff --check` — passed.

Privacy/security/licensing: manifest metadata never creates authority; the
grant must come from the local application. Validation is bounded and local,
with no network calls, credentials, prompts, files, or new dependencies.

Rollback: remove `McpTool.cs`, `McpToolPermissionGate.cs`, its focused tests,
and the test-runner entry. No transport or existing integration behavior is
changed.

Not yet verified: Streamable HTTP or stdio transport, server discovery and
health, cancellation/revocation, UI permission review, real read-only MCP
server invocation, write-tool confirmation, MCP audit records, Linux parity,
and packaged-release acceptance.

## Slice 56 — bounded MCP stdio JSON-RPC client

Status: `implemented_verified_core`

The Core layer now has a supervised stdio MCP JSON-RPC client for the
initialize, initialized notification, tools/list, and tools/call exchange.
Tool manifests are parsed conservatively: only an explicit `readOnlyHint`
produces a read-only descriptor; all other tools are treated as mutating.
Every call passes through the explicit local permission gate. Requests,
arguments, response lines, result text, and request duration are bounded, and
caller cancellation is combined with a 30-second default timeout. The client
does not launch processes, store credentials, or expose a UI connection yet.

Changed files:

- `native/ServerRock.Core/Models/McpTool.cs`
- `native/ServerRock.Core/Services/McpStdioClient.cs`
- `native/ServerRock.Core.Tests/McpStdioClientTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including a scripted MCP exchange covering initialization, conservative tool classification, approved read-only invocation, denied mutating invocation, and emitted JSON-RPC messages.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (116 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the transport is stream-only and makes no
network or credential decisions. Server responses remain untrusted text;
bounded parsing, conservative mutability classification, explicit grants,
request serialization, cancellation, and timeout handling limit exposure. No
dependency or third-party code was added.

Rollback: remove `McpStdioClient`, its focused test, and the result record;
the existing standalone permission gate remains available and no current UI
or provider route changes.

Not yet verified: supervised process creation/cleanup, Streamable HTTP,
protocol negotiation beyond the bounded initialize response, schema-change
re-consent, connection health/revocation UI, audit journal events, a live
external MCP server, Linux/Windows packaged acceptance, and ACP adapters.

## Slice 57 — authenticated release manifest verifier

Status: `implemented_verified_core`

The Core layer now verifies a signed release manifest before a staged artifact
can be considered eligible for update handling. The verifier authenticates a
canonical version/channel/artifact/digest/key-id payload with a supplied RSA
public key, rejects channel mismatches and non-monotonic versions, and checks
the staged artifact SHA-256 before use. This is verification infrastructure;
the existing manual GitHub release screen remains non-installing and no
automatic download, replacement, or rollback behavior was enabled by this
slice.

Changed files:

- `native/ServerRock.Core/Models/ReleaseManifest.cs`
- `native/ServerRock.Core/Services/ReleaseManifestVerifier.cs`
- `native/ServerRock.Core.Tests/ReleaseManifestVerifierTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including valid RSA authentication and artifact hashing plus tampered-manifest, tampered-artifact, and channel-mismatch rejection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (116 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: verification is local and bounded; it does not
contact a release host, execute a package, access user data, or persist keys.
The artifact is rejected on signature, channel, version, or digest failure.
No dependency or third-party code was added.

Rollback: remove the verifier, manifest model, focused tests, and test-runner
entry. Existing manual update discovery remains unchanged.

Not yet verified: trusted public-key provisioning and rotation, signed feed
fetch integration, package signature/Authenticode/GPG verification, staged
install transaction, restart/rollback orchestration, UI diagnostics, clean
machine acceptance, Windows/Linux packaged evidence, and release publication.

## Slice 58 — MCP connection lifecycle and revocation boundary

Status: `implemented_verified_core`

The MCP Core path now has a process-local connection registry with configured,
healthy, degraded, disconnected, and revoked states. Tool authorization checks
the registered server and connection identity and requires a healthy state;
revocation is terminal for that connection and blocks subsequent stdio calls.
The registry is connected to `McpStdioClient`, so revocation is enforced
outside the UI and before a request is written to the transport. This slice is
deliberately not a durable connection store or a UI lifecycle journey yet.

Changed files:

- `native/ServerRock.Core/Models/McpTool.cs`
- `native/ServerRock.Core/Services/McpConnectionRegistry.cs`
- `native/ServerRock.Core/Services/McpStdioClient.cs`
- `native/ServerRock.Core.Tests/McpConnectionRegistryTests.cs`
- `native/ServerRock.Core.Tests/McpStdioClientTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including configured-to-healthy authorization, revocation denial, terminal reactivation refusal, and client-level revocation before wire output.
- `git diff --check` — passed.

Privacy/security/licensing: connection state and revocation remain process-local
and contain no credentials or prompt content. Exact server/connection matching,
healthy-state checks, terminal revocation, and existing bounded grants prevent
stale UI state from authorizing a new tool call. No dependency or third-party
code was added.

Rollback: remove `McpConnectionRegistry`, the lifecycle model, its tests, and
the optional client registry dependency. The standalone MCP permission gate
and transport remain usable.

Not yet verified: durable connection persistence, process supervision and
cleanup, health probes, disconnect recovery, Streamable HTTP, UI permission
review/revocation, audit journal events, live external MCP server acceptance,
Linux/Windows packaged evidence, and ACP adapters.

## Slice 59 — durable MCP connection metadata

Status: `implemented_verified_core`

MCP connection metadata now persists atomically in the application-data area.
The store is bounded to 64 connections, validates identities, transport and
lifecycle states, rejects duplicates, and stores no endpoint credentials or
environment values. The registry reloads this metadata on startup, persists
configuration and state changes, and retains terminal revocation across a
reload. This slice does not launch or supervise a server process and does not
claim a UI connector journey.

Changed files:

- `native/ServerRock.Core/Services/McpConnectionStore.cs`
- `native/ServerRock.Core/Services/McpConnectionRegistry.cs`
- `native/ServerRock.Core.Tests/McpConnectionRegistryTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including atomic store reload and persisted revocation.
- `git diff --check` — passed.

Privacy/security/licensing: only reviewed connection identity, transport,
manifest digest, state, and bounded error metadata are persisted. Writes use
the existing temp-file-then-rename store primitive; malformed data is rejected
without replacing the file. No credentials, prompts, executable paths, or
third-party code were added.

Rollback: remove `McpConnectionStore` and pass no store to
`McpConnectionRegistry`; the process-local lifecycle and permission gate can
continue without persistence.

Not yet verified: encrypted credential references, process supervision and
cleanup, health probes, disconnect recovery, Streamable HTTP, UI lifecycle and
revocation review, audit journal events, live external MCP server acceptance,
Linux/Windows packaged evidence, and ACP adapters.

## Slice 60 — MCP Streamable HTTP transport

Status: `implemented_verified_core`

The Core layer now supports the JSON-response path of MCP Streamable HTTP.
It validates HTTPS endpoints (or loopback-only HTTP), rejects credentials,
query/fragment data, and redirects, applies the same bounded JSON-RPC request
and response limits as stdio, and routes tool calls through the shared
permission and connection-revocation boundary. Cancellation and a bounded
request timeout are applied to the HTTP operation. SSE response framing,
session negotiation, credential references, and UI configuration are
deliberately not claimed yet.

Changed files:

- `native/ServerRock.Core/Services/McpStreamableHttpClient.cs`
- `native/ServerRock.Core.Tests/McpStreamableHttpClientTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed with a scripted HTTP endpoint covering initialize, tool discovery, approved read-only invocation, revocation denial before request, and redirect refusal.
- `git diff --check` — passed.

Privacy/security/licensing: no credentials are accepted or persisted, public
HTTP is limited to loopback, redirects are refused to prevent host substitution,
and payloads/results are bounded. The injected HTTP client remains under the
caller’s ownership; no dependency or third-party code was added.

Rollback: remove `McpStreamableHttpClient`, its focused test, and the test-runner
entry. Stdio transport, connection storage, and permission policy remain
independent.

Not yet verified: Streamable HTTP SSE/session support, live endpoint health and
reconnect, process supervision, UI lifecycle/revocation review, audit journal
events, live external MCP server acceptance, Linux/Windows packaged evidence,
and ACP adapters.

## Slice 61 — bounded MCP Streamable HTTP SSE responses

Status: `implemented_verified_core`

The Streamable HTTP client now accepts both JSON responses and
`text/event-stream` responses. SSE parsing ignores event metadata, bounds each
line, skips empty and `[DONE]` frames, parses the first JSON-RPC data frame,
and applies the same response-id, error, output, timeout, cancellation, and
connection-revocation checks as the JSON path. The client still does not claim
MCP session negotiation or long-lived event subscriptions.

Changed files:

- `native/ServerRock.Core/Services/McpStreamableHttpClient.cs`
- `native/ServerRock.Core.Tests/McpStreamableHttpClientTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed with a scripted endpoint returning an SSE `tools/call` result, plus revocation and redirect negative cases.
- `git diff --check` — passed.

Privacy/security/licensing: SSE data is treated as untrusted bounded text;
event metadata does not gain authority, and no credentials or persistent
subscriptions are created. No dependency or third-party code was added.

Rollback: restore JSON-only response handling and the SSE assertion in the
focused test; stdio, JSON HTTP, permission, and persistence paths remain
independent.

Not yet verified: MCP session IDs and protocol negotiation, long-lived
notifications/resources, health/reconnect, process supervision, UI lifecycle
and revocation review, audit journal events, live external MCP server
acceptance, Linux/Windows packaged evidence, and ACP adapters.

## Slice 62 — MCP Streamable HTTP session negotiation

Status: `implemented_verified_core`

The Streamable HTTP client now captures a bounded `Mcp-Session-Id` from the
initialize response and sends it, together with the negotiated
`MCP-Protocol-Version`, on subsequent requests. Invalid or oversized session
identifiers fail closed. The session is scoped to the client instance and is
not persisted or treated as a credential; long-lived notification/resource
subscriptions remain outside this slice.

Changed files:

- `native/ServerRock.Core/Services/McpStreamableHttpClient.cs`
- `native/ServerRock.Core.Tests/McpStreamableHttpClientTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including scripted initialize/session response, subsequent session/protocol headers, SSE tool invocation, revocation denial, and redirect refusal.
- `git diff --check` — passed.

Privacy/security/licensing: session identifiers remain process-local, bounded,
and never enter persistence, prompts, logs, or credentials. No endpoint
redirects or secret-bearing URLs are accepted. No dependency or third-party
code was added.

Rollback: remove the session fields/header handling and the focused header
assertion; JSON/SSE parsing and permission/revocation checks remain intact.

Not yet verified: long-lived notifications/resources, session expiry and
reconnect, health probes, process supervision, UI lifecycle/revocation review,
audit journal events, live external MCP server acceptance, Linux/Windows
packaged evidence, and ACP adapters.

## Slice 63 — deterministic conversation context-window planner

Status: `implemented_verified_core`

Core now includes a bounded context planner used by the OpenAI-compatible
provider when the serialized request exceeds its existing 8 MB ceiling. It
preserves system messages that fit, selects the newest turns that fit the
requested character budget, inserts an explicit untrusted omission marker when
older history is dropped, and never mutates the source history or invents a
summary. Normal-sized requests are unchanged. This is a deterministic
compaction boundary, not semantic model summarization, persistent memory, or a
claim of 1M+ token support; the budget remains an internal provider safety
limit.

Changed files:

- `native/ServerRock.Core/Services/ConversationContextPlanner.cs`
- `native/ServerRock.Core/Services/OpenAiCompatibleProvider.cs`
- `native/ServerRock.Core.Tests/ConversationContextPlannerTests.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including newest-turn/system preservation, omitted-history marking, source immutability, empty-history handling, oversized multi-turn provider compaction before network access, and preservation of single-message oversize rejection.
- `git diff --check` — passed.

Privacy/security/licensing: planning is local and deterministic; omitted text
is not sent, persisted, or summarized by another service. The marker instructs
the model not to assume missing details. No dependency or third-party code was
added.

Rollback: remove the planner call and focused overflow test, then restore the
provider’s direct 8 MB rejection. Existing normal-sized provider message
assembly remains unchanged.

Not yet verified: user-visible budget controls, exact provider-token accounting,
semantic summarization with consent,
persistent memory, restart continuity, provider-specific context limits,
1M+ token inference, Linux parity, and packaged acceptance.

## Slice 64 — explicit local memory store

Status: `implemented_verified_core`

Core now provides a bounded, atomic memory store for user-approved facts or
project notes. Every entry requires an approval identity and records scope,
source, tags, enabled state, and timestamps. Search is local and bounded;
entries can be disabled, edited, or explicitly deleted, and changes survive
reload. The store does not automatically extract memories from prompts or
responses, inject entries into model requests, sync across devices, or store
credentials.

Changed files:

- `native/ServerRock.Core/Models/MemoryEntry.cs`
- `native/ServerRock.Core/Services/MemoryStore.cs`
- `native/ServerRock.Core.Tests/MemoryStoreTests.cs`
- `native/ServerRock.Core.Tests/Program.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including approved project search, enable/disable retrieval, edit/reload, explicit durable deletion, and invalid-content rejection.
- `git diff --check` — passed.

Privacy/security/licensing: memory is local-only, explicitly approved, bounded,
atomically persisted, and never sent to a provider by this slice. Multiline
notes are allowed, but other control characters and oversized entries are
rejected. No dependency or third-party code was added.

Rollback: remove `MemoryEntry`, `MemoryStore`, and its focused test/runner
entry. Existing conversations and settings remain independent; any stored
memory file is left for explicit user handling rather than silently deleted.

Not yet verified: native memory management UI, review before injection,
project/global retention controls, encrypted storage, backup/export, automatic
memory proposals, model-context injection, cross-device sync, Linux parity,
and packaged acceptance.

## Slice 65 — explicit native memory management journey

Status: `implemented_verified_windows`

Settings → Chats & history now exposes a Memory manager backed by the tested
local store. The surface searches all saved entries, displays scope/source/
approval metadata, creates explicitly approved notes, edits content and tags,
enables or disables retrieval eligibility, and deletes selected entries. The
screen clearly states that memory is local, not automatically extracted, and
not injected into model requests by this slice. It does not touch companion
settings or code.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the memory store persistence and mutation suite.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (120 contracts).`
- `git diff --check` — passed.
- Live Windows UI inspection — passed by launching the exact `net10.0-desktop`
  executable and opening Navigation → More → Connection & settings → Chats &
  history → Manage memory. The dialog exposed the local-only disclosure,
  Search saved memory, Saved memory entries, scope/source/content/tags fields,
  enable checkbox, and New/Save/Disable/Delete controls, with `0 saved entries
  · local only`. No memory entry was created or deleted during inspection.

Privacy/security/licensing: the UI writes only when the user clicks Save,
generates a local approval ID for that explicit action, and uses the existing
atomic store. No memory content is sent to a provider, logged, or synced by
this journey. No dependency or third-party code was added.

Rollback: remove the Memory manager section, its field, and the four static UI
contracts; the Core memory file and store remain independent for explicit
future recovery or removal.

Not yet verified: keyboard/screen-reader CRUD journey, memory export or backup,
encrypted storage, retention controls, review-before-injection,
automatic memory proposals, model-context injection, cross-device sync, Linux
parity, and packaged acceptance.

## Slice 66 — explicit local memory backup export

Status: `implemented_verified_core`

The Memory manager now offers an explicit JSON backup export. The export is
generated from the validated local store, includes enabled and disabled entries
with approval/provenance metadata, uses a versioned format marker, and writes
only after the user chooses a destination in the native save picker. There is
no automatic import, provider upload, or model injection.

Changed files:

- `native/ServerRock.Core/Services/MemoryStore.cs`
- `native/ServerRock.Core.Tests/MemoryStoreTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the versioned backup marker and disabled-entry preservation assertion.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors after closing the development app process that held its output DLL.
- `npm run audit:native-ui` — passed: `Native UI audit passed (121 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: backup content stays local until the user chooses
the destination and is written through the native file picker. The export does
not send memory to a provider, add credentials, or alter the source store. No
dependency or third-party code was added.

Rollback: remove `MemoryStore.ExportJson`, the export button/picker, its test,
and its static UI contract; saved memory management remains available.

Not yet verified: live Windows export-picker acceptance, memory restore/import,
encrypted storage, retention controls, review-before-injection, automatic
memory proposals, model-context injection, cross-device sync, Linux parity,
and packaged acceptance.

## Slice 70 — safe local memory backup restore

Status: `implemented_verified_core`

The Memory manager now supports importing its versioned JSON backup format
through a bounded native file picker. Import first creates a validated plan,
reports new versus duplicate identities, and requires explicit confirmation;
existing entries are never overwritten. Imported entries retain their original
approval, provenance, tags, timestamps, and enabled state.

Changed files:

- `native/ServerRock.Core/Services/MemoryStore.cs`
- `native/ServerRock.Core.Tests/MemoryStoreTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including import planning, merge, and duplicate-identity protection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (124 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed by launching the exact
  `net10.0-desktop` executable, opening Settings → Chats & history → Manage
  memory, and observing the local-only disclosure plus Import saved memory
  backup and Export saved memory backup controls. The empty state reported
  `0 saved entries · local only`; no memory data was changed.

Privacy/security/licensing: parsing is local and bounded to 12 MB; malformed,
unsupported, or over-capacity backups are rejected before persistence. Import
does not contact a provider or silently replace user data. No dependency or
third-party code was added.

Rollback: remove `MemoryStore.PlanImport`/`ApplyImport`, the import picker and
confirmation flow, its tests, and the static UI contract; existing memory
editing and export remain available.

Not yet verified: live Windows picker selection and confirmation acceptance,
encrypted storage, retention controls, review-before-injection, automatic
memory proposals, model-context injection, cross-device sync, Linux parity,
and packaged acceptance.

## Slice 71 — locale-independent conversation search

Status: `implemented_verified_core`

Conversation search now uses ordinal case-insensitive matching for message,
tag, attachment, and source text. This keeps search behavior deterministic
across machine cultures and adds a regression assertion for uppercase
attachment queries.

Changed files:

- `native/ServerRock.Core/Services/ConversationSearchService.cs`
- `native/ServerRock.Core.Tests/ConversationSearchTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the culture-independent uppercase attachment query.
- `git diff --check` — passed in commit `f10f09c`.

Privacy/security/licensing: matching remains local-only, adds no data flow or
dependency, and does not change stored conversation content.

Rollback: restore the prior culture-sensitive comparison and remove the
uppercase-search regression assertion.

Not yet verified: full cross-platform locale matrix and live accessibility
acceptance for every search result state.

## Slice 72 — bounded recurring scheduled tasks

Status: `implemented_verified_core`

The durable scheduler now supports one-hour, two-hour, daily, and weekly
recurrence options while preserving one-shot scheduling as the default. A
successful surfaced run is durably rescheduled with its run count incremented;
failed runs remain failed for review and are not automatically retried. Existing
pause/resume and no-replay behavior remain explicit and durable.

Changed files:

- `native/ServerRock.Core/Models/ScheduledTask.cs`
- `native/ServerRock.Core/Services/ScheduledTaskStore.cs`
- `native/ServerRock.Core.Tests/ScheduledTaskTests.cs`
- `native/ServerRock.Native/MainPage.ScheduledTasks.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including recurring claim, durable rescheduling, run-count, and early-replay protection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (125 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: recurrence stores only bounded local task metadata;
it does not execute a model request automatically. Each surfaced prompt still
passes through the existing review, availability, consent, and send controls.
No dependency or third-party code was added.

Rollback: remove `RepeatEveryMinutes`/`RunCount`, the recurrence branch in
completion, the recurrence picker/display, its tests, and the static UI
contract; one-shot task behavior remains available.

Not yet verified: live Windows recurring-task creation and pause/resume,
timezone/locale matrix, backoff policies, autonomous background execution,
parallel agents, Linux parity, and packaged acceptance.

## Slice 73 — explicit installed-skill enablement

Status: `implemented_verified_with_live_surface_boundary`

Installed local skills are now disabled by default and must be explicitly
enabled before their Markdown instructions can be loaded into the composer.
Enablement is stored atomically in the active manifest, displayed with the
publisher/source/digest details, and can be revoked without uninstalling the
package. Updates remain disabled until the new package is reviewed; rollback
restores the prior package and its prior enablement state.

Changed files:

- `native/ServerRock.Core/Models/InstalledSkill.cs`
- `native/ServerRock.Core/Services/SkillPackageInstaller.cs`
- `native/ServerRock.Core.Tests/SkillPackageInstallerTests.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including disabled-by-default, durable enablement, update, rollback, and executable-rejection checks.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (126 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: the Skills dialog exposed the
  read-only built-in catalog, installed-skills list, disabled `Use skill`, local
  package install, explicit enable/disable, rollback, and Done/Close controls;
  no package was installed, enabled, or changed.

Privacy/security/licensing: skill content remains local Markdown and is never
executed. The explicit grant state prevents a newly installed or updated
package from entering a prompt until the user reviews and enables it. No
dependency or third-party code was added.

Rollback: remove `Enabled`, `SetEnabled`, the UI control, test assertions, and
static UI contract; the existing package validation and rollback paths remain.

Not yet verified: GitHub transport/ref verification, publisher trust, live
Windows permission-control execution, cross-platform packaging, and update
diff review.

## Slice 74 — skill package update preview

Status: `implemented_verified_with_live_surface_boundary`

Local skill installation now performs a read-only package preview before the
explicit install confirmation. The preview validates the Markdown-only package,
shows version and publisher, computes the package digest, lists requested
permissions, and identifies whether the package replaces an existing version.
The install confirmation states that nothing executes automatically and that
the package remains disabled until explicitly enabled.

Changed files:

- `native/ServerRock.Core/Models/InstalledSkill.cs`
- `native/ServerRock.Core/Services/SkillPackageInstaller.cs`
- `native/ServerRock.Core.Tests/SkillPackageInstallerTests.cs`
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including pre-install digest/permission preview and update detection.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: the Skills dialog exposed the
  installed-skills area and local package-install entry point; no package was
  selected or installed, so the preview confirmation was not altered.

Privacy/security/licensing: preview reads only the user-selected local folder,
does not activate or execute it, and does not perform network access. The
digest and permissions are shown before confirmation. No dependency or
third-party code was added.

Rollback: remove `SkillPackagePreview`, the preview method/dialog content, its
tests, and the static UI contract; existing package validation and explicit
enablement remain available.

Not yet verified: GitHub transport/ref verification, publisher trust, live
Windows preview confirmation, cross-platform packaging, and update diff review.

## Slice 75 — pinned GitHub skill-reference validation

Status: `implemented_verified_core`

Skill installation now validates source provenance before reading or activating
a package. Local-folder references remain supported, while GitHub references
must use an HTTPS `github.com/owner/repository@<hex commit ref>` form with no
credentials, query, fragment, or floating branch/tag. This is a format guard,
not a claim that the remote commit exists; transport and remote ref resolution
remain separate future work.

Changed files:

- `native/ServerRock.Core/Models/SkillSourceReference.cs`
- `native/ServerRock.Core/Services/SkillPackageInstaller.cs`
- `native/ServerRock.Core.Tests/SkillPackageInstallerTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including rejection of a floating `@main` reference and preservation of pinned test references.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (127 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: validation is local-only and happens before package
file processing; it adds no network request, credential handling, dependency,
or remote trust decision.

Rollback: remove `SkillSourceReference`, the installer validation call, and the
invalid-reference test; existing local package safety checks remain.

Not yet verified: GitHub download/authentication, remote commit existence,
publisher trust, live Windows source-reference controls, cross-platform
packaging, and update diff review.

## Slice 67 — explicit local activity evidence export

Status: `implemented_verified_core`

Activity & tasks now offers an explicit JSON export of the durable operation
journal. The export is validated before serialization, carries a versioned
format marker, and includes run/event provenance so users can preserve local
evidence for review without changing operation state or replaying any side
effect.

Changed files:

- `native/ServerRock.Core/Infrastructure/OperationJournal.cs`
- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Core.Tests/OperationJournalTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the versioned export and reconciliation-event assertion.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (122 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the evidence bundle is written only after an
explicit native save-picker choice, remains local, and is clearly reported as
not sent to a provider. It can contain the user's operation instructions and
must be treated as sensitive when shared. No dependency or third-party code
was added.

Rollback: remove the journal/service export methods, Activity button/picker,
test assertion, and static UI contract; the existing Activity surface and
durable journal remain available.

Not yet verified: live Windows export-picker acceptance, searchable/exported
operation evidence beyond the full journal, OS session hosting, parallel agent
artifacts, Linux parity, and packaged acceptance.

## Slice 68 — bounded local activity search

Status: `implemented_verified_core`

Activity & tasks now includes a non-mutating search dialog for durable
operations. Queries are bounded and match local instruction, status, model,
provider, or workspace provenance; results are capped and ordered by recent
activity. The surface explicitly reports that searching does not change an
operation.

Changed files:

- `native/ServerRock.Core/Infrastructure/OperationJournal.cs`
- `native/ServerRock.Core/Services/OperationService.cs`
- `native/ServerRock.Core.Tests/OperationJournalTests.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including local provenance matching and no-result behavior.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (123 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: search stays on the local operation journal and
does not send queries or instructions to a provider. Query length and control
characters are rejected before processing, and no dependency was added.

Rollback: remove the journal/service search methods, Activity search dialog,
test assertion, and static UI contract; activity export and operation history
remain available.

Not yet verified: live Windows search accessibility acceptance, searchable
workflow-event evidence, OS session hosting, parallel agent artifacts, Linux
parity, and packaged acceptance.

## Slice 69 — centralized rebrand-safe product identity

Status: `implemented_verified_core`

User-facing native labels now read from the centralized `ProductIdentity` and
build `ProductDisplayName` settings instead of repeating the current product
name throughout sidebar, messages, update, archive, backup, terminal, GitHub,
and MCP client surfaces. Existing technical storage paths, namespaces,
credential entropy, package identifiers, and protocol formats remain unchanged
so a future rebrand does not migrate or invalidate user data.

Changed files:

- `native/ServerRock.Core/Models/ProductIdentity.cs`
- `native/Directory.Build.props`
- `native/ServerRock.Native/ServerRock.Native.csproj`
- `native/ServerRock.Native/SidebarView.xaml`
- `native/ServerRock.Native/SidebarView.xaml.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `native/ServerRock.Native/MainPage.Messages.cs`
- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.Sidebar.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.ImportExport.cs`
- `native/ServerRock.Native/MainPage.GitHub.cs`
- `native/ServerRock.Core/Infrastructure/WorkspaceTerminalSession.cs`
- `native/ServerRock.Core/Services/McpStdioClient.cs`
- `native/ServerRock.Core/Services/McpStreamableHttpClient.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (123 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: this is a local label/configuration change only;
it adds no network traffic, credential changes, data migration, or dependency.

Rollback: restore the prior display literals and application title property;
technical identifiers remain unaffected either way.

Not yet verified: packaged installer title/name propagation, Linux display
acceptance, and the final selected rebrand name.

## Slice 48 — session-only BYOK for compatible providers

Status: `implemented_verified_core`

The native connection dialog now accepts one optional bearer token for any
OpenAI-compatible endpoint. The same session-only value is used for model
discovery, chat streaming, prompt rewriting, and local benchmark requests;
the value is cleared or replaced when the user saves connection settings and
is never added to `AppSettings` or ordinary persistence. The OpenRouter
shortcut continues to prefill its endpoint and free-model route, but the
credential path is no longer provider-specific.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (108 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the token remains in process memory only, is
passed as a bearer header by the existing bounded provider transport, and is
not written to settings, logs, prompts, diagnostics, or URLs. No dependency
or third-party code was added.

Rollback: restore the OpenRouter-only key field and `GetProviderApiKey` branch;
the existing local endpoint flow remains unchanged.

Not yet verified: live hosted-provider discovery/chat with a real user key,
OS keyring-backed hosted credentials, provider-specific scopes/quotas, Linux
parity, and packaged-release acceptance.

## Slice 46 — block sends for definitively unavailable discovered models

Status: `implemented_verified_windows`

The native connection refresh now retains the provider-advertised model
inventory, and the send path performs a local selection check before smart
rewriting, web search, consent prompts, or provider streaming. When the
inventory is non-empty and excludes the selected model, Send leaves the draft
untouched and announces that the user must choose a model in Model Library.
If discovery fails and no inventory is known, the guard stays permissive so the
user can retry the connection instead of receiving a false incompatibility
claim. No automatic fallback or cloud route is introduced.

Changed files:

- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Connection.cs`
- `native/ServerRock.Native/MainPage.Chat.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (106 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: after temporarily selecting
  `missing-model`, sending `guard must block this request` kept the draft in
  the composer and announced `Selected model missing-model is unavailable ·
  choose a model in Model Library before sending`; no turn was created. The
  original model was restored afterward.

Privacy/security/licensing: the check uses only model IDs already returned by
the configured endpoint. It prevents an unintended request and does not send
prompt, attachment, project, credential, or web-search data.

Rollback: remove `_advertisedModels`, the connection inventory assignment, the
pre-send selection guard, and the two audit contracts; the existing connection
status and provider failure recovery remain available.

Not yet verified: proactive checks for richer advertised capability mismatches,
multi-route provider selection, durable provider identity across fallback
attempts, live Git dialog acceptance, remote fetch/reconciliation before push,
worktree isolation, Linux parity, and packaged-release acceptance.

## Slice 47 — refresh remote Git state before push

Status: `implemented_verified_core`

`PushAsync` now performs a bounded `git fetch --prune --no-tags origin` before
the final ahead/behind decision, then re-reads repository status. A fetch
failure leaves local commits untouched and reports that remote state could not
be refreshed. If the refreshed branch is behind, the existing no-force refusal
still stops the push and directs the user to review the remote changes. The
push path remains explicit and never uses force.

Changed files:

- `native/ServerRock.Core/Services/GitHubWorkspaceService.cs`
- `native/ServerRock.Core.Tests/GitHubTests.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the ahead/behind parser regression used by push preflight.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (106 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: fetch uses the already-authorized GitHub access
token through the existing process environment credential path, never the
command line or remote URL. It sends only Git protocol metadata and does not
send prompts, workspace file contents, or conversation data.

Rollback: remove the fetch and second status read in `PushAsync`; the existing
behind-count refusal and non-force push remain as a weaker safety boundary.

Not yet verified: live Git dialog push with a user-selected repository and
credential, remote-update race against an actual GitHub branch, worktree
isolation, proactive model capability mismatch checks, multi-route provider
selection, durable provider identity across fallback attempts, Linux parity,
and packaged-release acceptance.

## Slice 45 — isolate selected Git commits from the user index

Status: `implemented_verified_windows_core`

Selected Git commits now use a temporary `GIT_INDEX_FILE` initialized from
`HEAD`. The reviewed paths are staged and committed through that isolated index,
so unrelated changes the user had already staged remain untouched and cannot
accidentally enter the selected commit. The temporary index is removed after
success or failure; the user's working tree and real index are never reset.

Changed files:

- `native/ServerRock.Core/Services/GitHubWorkspaceService.cs`
- `native/ServerRock.Core.Tests/LocalStorageAndWorkspaceTests.cs`
- `native/ServerRock.Core.Tests/TestSupport.cs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including the regression that pre-stages `pre-staged-user-work.md`, commits only the selected safe review changes, and verifies the unrelated file remains staged and absent from `HEAD`.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (104 contracts).`
- `git diff --check` — passed.

Privacy/security/licensing: the isolated index prevents accidental disclosure
or publication of unrelated user changes. It uses local Git state only, never
places credentials in arguments or remotes, and adds no dependency.

Rollback: restore the prior `CommitSelectedCoreAsync` staging/commit calls and
remove the isolated-index fixture assertion; the existing path and secret
checks remain available.

Not yet verified: live Git dialog acceptance with a user-selected repository,
remote fetch/reconciliation before push, worktree isolation, proactive model
compatibility detection, multi-route provider selection, durable provider
identity across fallback attempts, Linux parity, and packaged-release
acceptance.

## Slice 43 — inline settings validation and draft cancellation

Status: `implemented_verified_windows`

Settings now validates the endpoint, model ID, and local-only policy through a
pure Core validator before any settings snapshot is committed. Invalid Save
attempts cancel the dialog close, show an assertive accessible inline message,
return to the General section, and focus the relevant draft field. Cancel still
discards the entire draft, so malformed or incomplete edits cannot replace the
last working connection.

Changed files:

- `native/ServerRock.Core/Services/ConnectionSettingsValidation.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.Settings.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including valid local, malformed endpoint, missing model, and local-only remote rejection cases.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (101 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Settings opened through the
  command palette; changing the endpoint to `not an endpoint` and choosing Save
  kept Settings open and announced `Enter a valid http:// or https:// model
  endpoint.`; choosing Cancel returned to chat with the original endpoint/model
  still selected.

Privacy/security/licensing: validation is local-only and uses no provider call,
credential, prompt, or workspace data. It reduces accidental endpoint changes
and preserves the user's last valid connection on cancellation.

Rollback: remove `ConnectionSettingsValidator`, the primary-click validation
handler, and its tests; the existing post-dialog checks remain as a weaker
fallback.

Not yet verified: broader settings validation matrix, platform-specific
diagnostics, proactive model compatibility detection, multi-route provider
selection, durable provider identity across fallback attempts, Linux parity, and
packaged-release acceptance.

## Slice 44 — preserve composer drafts across workspaces

Status: `implemented_verified_windows`

The Chat and Coding Agent workspaces now keep independent in-memory composer
drafts while the user switches between them. Text changes update the active
workspace draft, mode switching restores the destination draft, and starting a
new chat clears only the active workspace draft. Drafts are bounded to 100,000
characters and are intentionally not persisted as conversation history or
settings.

Changed files:

- `native/ServerRock.Core/Services/WorkspaceDraftStore.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.xaml.cs`
- `native/ServerRock.Native/MainPage.Navigation.cs`
- `native/ServerRock.Native/MainPage.Recents.cs`
- `native/ServerRock.Native/MainPage.SlashCommands.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed, including isolated Chat/Coding Agent draft state.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (104 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Chat draft
  `chat draft survives workspace switch` and Coding Agent draft
  `coding draft survives workspace switch` were entered separately; switching
  to Coding Agent and back restored the exact Chat draft and retained the same
  selected model.

Privacy/security/licensing: drafts remain in process memory only, never enter
conversation persistence or provider requests until the user sends them, and
no dependency or third-party code was added.

Rollback: remove `WorkspaceDraftStore`, the draft field/restore calls, and the
three UI audit contracts; existing mode switching continues with its prior
composer behavior.

Not yet verified: draft persistence across app restart, attachment-draft
continuity, typed navigation history, persistent tabs/layout, proactive model
compatibility detection, multi-route provider selection, durable provider
identity across fallback attempts, Linux parity, and packaged-release
acceptance.

## Slice 42 — preserve unavailable selected models until explicit choice

Status: `implemented_verified_windows`

The connection refresh path now evaluates the saved model against the provider's
advertised model list without silently replacing it or persisting the first
available model. When the saved model is unavailable, the runtime status tells
the user to choose a model in Model Library while the selected model identity
remains visible. Model changes continue to happen only through an explicit user
selection path.

Changed files:

- `native/ServerRock.Core/Services/ModelSelectionPolicy.cs`
- `native/ServerRock.Core.Tests/ChatProviderTests.cs`
- `native/ServerRock.Native/MainPage.Connection.cs`
- `scripts/audit-native-ui.cjs`

Evidence:

- `dotnet run --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj` — passed.
- `dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop --no-restore` — passed with 0 warnings and 0 errors.
- `npm run audit:native-ui` — passed: `Native UI audit passed (98 contracts).`
- `git diff --check` — passed.
- Live Windows accessibility inspection — passed: Settings temporarily saved
  `missing-model`; runtime status announced that the selected model was
  unavailable and directed the user to Model Library, while ModelButton still
  exposed `missing-model`. The original model was restored afterward.

Privacy/security/licensing: preserving the explicit selection prevents an
unexpected local model change and avoids silently changing provider behavior.
The policy uses only the locally advertised model list and adds no credentials,
network route, prompt data, or dependency.

Rollback: restore the prior unavailable-model branch in
`MainPage.Connection.cs` and remove `ModelSelectionPolicy`; the existing
explicit Model Library selection path remains available.

Not yet verified: proactive compatibility detection before a request,
multi-route provider selection, durable provider identity across fallback
attempts, Linux parity, and packaged-release acceptance.
