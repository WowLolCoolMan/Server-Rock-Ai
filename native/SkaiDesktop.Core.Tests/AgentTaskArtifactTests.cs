using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class AgentTaskArtifactTests
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "server-rock-agent-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new AgentTaskArtifactStore(Path.Combine(root, "agent-tasks.json"));
            var first = store.Create("Inspect provider routing", "Review capability preflight evidence.", "researcher", "run-1");
            var projectAttached = store.Create("Inspect selected project", "Review the bounded source context.", projectId: "project-1");
            var second = store.Create("Verify the routing change", "Check the result against the first task.", "verifier", dependsOn: [first.Id]);
            Check(first.Status == AgentTaskStatuses.Proposed && second.DependsOn.SequenceEqual([first.Id]), "agent task artifacts preserve roles and dependencies");
            Check(projectAttached.ProjectId == "project-1" && store.Load().Single(task => task.Id == projectAttached.Id).ProjectId == "project-1", "agent task artifacts persist an optional project context identity");
            var approvalStore = new AgentTaskArtifactStore(Path.Combine(root, "approval-tasks.json"));
            var approvalPlan = approvalStore.Create("Approve bounded review", "Review the approval boundary before local execution.");
            var pendingApproval = approvalStore.RequestApproval(approvalPlan.Id);
            Check(pendingApproval.Status == AgentTaskStatuses.AwaitingApproval && approvalStore.GetNextReady() is null, "agent task approval blocks execution before a decision");
            var approvedPlan = approvalStore.Approve(approvalPlan.Id, "test-user");
            Check(approvedPlan.Status == AgentTaskStatuses.Queued && approvedPlan.ResultSummary?.Contains("Approved by test-user", StringComparison.Ordinal) == true, "agent task approval releases a task to the explicit queue");
            var deniedPlan = approvalStore.Create("Deny bounded review", "Review a denied approval path.");
            approvalStore.RequestApproval(deniedPlan.Id);
            var denied = approvalStore.Deny(deniedPlan.Id, "Needs a narrower scope.");
            Check(denied.Status == AgentTaskStatuses.Cancelled && denied.ResultSummary?.Contains("Needs a narrower scope", StringComparison.Ordinal) == true, "agent task denial is durable and never executes");
            Check(store.GetNextReady() is null, "agent task readiness preview ignores proposed tasks");
            Check(store.TryClaimReady() is null, "queued dependent agent task is not claimable before its prerequisite is queued and completed");
            store.Queue(first.Id);
            var preview = store.GetNextReady();
            Check(preview?.Id == first.Id && store.Load().Single(task => task.Id == first.Id).Status == AgentTaskStatuses.Queued, "agent task readiness preview is dependency-aware and non-mutating");
            var claimedFirst = store.TryClaimReady();
            Check(claimedFirst?.Id == first.Id && claimedFirst.Status == AgentTaskStatuses.Running, "queued root agent task can be claimed without dispatching external work");
            store.Complete(first.Id, "Routing evidence reviewed.", ["local-review"]);
            store.Queue(second.Id);
            var claimedSecond = store.TryClaimReady();
            Check(claimedSecond?.Id == second.Id, "dependent agent task becomes claimable only after its prerequisite completes");
            store.Fail(second.Id, "Verifier found an unresolved compatibility gap.");
            var cancelled = store.Create("Cancel before execution", "Review whether a queued plan can be withdrawn.");
            store.Queue(cancelled.Id);
            var cancelledPlan = store.CancelPlanned(cancelled.Id, "User withdrew the plan before execution.");
            Check(cancelledPlan.Status == AgentTaskStatuses.Cancelled && store.GetNextReady() is null, "proposed agent task plans can be cancelled before execution");
            var interruptedPlan = store.Create("Retry interrupted review", "Review an interrupted task before retrying.");
            store.Queue(interruptedPlan.Id);
            Check(store.TryClaimReady()?.Status == AgentTaskStatuses.Running, "retry fixture claims an agent task before simulated restart");
            store.ReconcileAfterRestart();
            var retriedPlan = store.RetryBlocked(interruptedPlan.Id);
            Check(retriedPlan.Status == AgentTaskStatuses.Queued && retriedPlan.ResultSummary?.Contains("Retry approved", StringComparison.Ordinal) == true,
                "blocked agent tasks can be explicitly re-queued without starting execution");
            var reloaded = new AgentTaskArtifactStore(Path.Combine(root, "agent-tasks.json")).Load();
            Check(reloaded.Any(task => task.Id == first.Id && task.Status == AgentTaskStatuses.Completed && task.ArtifactLabels.Contains("local-review")), "completed agent task artifacts persist bounded result labels");
            Check(reloaded.Any(task => task.Id == second.Id && task.Status == AgentTaskStatuses.Failed), "failed agent task artifacts persist their terminal status");
            Check(store.Search("verifier").Count == 1, "agent task search matches role metadata");
            var executor = new AgentTaskExecutor(store);
            var explicitClaimPlan = store.Create("Explicitly claim review", "Claim one selected task for a local run.");
            store.Queue(explicitClaimPlan.Id);
            var explicitlyClaimed = store.TryClaim(explicitClaimPlan.Id);
            Check(explicitlyClaimed.Status == AgentTaskStatuses.Running, "a specific queued agent task can be claimed for an explicit local run");
            store.Fail(explicitClaimPlan.Id, "Reset explicit claim fixture.");
            var completedPlan = store.Create("Execute bounded review", "Run an injected review handler.");
            store.Queue(completedPlan.Id);
            var completed = await executor.ExecuteNextAsync((task, _) => Task.FromResult(new AgentTaskExecutionResult($"Completed {task.Title}.", ["handler-result"])));
            Check(completed?.Status == AgentTaskStatuses.Completed && completed.ArtifactLabels.Contains("handler-result"), "agent task executor durably completes an injected handler");
            var failedPlan = store.Create("Fail bounded review", "Run a failing injected handler.");
            store.Queue(failedPlan.Id);
            var failed = await executor.ExecuteNextAsync((_, _) => throw new InvalidOperationException("handler failure"));
            Check(failed?.Status == AgentTaskStatuses.Failed && failed.ResultSummary == "handler failure", "agent task executor durably records handler failure");
            var cancelledExecutionPlan = store.Create("Cancel bounded review", "Run a cancelled injected handler.");
            store.Queue(cancelledExecutionPlan.Id);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelledExecution = await executor.ExecuteNextAsync(async (_, token) =>
            {
                await Task.Delay(1, token);
                return new AgentTaskExecutionResult("unreachable");
            }, cancellation.Token);
            Check(cancelledExecution?.Status == AgentTaskStatuses.Cancelled, "agent task executor durably records cancellation");

            var isolatedRepository = Path.Combine(root, "agent-repo");
            Directory.CreateDirectory(isolatedRepository);
            RunGit(isolatedRepository, "init", "-b", "main");
            RunGit(isolatedRepository, "config", "user.email", "tests@example.invalid");
            RunGit(isolatedRepository, "config", "user.name", "Native Tests");
            File.WriteAllText(Path.Combine(isolatedRepository, "README.md"), "isolated fixture\n");
            RunGit(isolatedRepository, "add", "README.md");
            RunGit(isolatedRepository, "commit", "-m", "initial agent fixture");
            var isolatedPlan = store.Create("Execute in isolated worktree", "Inspect the isolated project target.", "specialist");
            store.Queue(isolatedPlan.Id);
            var worktreeExecutor = new AgentTaskExecutor(store);
            var coordinator = new GitWorktreeCoordinator();
            var worktreeParent = Path.Combine(root, "worktrees");
            var isolatedResult = await worktreeExecutor.ExecuteNextInWorktreeAsync(coordinator, isolatedRepository, worktreeParent, async (task, lease, _) =>
            {
                Check(task.WorktreePath == lease.WorktreePath && File.Exists(Path.Combine(lease.WorktreePath, "README.md")), "agent executor injects a real isolated worktree into the handler");
                Check(RunGitOutput(lease.WorktreePath, "branch", "--show-current").Trim() == lease.Branch, "agent executor persists the isolated task branch");
                await Task.CompletedTask;
                return new AgentTaskExecutionResult("Isolated review completed.", ["isolated-review"]);
            });
            Check(isolatedResult?.Status == AgentTaskStatuses.Completed && isolatedResult.WorktreePath is not null && isolatedResult.WorktreeRepositoryRoot == isolatedRepository, "agent executor durably retains the worktree lease after handler completion");
            var releasedResult = await worktreeExecutor.ReleaseTaskWorktreeAsync(isolatedResult!.Id, coordinator);
            Check(releasedResult.WorktreePath is null && !Directory.Exists(Path.Combine(worktreeParent, $"agent-task-{isolatedResult.Id}")), "agent executor releases a retained clean worktree explicitly");
            try
            {
                store.Create("bad", "missing dependency", dependsOn: ["missing"]);
                throw new Exception("Expected an unknown dependency to be rejected.");
            }
            catch (ArgumentException)
            {
                Check(true, "agent task artifacts reject dependencies outside the local task set");
            }
        }
        finally { DeleteDirectoryTree(root); }
    }
}
