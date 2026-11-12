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

Status: `implemented_verified_core`

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

Privacy/security/licensing: skill content remains local Markdown and is never
executed. The explicit grant state prevents a newly installed or updated
package from entering a prompt until the user reviews and enables it. No
dependency or third-party code was added.

Rollback: remove `Enabled`, `SetEnabled`, the UI control, test assertions, and
static UI contract; the existing package validation and rollback paths remain.

Not yet verified: GitHub transport/ref verification, publisher trust, live
Windows permission-control acceptance, cross-platform packaging, and update
diff review.

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
