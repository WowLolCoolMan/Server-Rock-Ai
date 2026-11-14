// GitHub device flow, credential storage (Windows only), repository parsing, git status
// parsing, and API client limits (GitHubDeviceFlow, GitHubCredentialStore, GitHubApiClient,
// GitHubWorkspaceService).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Net;
using System.Text;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class GitHubTests
{
    public static async Task RunAsync()
    {
        var githubNow = DateTimeOffset.Parse("2026-11-03T12:00:00Z");
        var githubResponses = new Queue<string>([
            "{\"device_code\":\"device-code-long-value-0123456789\",\"user_code\":\"ABCD-EFGH\",\"verification_uri\":\"https://github.com/login/device\",\"expires_in\":900,\"interval\":5}",
            "{\"error\":\"authorization_pending\"}",
            "{\"error\":\"slow_down\"}",
            "{\"access_token\":\"gho_12345678901234567890\",\"refresh_token\":\"ghr_12345678901234567890\",\"expires_in\":28800,\"refresh_token_expires_in\":15897600}"
        ]);
        var githubRequestCount = 0;
        var githubFlow = new GitHubDeviceFlow(new HttpClient(new StubHandler((request, _) =>
        {
            githubRequestCount++;
            Check(request.Headers.Accept.Any(item => item.MediaType == "application/json"), "GitHub device flow requests JSON responses");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(githubResponses.Dequeue(), Encoding.UTF8, "application/json") });
        })), () => githubNow);
        var githubAuthorization = await githubFlow.BeginAsync("Iv1.example-client-id");
        Check(githubAuthorization.UserCode == "ABCD-EFGH" && githubAuthorization.VerificationUri.AbsoluteUri == "https://github.com/login/device", "GitHub device flow returns a verified browser address and user code");
        Check(githubAuthorization.IntervalSeconds == 5 && githubAuthorization.ExpiresAt == githubNow.AddMinutes(15), "GitHub device flow honors expiry and initial polling interval");
        var earlyPoll = await githubFlow.PollAsync(githubAuthorization);
        Check(earlyPoll.IsPending && githubRequestCount == 1, "GitHub device flow never polls before its required interval");
        githubNow = githubAuthorization.NextPollAt;
        var pendingPoll = await githubFlow.PollAsync(githubAuthorization);
        Check(pendingPoll.IsPending && githubAuthorization.NextPollAt == githubNow.AddSeconds(5), "GitHub authorization_pending schedules the next poll at the supplied interval");
        githubNow = githubAuthorization.NextPollAt;
        var slowedPoll = await githubFlow.PollAsync(githubAuthorization);
        Check(slowedPoll.IsPending && githubAuthorization.IntervalSeconds == 10, "GitHub slow_down increases the polling interval by five seconds");
        githubNow = githubAuthorization.NextPollAt;
        var completedPoll = await githubFlow.PollAsync(githubAuthorization);
        Check(!completedPoll.IsPending && completedPoll.Credential?.AccessToken == "gho_12345678901234567890" && completedPoll.Credential.RefreshToken == "ghr_12345678901234567890", "GitHub device flow returns access and refresh credentials only after authorization");
        Check(completedPoll.Credential?.AccessTokenExpiresAt == githubNow.AddHours(8) && completedPoll.Credential.RefreshTokenExpiresAt == githubNow.AddDays(184), "GitHub token expiries are retained from the authorization response");
        var refreshBody = "";
        var refreshedFlow = new GitHubDeviceFlow(new HttpClient(new StubHandler(async (request, _) =>
        {
            refreshBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"access_token\":\"gho_refreshed-access-token-12345\",\"refresh_token\":\"ghr_refreshed-refresh-token-12345\",\"expires_in\":28800,\"refresh_token_expires_in\":15897600}", Encoding.UTF8, "application/json") };
        })), () => githubNow);
        var refreshedCredential = await refreshedFlow.RefreshAsync("Iv1.example-client-id", completedPoll.Credential!);
        Check(refreshBody.Contains("grant_type=refresh_token", StringComparison.Ordinal) && refreshBody.Contains("ghr_12345678901234567890", StringComparison.Ordinal), "GitHub refresh requests use the required refresh_token grant");
        Check(refreshedCredential.AccessToken == "gho_refreshed-access-token-12345" && refreshedCredential.RefreshToken == "ghr_refreshed-refresh-token-12345" && refreshedCredential.AccessTokenExpiresAt == githubNow.AddHours(8), "GitHub refresh rotates and persists expiring credentials");
        try
        {
            await refreshedFlow.RefreshAsync("bad/client/id", completedPoll.Credential!);
            throw new Exception("Expected malformed GitHub refresh client IDs to be rejected.");
        }
        catch (InvalidOperationException)
        {
            Check(true, "GitHub refresh validates the client ID before network access");
        }
        try
        {
            await githubFlow.BeginAsync("bad/client/id");
            throw new Exception("Expected an invalid GitHub client ID to be rejected.");
        }
        catch (ArgumentException)
        {
            Check(githubRequestCount == 4, "GitHub client ID validation stops malformed IDs before any network request");
        }
        if (OperatingSystem.IsWindows())
        {
            var credentialPath = Path.Combine(Path.GetTempPath(), "server-rock-github-credential-" + Guid.NewGuid().ToString("N"), "github-token.bin");
            var credentialStore = new GitHubCredentialStore(credentialPath);
            await credentialStore.WriteAsync("gho_test-access-token-value");
            Check(File.Exists(credentialPath) && !File.ReadAllText(credentialPath).Contains("gho_test-access-token-value", StringComparison.Ordinal), "Windows GitHub credentials are stored as DPAPI ciphertext");
            Check(await credentialStore.ReadAsync() == "gho_test-access-token-value", "Windows GitHub credentials decrypt for the same user");
            var credentialBeforeRejectedWrite = File.ReadAllBytes(credentialPath);
            try
            {
                await credentialStore.WriteAsync("gho_test-access-token\nwith-control-character");
                throw new Exception("Expected an invalid GitHub credential to be rejected.");
            }
            catch (ArgumentException)
            {
                Check(File.ReadAllBytes(credentialPath).SequenceEqual(credentialBeforeRejectedWrite) && await credentialStore.ReadAsync() == "gho_test-access-token-value",
                    "invalid GitHub credential writes preserve the previous protected credential");
            }
            await credentialStore.DeleteAsync();
            Check(!File.Exists(credentialPath), "Windows GitHub credential disconnect removes the protected file");
            Directory.Delete(Path.GetDirectoryName(credentialPath)!, recursive: true);
        }
        Check(GitHubApiClient.ParseRepositoryReference("owner/project") == "owner/project", "GitHub repository input accepts owner/name references");
        Check(GitHubApiClient.ParseRepositoryReference("https://github.com/owner/project.git") == "owner/project", "GitHub repository input safely normalizes HTTPS clone URLs");
        var parsedGitStatus = GitHubWorkspaceService.ParseStatus("# branch.oid 0123456789abcdef\n# branch.head main\n# branch.upstream origin/main\n# branch.ab +2 -1\n1 .M N... 100644 100644 100644 00000000 11111111 path with spaces.txt\n? new file.md\n", "owner/project");
        Check(parsedGitStatus.Branch == "main" && parsedGitStatus.CommitsAhead == 2 && parsedGitStatus.CommitsBehind == 1, "GitHub workspace parses branch status and ahead/behind counts");
        Check(parsedGitStatus.CommitsBehind > 0, "GitHub push preflight blocks a branch that is behind after remote refresh");
        Check(parsedGitStatus.Changes.Count == 2 && parsedGitStatus.Changes[0].Path == "path with spaces.txt" && parsedGitStatus.Changes[1].Status == "??", "GitHub workspace preserves modified and untracked paths for review");
        Check(GitHubWorkspaceService.IsConflictStatus("UU") && GitHubWorkspaceService.IsConflictStatus("AA") && !GitHubWorkspaceService.IsConflictStatus(".M") &&
              GitHubWorkspaceService.HasConflicts([new GitWorkspaceFileChange("UU", "src/conflict.cs")]), "GitHub workspace identifies unmerged conflict paths for recovery");
        var parsedRenameStatus = GitHubWorkspaceService.ParseStatus("# branch.head main\n2 R. N... 100644 100644 100644 00000000 11111111 R100 safe.txt\t.env.production\n", "owner/project");
        Check(parsedRenameStatus.Changes.Count == 1 && parsedRenameStatus.Changes[0].Path == "safe.txt" && parsedRenameStatus.Changes[0].OriginalPath == ".env.production", "GitHub workspace preserves both sides of a rename for review");
        var unsafeRenamePaths = GitHubWorkspaceService.FindUnsafeChangePaths(parsedRenameStatus.Changes);
        Check(unsafeRenamePaths.Contains(".env.production"), "GitHub commit safety checks the source path of renames");
        foreach (var unsafeReference in new[] { "https://evil.example/owner/project", "https://user:pass@github.com/owner/project", "owner/../settings", "owner/project?tab=code" })
        {
            try
            {
                GitHubApiClient.ParseRepositoryReference(unsafeReference);
                throw new Exception("Expected unsafe GitHub repository reference to be rejected.");
            }
            catch (ArgumentException) { }
        }
        Check(true, "GitHub repository input rejects alternate hosts, credentials, traversal, and URL query data");
        var unsafeGitPaths = GitHubWorkspaceService.FindUnsafeChangePaths([
            new GitWorkspaceFileChange("??", ".env.production"),
            new GitWorkspaceFileChange("??", "config/credentials.json"),
            new GitWorkspaceFileChange("??", "certs/client.p12"),
            new GitWorkspaceFileChange("??", ".env.example"),
            new GitWorkspaceFileChange("??", "src/app.cs")
        ]);
        Check(unsafeGitPaths.Count == 3 && unsafeGitPaths.Contains(".env.production") && unsafeGitPaths.Contains("config/credentials.json") && unsafeGitPaths.Contains("certs/client.p12"), "GitHub commit safety blocks secret-like paths");
        const string repositoryJson = "{\"id\":42,\"full_name\":\"owner/project\",\"name\":\"project\",\"private\":true,\"default_branch\":\"main\",\"description\":\"Demo\",\"updated_at\":\"2026-11-03T12:00:00Z\",\"clone_url\":\"https://github.com/owner/project.git\"}";
        var githubApi = new GitHubApiClient(new HttpClient(new StubHandler((request, _) =>
        {
            Check(request.RequestUri?.Host == "api.github.com", "GitHub API calls stay on the official API host");
            Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "test-access-token", "GitHub API calls use the signed-in user's bearer token");
            Check(request.Headers.Contains("X-GitHub-Api-Version"), "GitHub API calls declare a stable API version");
            var responseBody = request.RequestUri?.AbsolutePath == "/user/repos"
                ? "[" + repositoryJson + "]"
                : repositoryJson;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") });
        })));
        var resolvedRepository = await githubApi.ResolveRepositoryAsync("test-access-token", "owner/project");
        Check(resolvedRepository.FullName == "owner/project" && resolvedRepository.IsPrivate && resolvedRepository.DefaultBranch == "main", "GitHub API resolves repository identity and clone metadata");
        var listedRepositories = await githubApi.ListRepositoriesAsync("test-access-token");
        Check(listedRepositories.Count == 1 && listedRepositories[0].Id == 42 && listedRepositories[0].CloneUrl.AbsoluteUri == "https://github.com/owner/project.git", "GitHub API lists authorized repositories with safe HTTPS clone URLs");
        var accountApi = new GitHubApiClient(new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"login\":\"octocat\",\"name\":\"Octo\",\"avatar_url\":\"https://github.com/" + new string('a', 2050) + "\"}", Encoding.UTF8, "application/json")
            }))));
        var account = await accountApi.GetAccountAsync("test-access-token");
        Check(account.AvatarUrl is null, "GitHub API drops oversized avatar URLs at the trust boundary");
        var oversizedBranchApi = new GitHubApiClient(new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(repositoryJson.Replace("\"main\"", "\"" + new string('b', 257) + "\"", StringComparison.Ordinal), Encoding.UTF8, "application/json")
            }))));
        try
        {
            await oversizedBranchApi.ResolveRepositoryAsync("test-access-token", "owner/project");
            throw new Exception("Expected an oversized GitHub branch name to be rejected.");
        }
        catch (InvalidOperationException)
        {
            Check(true, "GitHub API bounds repository branch identifiers");
        }

        var worktreeFixture = Path.Combine(Path.GetTempPath(), "server-rock-worktree-" + Guid.NewGuid().ToString("N"));
        var isolatedPath = Path.Combine(Path.GetDirectoryName(worktreeFixture)!, Path.GetFileName(worktreeFixture) + "-isolated");
        try
        {
            Directory.CreateDirectory(worktreeFixture);
            RunGit(worktreeFixture, "init", "-b", "main");
            RunGit(worktreeFixture, "config", "user.email", "tests@example.invalid");
            RunGit(worktreeFixture, "config", "user.name", "Native Tests");
            File.WriteAllText(Path.Combine(worktreeFixture, "README.md"), "fixture\n");
            RunGit(worktreeFixture, "add", "README.md");
            RunGit(worktreeFixture, "commit", "-m", "initial fixture");

            var coordinator = new GitWorktreeCoordinator();
            var lease = await coordinator.CreateAsync(worktreeFixture, isolatedPath, "codex/isolated-fixture");
            Check(Directory.Exists(lease.WorktreePath) && lease.Branch == "codex/isolated-fixture" && File.Exists(Path.Combine(lease.WorktreePath, "README.md")), "Git worktree coordinator creates a sibling isolated target");
            Check(RunGitOutput(lease.WorktreePath, "branch", "--show-current").Trim() == lease.Branch, "Git worktree coordinator checks out the requested branch");

            File.WriteAllText(Path.Combine(lease.WorktreePath, "pending.txt"), "review me\n");
            try
            {
                await coordinator.ReleaseCleanAsync(worktreeFixture, isolatedPath);
                throw new Exception("Expected dirty worktree release to be rejected.");
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("uncommitted changes", StringComparison.OrdinalIgnoreCase))
            {
                Check(true, "Git worktree coordinator refuses to release dirty targets");
            }
            File.Delete(Path.Combine(lease.WorktreePath, "pending.txt"));
            await coordinator.ReleaseCleanAsync(worktreeFixture, isolatedPath);
            Check(!Directory.Exists(isolatedPath), "Git worktree coordinator releases only a clean registered target");

            try
            {
                await coordinator.CreateAsync(worktreeFixture, Path.Combine(worktreeFixture, "nested"), "codex/nested");
                throw new Exception("Expected nested worktree target to be rejected.");
            }
            catch (ArgumentException)
            {
                Check(true, "Git worktree coordinator rejects repository-nested targets");
            }
        }
        finally
        {
            if (Directory.Exists(isolatedPath))
            {
                try { RunGit(worktreeFixture, "worktree", "remove", "--force", "--", isolatedPath); } catch { }
            }
            if (Directory.Exists(worktreeFixture)) DeleteDirectoryTree(worktreeFixture);
            if (Directory.Exists(isolatedPath)) DeleteDirectoryTree(isolatedPath);
        }
    }
}
