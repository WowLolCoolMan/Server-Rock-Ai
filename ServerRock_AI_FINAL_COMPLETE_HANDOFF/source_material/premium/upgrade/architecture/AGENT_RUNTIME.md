# Agent execution engine and durable operation semantics

## Core concepts
`Workspace` identifies authorized roots. `Task` is user goal + parameters + model/profile and contains `Run`s. `Run` is one execution attempt with immutable id and event stream. `Step` is plan/tool/model/test/approval activity. `Artifact` is a proposed or produced file/diff/test/log linked to source step. `Approval` is scoped grant for a specific operation, never reusable silently.

## State machine
```
CREATED → PREFLIGHT → QUEUED → RUNNING → VERIFYING → AWAITING_REVIEW → COMPLETED
             ↘ BLOCKED_AUTH  ↘ WAITING_APPROVAL ↗       ↘ FAILED
             ↘ FAILED         ↘ PAUSING → PAUSED → RUNNING
                               ↘ CANCELLING → CANCELLED
```
Enforce allowed transitions centrally with optimistic concurrency (revision sequence). `RUNNING→COMPLETED` without verification/review is only allowed for tasks explicitly declared no-write/answer-only, with reason recorded. `CANCEL_REQUESTED` does not equal `CANCELLED` until the executor has acknowledged and reaped processes.

## Execution loop
1. Resolve project and authorized roots, provider and capabilities, context scope, prior history and privacy routing.
2. Create immutable run ID and append `run.created` before irreversible work.
3. Enqueue under resource budgets (CPU/GPU tokens, provider concurrency and process limits). Preflight paths, tools and secret scopes.
4. Generate plan and call agent/model adapter. Stream events as complete ordered records with run_id/seq/timestamp and schema version.
5. Before side effects, construct canonical tool proposal (tool, arguments, workspace root, intended effects, risk). Permission decision in trusted application service; wait for explicit approval if needed.
6. Execute under scoped environment; redacted outputs and exit codes journaled; detect errors, retries and cancel.
7. Verify changed files with baseline hash, tests (only if actually run) and diff. Show to reviewer; no auto push/commit without user policy and approval.
8. Persist terminal state and artifacts. On app restart rehydrate projection and mark interrupted processes accurately. Resumption is an explicit new safe attempt unless process supervision proves original still exists.

## Event journal contract
Every event: `schemaVersion`, `eventId`, `runId`, `sequence`, `timestampUtc`, `type`, `actor`, `correlationId`, `payload`, `sensitivity`, optional `causedBy` and `idempotencyKey`. Append atomically; monotonic sequence per run; unique event id; replay produces same state projection; UI never infers completion from last streamed token. Snapshot projections periodically, but journal remains truth.

## Parallel agents
- `AgentRun` gets unique workspace modification context, preferably dedicated Git worktree/branch. Coordinate shared GPU and provider rate constraints.
- Scheduler supports explicit concurrency limit, priorities, fairness, cancellation and configurable per-model memory budget; different provider models can run concurrently if resources permit.
- Parent task has child run IDs, dependency graph, success policy and merge gate. A finished subagent yields artifact + diff + tests for review; it does not silently merge into primary worktree.
- If agent is read-only it may share a workspace snapshot with others. If write-enabled, no overlapping writes to same root without a conflict strategy.

## Restart/recovery
Journal writes are transactional/durable; no claim of resumed external processes without verification. At startup inspect incomplete runs; mark recovered, interrupted or awaiting action. Never replay a side-effecting tool call without idempotency key/reconciliation and approval. Persist progress without storing full secrets. Provide run replay in Activity for audit.

## Failure taxonomy
Network timeout, auth denied, quota/rate limit, provider capacity, malformed output, tool timeout, invalid tool args, command exit nonzero, filesystem conflict, low disk, model OOM, process crash, platform unsupported, permission revoked. Each has user-facing copy, retry safety, diagnostic category and known no-write guarantee if possible.

## Acceptance
Simulate crash at every transition; race stop vs completion; simultaneous event delivery; duplicate provider event; stale approval; UI reload while terminal active; two agents editing same file; local model OOM; update download aborted; no duplicate writes/false completion.
