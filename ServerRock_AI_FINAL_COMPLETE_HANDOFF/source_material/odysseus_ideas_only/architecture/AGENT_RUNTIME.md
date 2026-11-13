# Engineering spec: Agent Runtime, persistent execution and typed tools

## Avoid the current one-shot limitations

Existing path: `MainPage.Chat.SendClick` creates message context, sends one streaming provider call, then `AttachWorkspaceProposals` parses XML-style string payload into proposed changes/commands; a review dialog manually applies each. This is useful supervised functionality and should be retained as Simple Coding Mode. Build Goal Mode beside it, never silently overwrite it.

## Proposed services (new C# namespaces `SkaiDesktop.Core.Agent`)

- `IAgentRunCoordinator`: creates immutable user goal, project selection, run policy and run id; stores durable run, cancel and resume.
- `ITaskPlanner`: typed tasks and dependencies, success criteria, risk flags, verification steps. Initial plan is mutable only through logged revisions.
- `ICapabilityResolver`: calculates model capability and user/tool eligibility (read-only, workspace write, shell, network, Git, plugin, external account), separate from user authorization.
- `IToolRegistry`: `ToolDefinition` with stable ID/version/JSON schema, effect class, permission scopes, output size, timeout, reentrancy, executable binding.
- `IToolDispatcher`: validates exact schema + effect permission + bound directory/resource identity; executes via policy-enforcing process/service; returns structured `ToolResult`.
- `IWorkspaceIndexer`: enumerate allowed files, ignore VCS/build dirs, search names/content/symbols, open bounded passages and cite relative paths.
- `IModelRouter`: choose provider/model per phase using model/tool capability, privacy, context, quota, cost and measured latency. Fallback only if policy and consent permit it.
- `IExecutionJournal`: SQLite WAL with migrations, run ledger, step logs, tool input/output hash, approvals, checkpoints and idempotency keys.
- `IEvaluator`: compile/test/lint static+integration evidence, evaluate requirements and verify diff. Don't use model self-affirmations as test results.
- `IRecoveryCoordinator`: after process failure, mark interrupted effect as unknown; never duplicate non-idempotent operations blindly; request reconciliation.
- `INotificationSink`: progress, approval and terminal results to UI; events scoped by run id and cancellation token.

## State machine

`Created → Inspecting → Planning → AwaitingApproval? → Executing → Verifying → (Repairing → Executing)* → Completed | Failed | Canceled | Interrupted`.

Every transition persists a versioned event with monotonic step sequence, UTC time, owner, project identity, selected model, policy snapshot, and evidence references. Use optimistic concurrency on event write to prevent duplicate callbacks. A command's `Started` event does not imply completion; record separate dispatched/observed result, and reconcile OS process identity after restart.

## Typed tool invocation example (illustrative, not shipped)

```json
{"tool":"workspace.read_file","version":"1","arguments":{"workspace_id":"abc","path":"src/Example.cs","start_line":1,"end_line":160},"authority_id":"read-only-grant","idempotency_key":"run-4-step-1"}
```

```json
{"tool":"workspace.apply_patch","arguments":{"path":"src/Example.cs","base_sha256":"...","unified_diff":"..."},"authority_id":"exact-path-write-grant","idempotency_key":"run-4-step-7"}
```

Tool arguments and results are untrusted data. Model text must not be parsed as executable instructions unless a validated structured tool schema and authorization accompany it. Validate exact paths with symlink/junction policy, root IDs, expected content hash, file size and encoding. Restrict shell capability to separately granted command in isolated environment, not a generic tool accessible because search results suggested it.

## Repo exploration

Provide search-first: `workspace.list_tree`, `workspace.search_text`, `workspace.search_symbols`, `workspace.read_file`, `workspace.git_status`, `workspace.git_diff`, `workspace.apply_patch`, `workspace.test`, `workspace.build`. Prefer precise incremental reads to inserting whole files into system prompt. Index invalidation listens to file changes and Git branch changes; use bounded incremental rebuild.

## Agent autonomy controls

- `Ask each action`: current behavior, default for arbitrary shell/write operations.
- `Approve bounded work`: exact workspace and operation classes with budget/time limits; persistent audit view.
- `Read only`: inspect and propose without mutation.
- `Unattended`: only when user explicitly opts into a contained execution environment and states limits. High-impact external actions remain granular.

Plan → Act → Verify loop includes per-run time, token, iteration, command-output and monetary ceilings. A failed tool response never authorizes broadening capabilities. Use progress and artifacts that remain viewable even if page is closed.

## Verification harness

- Fixture repo: tiny .NET library with a deliberate failing test; agent must find root cause, apply patch, run test, preserve unrelated user changes.
- Failure: model hangs; provider 429; tool times out; agent cancels; app crashes after patch but before verification; Git branch changes between read/write.
- Safety: malicious README contains fake system instructions; untrusted skill contains command escalation; shell tries to access home directory; symlink points outside selected workspace; network disabled.
- Evidence: every Completed run links verified exit codes, test logs, before/after diff and any unresolved warnings.

Odysseus reference: `docs/AGENT_TURN_CONTRACT.md`, `src/turn_contract.py`, `services/skills/agent_runner.py`, `docs/BACKGROUND_TOOL_JOBS.md`. Borrow domain boundaries, not Python code; Odysseus itself documents residual trust issues.
