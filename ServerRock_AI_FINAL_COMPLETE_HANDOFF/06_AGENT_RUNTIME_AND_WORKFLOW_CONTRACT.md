# Agent host, task orchestrator, provider routing and workflows

## Runtime boundary

UI is a client of an explicit application service; UI elements must not create their own untracked shell processes or provider HTTP calls. AgentHost owns execution identity, lifecycle, planning, cancellation, tools, data locality and event journal. The frontend observes immutable state by task ID and requests operations through service interfaces. Isolate untrusted tools in a child process where appropriate for fault containment; even if the initial host is in-process, tasks need durable bookkeeping and accurate interruption recovery.

## Canonical task state machine

`draft → validating → queued → running ↔ awaiting_approval → verifying → review_ready → completed`.

Failure transitions: `running/queued/verifying → cancelling → cancelled`; `running/verifying → failed`; any interrupted live task after crash → `needs_reconciliation` with checks before permitted resume; paused tasks → resumed only when runner supports it. Do not mark `completed` just because the UI window closes or a provider stream ends early.

Each task persists: taskId, runId, workspaceId, model/provider/agent IDs, profile, execution policy, worktree ID, user instruction, plan revisions, status, tool invocations, approval tokens, durable checkpoints, verified outputs, cost/usage metadata if reported, and last event sequence. Journal envelopes have schemaVersion/eventId/runId/monotonic sequence/timestampUTC/type/actor/correlationId/payload; events are idempotently applied by `(runId,sequence)` and replayable after reconnect. See `source_material/premium/upgrade/data/event_catalog.json` for 44 event proposals and `source_material/premium/upgrade/schemas/operation-event.schema.json` for the illustrative validation contract.

## Model provider protocols are not interchangeable

Create explicit `IModelProvider`, `IConversationProvider`, `IAgentAdapter`, `IToolTransport`, `IModelCapabilityProbe`, `IRunScheduler`, `IApprovalService`, `IEventJournal`, `IArtifactStore`, `IWorkspaceProvider` boundaries in UI-free C#. Underlying model inference (Ollama/llama.cpp/OpenAI-compatible endpoints) is not equivalent to a full tool-using agent. Use ACP for compatible agent connections, official Codex App Server API for supported Codex operations, MCP for tools/resources, and provider-specific adapters for hosted models, all behind capability detection and version pinning. Refuse unsupported calls with reason and setup path.

## Routing

`Cloud` runs selected cloud model; `Local` uses approved loopback runtime only; `Auto` chooses according to explicit user-set task, cost, privacy, capability, context and resource policy. `Sticky local` must not silently switch back to cloud. Fallback only on classified failures (quota/limits/unavailability, not arbitrary errors), and only with provider transition policy and privacy consent. Preserve conversation semantics, provenance and user-confirmed attachment transfer. Never silently transmit repository content to a newly selected cloud provider. Label every message/run with **actual** model/provider and endpoint privacy location; do not call a non-loopback endpoint 'local'.

## Parallel execution / worktrees

Task scheduler bounds agent concurrency and GPU memory; independent mutating agents use separate Git worktrees with distinct worktree IDs. Review must show proposed merge/cherry-pick action, conflicts and test evidence before mutation. Never combine changes from multiple agents via blind writes to one project tree. Failed workers retain inspectable logs, cancellation and cleanup policy; cleanup never deletes user files or unknown worktrees.

## Tool safety

- Exact path authorization per workspace and action, symlink/path traversal checks, max read/write sizes, allowlist of commands/providers where possible, timeouts and output bounds.
- Command proposal displays actual executable, argv/cwd/env policy/network requirement and effect scope; read-only commands may use approved automation policies; destructive actions require explicit review.
- Terminal PTY is genuine and OS-specific. UI `Stop` must target exact run/process group and await outcome.
- Tool results include real exit code, stdout/stderr bounds, affected files, original request and correlation ID; never fabricate successful tests.
- Credential use is indirect through OS secret services; never include tokens in task logs, diffs or cloud prompts without necessary explicit consent.

## Workflow design

A workflow is a versioned, persisted DAG with typed nodes (`agent`, `tool`, `review`, `condition`, `parallel`, `wait`, `notify`, `artifact`) and guarded edges. Explicitly define: node input/output schema, retries only for idempotent actions, timeout/cancellation, manual checkpoint, dependency wiring, cycle rejection, scheduling permission, pause/resume semantics, parent/child relationships, and policy inheritance. All node progress must be visible in one timeline. `source_material/premium/upgrade/data/workflow_templates.json` has 10 **illustrative, non-executable** templates; validate before turning them into runnable automation.

## Crash and recovery contracts

Persist task intent before dispatch; every irreversible tool action records preflight and completion evidence; on restart reconcile process liveness and effects before retrying. Do not replay a git commit/push, shell command, payment or remote action because its acknowledgment was lost. When uncertain, present `Unknown outcome—inspect manually` with evidence and safe options. Journal compaction never removes unresolved approvals, postmortem diagnostics or rollback information. Support schema versions and reversible migrations of app data.
