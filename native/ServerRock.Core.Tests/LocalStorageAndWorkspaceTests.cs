// Everything that writes to a temp app-data folder: settings, conversations, projects,
// workspace file context, git review, command runner, terminal, file editor, diff, and undo
// history. These share one temp folder, so they stay in one method.
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class LocalStorageAndWorkspaceTests
{
    public static async Task RunAsync()
    {
        // Same fixture file that ConversationArchiveTests reads.
        var electronExportPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "electron-localstorage-export.json");
        var settingsDirectory = Path.Combine(Path.GetTempPath(), "server-rock-core-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            var settingsStore = new JsonSettingsStore(Path.Combine(settingsDirectory, "settings.json"));
            settingsStore.Save(new AppSettings { Endpoint = "http://127.0.0.1:1234/v1", Model = "persisted-model", ModelProvider = "LM Studio", PinnedModels = ["persisted-model", "second-model"], PinnedModelConnections = [new("LM Studio", "persisted-model", "http://127.0.0.1:1234/v1"), new("Ollama", "llama3.2", "http://127.0.0.1:11434/v1")], LocalModelPerformance = new() { ["http://127.0.0.1:1234/v1\npersisted-model"] = new(3, 28.5, 410, DateTimeOffset.Parse("2026-11-03T12:00:00Z")) }, VisionApiFormat = "ollama", VisionCapabilityOverride = "supported", Temperature = 1.2, EffortMode = "high", WebSearchEnabled = true, SmartPromptRewritingEnabled = true, VoiceOutputEnabled = true, AccentColor = "violet", ThemeMode = "dark", BackdropPreset = "deep-space", ReduceMotion = true, InterfaceDensity = "compact", HostLabel = "Test workstation", RequireLocalModelEndpoints = true, ComfyEndpoint = "http://127.0.0.1:8188", ImageWorkflow = "{\"workflow\":\"{{prompt}}\"}", GitHubAppClientId = "Iv1.example-client-id", ChatFolders = ["Research", "Writing"], PromptSnippets = [new PromptSnippet { Title = "Review", Text = "Review this carefully." }], SavedPersonas = [new SavedPersona { Id = "reviewer", Name = "Reviewer", Instructions = "Find defects." }], ActivePersonaId = "reviewer", WorkspaceRouteHistory = ["chat", "coding"] });
            var quicklinkSettings = settingsStore.Load();
            quicklinkSettings.Quicklinks = [new Quicklink { Title = "Project docs", Url = "https://example.com/docs" }];
            quicklinkSettings.DeveloperMode = true;
            settingsStore.Save(quicklinkSettings);
            var ratioSettings = settingsStore.Load();
            ratioSettings.SplitChatRatios["Chat:project-1"] = 62.5;
            settingsStore.Save(ratioSettings);
            var reloaded = settingsStore.Load();
            Check(reloaded.Model == "persisted-model" && Math.Abs(reloaded.Temperature - 1.2) < 0.001 && reloaded.EffortMode == "high" && reloaded.WebSearchEnabled && reloaded.SmartPromptRewritingEnabled, "settings and effort mode survive save and reload");
            Check(reloaded.AccentColor == "violet" && reloaded.ThemeMode == "dark" && reloaded.BackdropPreset == "deep-space" && reloaded.ReduceMotion && reloaded.InterfaceDensity == "compact" && reloaded.HostLabel == "Test workstation" && reloaded.RequireLocalModelEndpoints && reloaded.PromptSnippets is [{ Title: "Review", Text: "Review this carefully." }] && reloaded.SavedPersonas is [{ Id: "reviewer", Name: "Reviewer", Instructions: "Find defects." }] && reloaded.ActivePersonaId == "reviewer", "appearance, backdrop, snippets, personas, and host settings survive save and reload");
            Check(reloaded.Quicklinks is [{ Title: "Project docs", Url: "https://example.com/docs" }], "quicklinks survive save and reload");
            Check(reloaded.DeveloperMode, "developer mode preference survives save and reload");
            Check(reloaded.VoiceOutputEnabled, "voice output preference survives save and reload");
            Check(reloaded.PinnedModels.SequenceEqual(["persisted-model", "second-model"]), "pinned model hotswapper selection survives settings save and reload");
            Check(reloaded.ModelProvider == "LM Studio" && reloaded.PinnedModelConnections.Count == 2 && reloaded.PinnedModelConnections[1].Endpoint.EndsWith(":11434/v1", StringComparison.Ordinal), "pinned model endpoints survive settings save and reload");
            Check(reloaded.LocalModelPerformance["http://127.0.0.1:1234/v1\npersisted-model"].Samples == 3, "local model benchmark measurements survive settings save and reload");
            Check(reloaded.VisionApiFormat == "ollama", "vision API format selection survives save and reload");
            Check(reloaded.VisionCapabilityOverride == "supported", "vision capability override survives save and reload");
            Check(reloaded.ComfyEndpoint == "http://127.0.0.1:8188" && reloaded.ImageWorkflow.Contains("{{prompt}}", StringComparison.Ordinal), "local ComfyUI image workflow settings survive save and reload");
            Check(reloaded.GitHubAppClientId == "Iv1.example-client-id", "GitHub App client ID persists as a non-secret preference");
            Check(reloaded.ChatFolders.SequenceEqual(["Research", "Writing"]), "chat folders survive settings save and reload");
            Check(reloaded.WorkspaceRouteHistory.SequenceEqual(["chat", "coding"]), "workspace route history survives settings save and reload");
            Check(Math.Abs(reloaded.SplitChatRatios["Chat:project-1"] - 62.5) < 0.001, "split chat ratio survives settings save and reload");
            Check(EffortModePolicy.Normalize("ULTRA") == "ultra" && EffortModePolicy.MaxTokens("light") == 512 && EffortModePolicy.MaxTokens("standard") == 1024 && EffortModePolicy.MaxTokens("high") == 2048 && EffortModePolicy.MaxTokens("ultra") == 4096, "effort modes normalize and map to bounded output budgets");

            var corruptSettingsPath = Path.Combine(settingsDirectory, "corrupt-settings.json");
            File.WriteAllText(corruptSettingsPath, "{broken");
            var recoveredSettings = new JsonSettingsStore(corruptSettingsPath).Load();
            Check(recoveredSettings is not null && Directory.EnumerateFiles(settingsDirectory, "corrupt-settings.json.corrupt-*").Any(), "corrupt settings are preserved for recovery");
            var nullableSettingsPath = Path.Combine(settingsDirectory, "nullable-settings.json");
            File.WriteAllText(nullableSettingsPath, "{\"endpoint\":null,\"model\":null,\"modelProvider\":null,\"visionApiFormat\":null,\"comfyEndpoint\":null,\"imageWorkflow\":null,\"gitHubAppClientId\":null,\"pinnedModels\":null,\"pinnedModelConnections\":null,\"localModelPerformance\":null,\"chatFolders\":null}");
            var normalizedSettings = new JsonSettingsStore(nullableSettingsPath).Load();
            Check(normalizedSettings.Endpoint == "http://localhost:11434/v1" && normalizedSettings.Model == "llama3.2" && normalizedSettings.ModelProvider == "Ollama" &&
                  normalizedSettings.PinnedModels is not null && normalizedSettings.PinnedModelConnections is not null && normalizedSettings.LocalModelPerformance is not null && normalizedSettings.ChatFolders is not null && normalizedSettings.WorkspaceRouteHistory is not null,
                "nullable settings fields recover to usable native defaults");

            var conversationsPath = Path.Combine(settingsDirectory, "conversations.json");
            var conversationStore = new ConversationStore(conversationsPath);
            try
            {
                conversationStore.Save([new ChatSession { Messages = [new ChatMessage("user", new string('x', BoundedFileReader.MaxJsonBytes))] }]);
                throw new Exception("Expected oversized conversation persistence to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(true, "native persistence rejects oversized serialized conversations before writing");
            }
            conversationStore.Save([
                new ChatSession
                {
                    Id = "chat-one",
                    Title = "Persisted local conversation",
                    UpdatedAt = DateTimeOffset.UtcNow,
                    ProjectId = "project-demo",
                    ProjectName = "Demo project",
                    ProjectInstructions = "Use concise answers.",
                    Mode = "Coding Agent",
                    IncludeProjectFilesInChat = true,
                    FolderName = "Research",
                    Pinned = true,
                    Tags = ["research", "priority"],
                    Messages = [new ChatMessage("user", "Keep this on device", [new ChatAttachment("notes.md", "Attached note")]), new ChatMessage("assistant", "Saved locally.", Sources: [new ChatSource("Docs", "https://example.org", "A snippet")])]
                },
                new ChatSession
                {
                    Id = "chat-two",
                    Title = "Another conversation",
                    Messages = [new ChatMessage("user", "A separate thread", [new ChatAttachment("scene.jpg", Convert.ToBase64String([0xFF, 0xD8, 0xFF, 0xD9]), "image/jpeg")])]
                },
                new ChatSession
                {
                    Id = "electron-chat-001",
                    ImportSource = "electron-localstorage",
                    ImportSourceId = "electron-chat-001",
                    Title = "Ollama startup notes",
                    Archived = true,
                    ForkedFrom = "electron-chat-parent",
                    BaseMessages = [new ChatMessage("user", "Original question")],
                    Branches = [[new ChatMessage("user", "Alternative question"), new ChatMessage("assistant", "Alternative answer")]],
                    BranchIndex = 1,
                    Messages = [new ChatMessage("user", "How do I check whether Ollama is running?"), new ChatMessage("assistant", "Check the local Ollama service and request its model list.")]
                }
            ]);
            var restoredChats = conversationStore.Load();
            Check(restoredChats.Count == 3, "conversation store keeps multiple chats");
            Check(restoredChats[0].Messages[1].Content == "Saved locally.", "conversation messages survive save and reload");
            Check(restoredChats[0].Messages[0].Attachments is [{ FileName: "notes.md", Content: "Attached note" }], "text attachments survive conversation save and reload");
            Check(restoredChats[1].Messages[0].Attachments is [{ FileName: "scene.jpg", MediaType: "image/jpeg", Content: "/9j/2Q==" }], "image attachments survive local conversation save and reload");
            Check(restoredChats[0].Messages[1].Sources is [{ Title: "Docs", Url: "https://example.org", Snippet: "A snippet" }], "web source cards survive conversation save and reload");
            Check(restoredChats[0].ProjectId == "project-demo" && restoredChats[0].ProjectName == "Demo project" && restoredChats[0].ProjectInstructions == "Use concise answers.", "project context stays attached to its conversation");
            Check(restoredChats[0].Mode == "Coding Agent", "chat and coding-agent workspace identity survives save and reload");
            Check(restoredChats[0].IncludeProjectFilesInChat, "per-conversation project file context opt-in survives save and reload");
            Check(restoredChats[0].FolderName == "Research", "conversation folder assignment survives save and reload");
            Check(restoredChats[0].Pinned, "pinned conversation state survives save and reload");
            Check(restoredChats[0].Tags.SequenceEqual(["research", "priority"]), "conversation tags survive save and reload");
            Check(restoredChats[1].FolderName is null, "older conversations without folder metadata remain unfiled");
            Check(restoredChats[2].ImportSource == "electron-localstorage" && restoredChats[2].ImportSourceId == "electron-chat-001", "Electron import identity survives native history persistence");
            Check(restoredChats[2].Archived && restoredChats[2].ForkedFrom == "electron-chat-parent" && restoredChats[2].BranchIndex == 1 &&
                  restoredChats[2].BaseMessages is [{ Role: "user", Content: "Original question" }] &&
                  restoredChats[2].Branches is [[{ Role: "user", Content: "Alternative question" }, { Role: "assistant", Content: "Alternative answer" }]],
                "archived fork and branch metadata survives native history persistence");
            var branchRoundTripPath = Path.Combine(settingsDirectory, "branch-round-trip.json");
            var branchSource = new ChatSession
            {
                Id = "branch-source",
                Title = "Branch source",
                Messages = [new ChatMessage("user", "Main prompt"), new ChatMessage("assistant", "Main answer")],
                Branches =
                [
                    [new ChatMessage("user", "First prompt"), new ChatMessage("assistant", "First answer")],
                    [new ChatMessage("user", "Middle prompt"), new ChatMessage("assistant", "Middle answer")],
                    [new ChatMessage("user", "Last prompt"), new ChatMessage("assistant", "Last answer")]
                ],
                BranchIndex = 0
            };
            var forkedBranch = new ChatSession
            {
                Id = "branch-fork",
                Title = "Branch source · fork",
                ForkedFrom = branchSource.Id,
                Messages = [.. branchSource.Branches[1]]
            };
            var branchStore = new ConversationStore(branchRoundTripPath);
            branchStore.Save([branchSource, forkedBranch]);
            var branchReloaded = branchStore.Load();
            Check(branchReloaded[0].BranchIndex == 0 && branchReloaded[0].Branches.Count == 3 &&
                  branchReloaded[0].Branches[0][1].Content == "First answer" &&
                  branchReloaded[0].Branches[1][1].Content == "Middle answer" &&
                  branchReloaded[0].Branches[2][1].Content == "Last answer",
                "main, first, middle, and last conversation branches survive persistence");
            branchReloaded[0].Archived = true;
            branchStore.Save(branchReloaded);
            var archivedParentReloaded = branchStore.Load();
            Check(archivedParentReloaded.Single(session => session.Id == "branch-fork").Archived == false,
                "archiving a source conversation does not cascade to its fork");
            archivedParentReloaded.RemoveAll(session => session.Id == branchSource.Id);
            branchStore.Save(archivedParentReloaded);
            var forkAfterSourceDelete = branchStore.Load().Single();
            Check(forkAfterSourceDelete.Id == "branch-fork" && forkAfterSourceDelete.ForkedFrom == "branch-source" &&
                  forkAfterSourceDelete.Messages[1].Content == "Middle answer",
                "forked conversation remains independent when its source is deleted");
            var nullableConversationPath = Path.Combine(settingsDirectory, "nullable-conversations.json");
            File.WriteAllText(nullableConversationPath, "[{\"id\":null,\"title\":null,\"messages\":null,\"baseMessages\":null,\"branches\":null,\"mode\":null,\"importSource\":\"electron-localstorage\",\"importSourceId\":\"legacy-chat\"}]");
            var normalizedConversation = new ConversationStore(nullableConversationPath).Load().Single();
            Check(!string.IsNullOrWhiteSpace(normalizedConversation.Id) && normalizedConversation.Title == "New chat" && normalizedConversation.Messages.Count == 0 && normalizedConversation.BaseMessages.Count == 0 && normalizedConversation.Branches.Count == 0 && normalizedConversation.Mode == "ChatBot" && normalizedConversation.ImportSourceId == "legacy-chat",
                "nullable conversation fields recover without losing migration identity");
            var healthyHistoryBeforeRejectedSave = File.ReadAllBytes(conversationsPath);
            try
            {
                conversationStore.Save([new ChatSession
                {
                    Id = "invalid-local-history",
                    Messages = [new ChatMessage("system", "This role must never enter persisted user history.")]
                }]);
                throw new Exception("Expected invalid local conversation persistence to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(File.ReadAllBytes(conversationsPath).SequenceEqual(healthyHistoryBeforeRejectedSave),
                    "invalid local history is rejected without replacing the previous saved conversations");
            }
            var restartSafeImport = ConversationArchive.PlanImport(restoredChats, await ReadElectronFixtureAsync(electronExportPath));
            Check(restartSafeImport.Conversations.Count == 1 && restartSafeImport.SkippedDuplicates == 1, "re-import remains idempotent after closing and reopening the app");
            var editedChats = restoredChats.ToList();
            editedChats[0].Title = "Renamed thread";
            editedChats.RemoveAt(1);
            editedChats.RemoveAt(1);
            conversationStore.Save(editedChats);
            var reloadedEdits = conversationStore.Load();
            Check(reloadedEdits.Count == 1 && reloadedEdits[0].Title == "Renamed thread", "renaming and deleting conversations survive save and reload");

            var projectsPath = Path.Combine(settingsDirectory, "projects.json");
            var projectStore = new ProjectStore(projectsPath);
            projectStore.Save([
                new LocalProject { Id = "project-demo", Name = "Demo project", Instructions = "Use concise answers." },
                new LocalProject { Id = "project-notes", Name = "Notes", Instructions = "Prefer bullet points." }
            ]);
            var restoredProjects = projectStore.Load();
            Check(restoredProjects.Count == 2, "project store keeps multiple projects");
            Check(restoredProjects[0].Instructions == "Use concise answers." && restoredProjects[1].Instructions == "Prefer bullet points.", "project instructions survive save and reload independently");
            var nullableProjectPath = Path.Combine(settingsDirectory, "nullable-projects.json");
            File.WriteAllText(nullableProjectPath, "[{\"id\":null,\"name\":null,\"instructions\":null,\"folderPaths\":null,\"gitHubRepository\":null}]");
            var normalizedProject = new ProjectStore(nullableProjectPath).Load().Single();
            Check(!string.IsNullOrWhiteSpace(normalizedProject.Id) && normalizedProject.Name == "New project" && normalizedProject.Instructions == "" && normalizedProject.FolderPaths is not null,
                "nullable project fields recover to usable native defaults");
            var duplicateProjectFilePath = Path.Combine(settingsDirectory, "duplicate-projects.json");
            const string duplicateProjectJson = "[{\"id\":\"duplicate-project\",\"name\":\"First\",\"instructions\":\"\",\"folderPaths\":[]},{\"id\":\"duplicate-project\",\"name\":\"Second\",\"instructions\":\"\",\"folderPaths\":[]}]";
            File.WriteAllText(duplicateProjectFilePath, duplicateProjectJson);
            try
            {
                _ = new ProjectStore(duplicateProjectFilePath).Load();
                throw new Exception("Expected duplicate saved project IDs to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(File.ReadAllText(duplicateProjectFilePath) == duplicateProjectJson,
                    "duplicate project data is rejected without replacing the recovery file");
            }
            var healthyProjectsBeforeRejectedSave = File.ReadAllBytes(projectsPath);
            try
            {
                projectStore.Save([
                    new LocalProject { Id = "project-demo", Name = "Duplicate ID" },
                    new LocalProject { Id = "project-demo", Name = "Duplicate ID again" }
                ]);
                throw new Exception("Expected invalid project persistence to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(File.ReadAllBytes(projectsPath).SequenceEqual(healthyProjectsBeforeRejectedSave),
                    "invalid local projects are rejected without replacing the previous saved projects");
            }

            var workspacePath = Path.Combine(settingsDirectory, "workspace");
            Directory.CreateDirectory(Path.Combine(workspacePath, "src"));
            Directory.CreateDirectory(Path.Combine(workspacePath, ".git"));
            Directory.CreateDirectory(Path.Combine(workspacePath, "node_modules"));
            File.WriteAllText(Path.Combine(workspacePath, "README.md"), "Workspace overview");
            File.WriteAllText(Path.Combine(workspacePath, ".env"), "API_KEY=secret");
            File.WriteAllText(Path.Combine(workspacePath, ".git", "config"), "private git data");
            File.WriteAllText(Path.Combine(workspacePath, "node_modules", "package.json"), "dependency data");
            File.WriteAllText(Path.Combine(workspacePath, "src", "Program.cs"), "Console.WriteLine(\"local\");");
            File.WriteAllBytes(Path.Combine(workspacePath, "preview.png"), [0x89, 0x50, 0x4E, 0x47]);
            File.WriteAllBytes(Path.Combine(workspacePath, "report.pdf"), "%PDF-"u8.ToArray());
            var workspaceFiles = WorkspaceFileContext.ReadSelectedFiles([workspacePath]);
            Check(workspaceFiles.Any(item => item.Path == "README.md"), "selected workspace context reads project documentation");
            Check(workspaceFiles.Any(item => item.Path == Path.Combine("src", "Program.cs")), "selected workspace context reads nested source files");
            Check(workspaceFiles.All(item => !item.Path.Contains(".git", StringComparison.OrdinalIgnoreCase) && !item.Path.Contains("node_modules", StringComparison.OrdinalIgnoreCase) && !item.Path.EndsWith(".env", StringComparison.OrdinalIgnoreCase)), "workspace context skips common secrets and dependency metadata");
            Check(workspaceFiles.Count <= WorkspaceFileContext.MaxFiles && workspaceFiles.Sum(item => item.Content.Length) <= WorkspaceFileContext.MaxTotalCharacters, "workspace context stays within its file and character budgets");
            var previewableFiles = WorkspaceFileContext.EnumeratePreviewableFiles([workspacePath]);
            Check(previewableFiles.Any(item => item.RelativePath == "preview.png" && item.Kind == "image") && previewableFiles.Any(item => item.RelativePath == "report.pdf" && item.Kind == "pdf"), "workspace browser enumerates bounded image and PDF previews");
            Check(previewableFiles.All(item => !item.RelativePath.Contains(".git", StringComparison.OrdinalIgnoreCase) && !item.RelativePath.Contains("node_modules", StringComparison.OrdinalIgnoreCase) && !item.RelativePath.EndsWith(".env", StringComparison.OrdinalIgnoreCase)), "workspace browser excludes protected and dependency paths");

            var gitReviewPath = Path.Combine(settingsDirectory, "git-review");
            Directory.CreateDirectory(gitReviewPath);
            RunGit(gitReviewPath, "init", "-b", "main");
            RunGit(gitReviewPath, "config", "user.email", "server-rock-tests@example.invalid");
            RunGit(gitReviewPath, "config", "user.name", "Skai Desktop Tests");
            RunGit(gitReviewPath, "remote", "add", "origin", "https://github.com/owner/project.git");
            File.WriteAllText(Path.Combine(gitReviewPath, "README.md"), "Original README\n");
            File.WriteAllText(Path.Combine(gitReviewPath, "notes.md"), "Original notes\n");
            RunGit(gitReviewPath, "add", "--all");
            RunGit(gitReviewPath, "commit", "-m", "initial");
            File.WriteAllText(Path.Combine(gitReviewPath, "README.md"), "Changed README\n");
            var renamedNotesName = new string('r', 96) + ".md";
            File.Move(Path.Combine(gitReviewPath, "notes.md"), Path.Combine(gitReviewPath, renamedNotesName));
            RunGit(gitReviewPath, "add", "-A", "--", "notes.md", renamedNotesName);
            Directory.CreateDirectory(Path.Combine(gitReviewPath, ".github", "workflows"));
            File.WriteAllText(Path.Combine(gitReviewPath, ".github", "workflows", "ci.yml"), "name: CI\n");
            File.WriteAllText(Path.Combine(gitReviewPath, "secrets.json"), "{\"token\":\"secret-value\"}\n");
            var gitReviewService = new GitHubWorkspaceService();
            var reviewStatus = await gitReviewService.GetStatusAsync(gitReviewPath);
            Check(reviewStatus.Changes.Any(change => change.Path == "README.md"), "GitHub review status includes normal modified files");
            Check(reviewStatus.Changes.Any(change => change.OriginalPath == "notes.md" && change.Path == renamedNotesName), "GitHub review status preserves long rename provenance");
            Check(reviewStatus.Changes.Any(change => change.Path == ".github/workflows/ci.yml"), "GitHub review status includes workflow files");
            Check(reviewStatus.Changes.Any(change => change.Path == "secrets.json"), "GitHub review status includes protected files for redacted review");
            var reviewDiff = await gitReviewService.GetDiffPreviewAsync(gitReviewPath, reviewStatus.Changes);
            Check(reviewDiff.IncludedFiles == 3 && reviewDiff.RedactedFiles == 1 && reviewDiff.Text.Contains("Changed README", StringComparison.Ordinal) && reviewDiff.Text.Contains(renamedNotesName, StringComparison.Ordinal) && reviewDiff.Text.Contains("[protected path redacted; review locally before staging]", StringComparison.Ordinal), "GitHub review previews bounded unified diffs and explains protected-file redaction");
            Check(!reviewDiff.Text.Contains("secret-value", StringComparison.Ordinal), "GitHub review diff never exposes protected file content");
            using var canceledReview = new CancellationTokenSource();
            canceledReview.Cancel();
            try
            {
                await gitReviewService.GetDiffPreviewAsync(gitReviewPath, reviewStatus.Changes, canceledReview.Token);
                throw new Exception("Expected canceled Git review preview to stop before running Git.");
            }
            catch (OperationCanceledException)
            {
                Check(true, "GitHub review diff honors cancellation before starting work");
            }
            var safeReviewChanges = reviewStatus.Changes.Where(change => GitHubWorkspaceService.FindUnsafeChangePaths([change]).Count == 0).ToArray();
            var preStagedPath = Path.Combine(gitReviewPath, "pre-staged-user-work.md");
            File.WriteAllText(preStagedPath, "keep staged but out of the selected commit\n");
            RunGit(gitReviewPath, "add", "--", "pre-staged-user-work.md");
            _ = await gitReviewService.CommitSelectedAsync(gitReviewPath, safeReviewChanges, "Review safe workspace changes", "test-access-token");
            var committedFiles = RunGitOutput(gitReviewPath, "show", "--format=", "--name-only", "HEAD");
            var stagedFiles = RunGitOutput(gitReviewPath, "diff", "--cached", "--name-only");
            Check(!committedFiles.Contains("pre-staged-user-work.md", StringComparison.Ordinal) && stagedFiles.Contains("pre-staged-user-work.md", StringComparison.Ordinal), "GitHub selected commit excludes unrelated staged changes and preserves the existing index");
            var afterReviewStatus = await gitReviewService.GetStatusAsync(gitReviewPath);
            Check(afterReviewStatus.Changes.Any(change => change.Path == "secrets.json") && afterReviewStatus.Changes.Any(change => change.Path == "pre-staged-user-work.md"), "GitHub review selectively stages safe normal, rename, and workflow changes");

            var commandRunner = new WorkspaceCommandRunner();
            var commandResponse = "Check the result first.\n<workspace-command cwd=\"src\" timeout-seconds=\"999\">  printf 'one\\ntwo'  </workspace-command>\n<workspace-command>echo next</workspace-command>";
            var proposedCommands = WorkspaceCommandProposalParser.Parse(commandResponse);
            Check(proposedCommands.Count == 2 && proposedCommands[0].RelativeWorkingDirectory == "src" && proposedCommands[0].Command.Contains("printf", StringComparison.Ordinal), "Coding Agent command proposals preserve command and selected project subfolder");
            Check(proposedCommands[0].TimeoutSeconds == 120 && proposedCommands[1].TimeoutSeconds == 30, "Coding Agent command proposals clamp long timeouts and default missing timeouts");
            Check(!WorkspaceCommandProposalParser.RemoveProposals(commandResponse).Contains("workspace-command", StringComparison.Ordinal), "internal command proposal markup is hidden from the conversation text");
            try
            {
                await commandRunner.RunAsync(workspacePath, ".", "echo too-fast", TimeSpan.FromMilliseconds(500));
                throw new Exception("Expected a sub-second command timeout to be rejected.");
            }
            catch (ArgumentOutOfRangeException)
            {
                Check(true, "project command rejects sub-second timeouts");
            }
            const string secretEnvironmentName = "SERVER_ROCK_TEST_SECRET";
            var previousSecretEnvironment = Environment.GetEnvironmentVariable(secretEnvironmentName);
            Environment.SetEnvironmentVariable(secretEnvironmentName, "must-not-leak");
            try
            {
                var environmentResult = await commandRunner.RunAsync(workspacePath, ".",
                    OperatingSystem.IsWindows() ? $"Write-Output $env:{secretEnvironmentName}" : $"printf \"${secretEnvironmentName}\"",
                    TimeSpan.FromSeconds(10));
                Check(!environmentResult.Output.Contains("must-not-leak", StringComparison.Ordinal), "project commands do not inherit secret-shaped environment variables");
            }
            finally
            {
                Environment.SetEnvironmentVariable(secretEnvironmentName, previousSecretEnvironment);
            }
            var commandResult = await commandRunner.RunAsync(workspacePath, "src",
                OperatingSystem.IsWindows() ? "Write-Output server-rock-command-ok" : "printf server-rock-command-ok",
                TimeSpan.FromSeconds(10));
            Check(commandResult.ExitCode == 0 && commandResult.Output.Contains("server-rock-command-ok", StringComparison.Ordinal), "approved project command runs in the selected project subfolder and captures output");
            Check(commandResult.WorkingDirectory == Path.Combine(workspacePath, "src"), "project command reports its resolved working directory");
            var timedCommand = await commandRunner.RunAsync(workspacePath, ".",
                OperatingSystem.IsWindows() ? "Start-Sleep -Seconds 5" : "sleep 5",
                TimeSpan.FromSeconds(1));
            Check(timedCommand.TimedOut, "project command timeout stops the process instead of leaving a task running");
            using (var commandCancellation = new CancellationTokenSource())
            {
                var cancellableCommand = commandRunner.RunAsync(workspacePath, ".",
                    OperatingSystem.IsWindows() ? "Start-Sleep -Seconds 5" : "sleep 5",
                    TimeSpan.FromSeconds(30), commandCancellation.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                commandCancellation.Cancel();
                try
                {
                    await cancellableCommand;
                    throw new Exception("Expected a cancelled project command to stop with OperationCanceledException.");
                }
                catch (OperationCanceledException)
                {
                    Check(true, "project command cancellation kills the process tree and propagates cancellation");
                }
            }
            await using (var terminalSession = new WorkspaceTerminalSession())
            {
                await terminalSession.StartAsync(workspacePath, shell: OperatingSystem.IsWindows() ? "Command Prompt" : null);
                var terminalCommand = OperatingSystem.IsWindows()
                    ? "echo server-rock-pty-ok & exit"
                    : "printf 'server-rock-pty-ok\\n'; exit";
                await terminalSession.SendTextAsync(terminalCommand + "\r");
                var terminalExitCode = await terminalSession.Completion.WaitAsync(TimeSpan.FromSeconds(15));
                var terminalScreen = terminalSession.ScreenText;
                // On failure, include the exit code and screen so CI shows why (it only said "Check failed").
                var terminalPassed = terminalExitCode == 0 && terminalScreen.Split('\n').Any(line => line.Trim() == "server-rock-pty-ok");
                Check(terminalPassed, terminalPassed
                    ? "interactive project terminal accepts input and renders a real PTY response"
                    : $"interactive project terminal accepts input and renders a real PTY response (exit code: {terminalExitCode?.ToString() ?? "none"}; screen: {terminalScreen.Trim().Replace("\n", " | ")})");
            }
            try
            {
                await commandRunner.RunAsync(workspacePath, "../", "echo outside", TimeSpan.FromSeconds(1));
                throw new Exception("Expected a project command working-directory traversal to be rejected.");
            }
            catch (ArgumentException)
            {
                Check(true, "project command refuses working directories outside the selected folder");
            }

            var editor = new WorkspaceFileEditor();
            var oversizedPreviewPath = Path.Combine(workspacePath, "src", "Oversized.cs");
            File.WriteAllText(oversizedPreviewPath, new string('x', WorkspaceFileEditor.MaxCharacters * 4 + 1));
            try
            {
                _ = editor.ReadText(workspacePath, Path.Combine("src", "Oversized.cs"));
                throw new Exception("Expected oversized workspace preview to be rejected.");
            }
            catch (InvalidOperationException)
            {
                Check(true, "workspace editor bounds oversized file previews");
            }
            try
            {
                _ = editor.GetCurrentHash(workspacePath, Path.Combine("src", "Oversized.cs"));
                throw new Exception("Expected oversized workspace hashing to be rejected.");
            }
            catch (InvalidOperationException)
            {
                Check(true, "workspace editor bounds oversized file hashing before undo state capture");
            }
            var replacementDiff = WorkspaceTextDiff.Create("one\ntwo\nthree\n", "one\nTWO\nthree\n");
            Check(replacementDiff.AddedLineCount == 1 && replacementDiff.RemovedLineCount == 1, "workspace diff reports one replaced line as one addition and removal");
            Check(replacementDiff.Lines.Any(item => item.Kind == WorkspaceDiffLineKind.Removed && item.Text == "two") && replacementDiff.Lines.Any(item => item.Kind == WorkspaceDiffLineKind.Added && item.Text == "TWO"), "workspace diff preserves removed and added replacement text");
            var identicalDiff = WorkspaceTextDiff.Create("same\n", "same\n");
            Check(!identicalDiff.HasTextChanges && identicalDiff.Lines.Count == 0, "workspace diff marks identical files as having no text changes");
            var newlineDiff = WorkspaceTextDiff.Create("line\n", "line");
            Check(newlineDiff.HasTextChanges && newlineDiff.Lines.Any(item => item.Kind == WorkspaceDiffLineKind.Metadata && item.Text.Contains("No newline at end of proposed", StringComparison.Ordinal)), "workspace diff exposes a missing final newline");
            var largeDiff = WorkspaceTextDiff.Create(string.Join('\n', Enumerable.Range(1, WorkspaceTextDiff.MaxComparableLines + 1)), "small");
            Check(largeDiff.IsTooLarge && largeDiff.Lines.Count == 0, "workspace diff declines oversized files instead of rendering a misleading preview");
            try
            {
                editor.Apply(workspacePath, "../outside.txt", "should not escape", null);
                throw new Exception("Expected workspace traversal to be rejected.");
            }
            catch (InvalidOperationException)
            {
                Check(!File.Exists(Path.Combine(settingsDirectory, "outside.txt")), "workspace editor rejects paths outside the selected folder");
            }
            try
            {
                editor.Apply(workspacePath, ".env", "API_KEY=changed", null);
                throw new Exception("Expected protected secrets to be rejected.");
            }
            catch (InvalidOperationException)
            {
                Check(File.ReadAllText(Path.Combine(workspacePath, ".env")) == "API_KEY=secret", "workspace editor refuses to modify secret files");
            }
            var createdReceipt = editor.Apply(workspacePath, Path.Combine("src", "NewFile.cs"), "// proposed", null);
            Check(File.ReadAllText(Path.Combine(workspacePath, "src", "NewFile.cs")) == "// proposed", "approved workspace write creates the reviewed file");
            editor.Undo(createdReceipt);
            Check(!File.Exists(Path.Combine(workspacePath, "src", "NewFile.cs")), "undo removes a file created by the agent");
            var readmePath = Path.Combine(workspacePath, "README.md");
            var readmeHash = editor.GetCurrentHash(workspacePath, "README.md");
            var replacedReceipt = editor.Apply(workspacePath, "README.md", "Updated overview", readmeHash);
            editor.Undo(replacedReceipt);
            Check(File.ReadAllText(readmePath) == "Workspace overview", "undo restores the previous text file contents");
            var concurrentHash = editor.GetCurrentHash(workspacePath, "README.md")!;
            var concurrentWrites = Enumerable.Range(0, 2)
                .Select(index => Task.Run(() =>
                {
                    try
                    {
                        editor.Apply(workspacePath, "README.md", $"Concurrent writer {index}", concurrentHash);
                        return true;
                    }
                    catch (IOException) { return false; }
                }))
                .ToArray();
            var concurrentResults = await Task.WhenAll(concurrentWrites);
            Check(concurrentResults.Count(result => result) == 1, "workspace edits serialize reviewed writes and reject the losing concurrent hash");
            readmeHash = editor.GetCurrentHash(workspacePath, "README.md");
            var undoHistoryPath = Path.Combine(settingsDirectory, "workspace-undo.json");
            var undoStore = new WorkspaceUndoStore(undoHistoryPath);
            var durableReceipt = editor.Apply(workspacePath, Path.Combine("src", "Durable.cs"), "// durable change", null);
            var durableEntry = new WorkspaceUndoEntry("durable-entry", DateTimeOffset.UtcNow, "Test project", workspacePath,
                Path.Combine("src", "Durable.cs"), durableReceipt.PreviousContent, durableReceipt.AppliedHash);
            undoStore.Add([], durableEntry);
            var reloadedUndoStore = new WorkspaceUndoStore(undoHistoryPath);
            var reloadedUndo = reloadedUndoStore.Load();
            Check(reloadedUndo.Count == 1 && reloadedUndo[0].Id == durableEntry.Id, "approved file undo history survives store reload");
            reloadedUndoStore.Undo(reloadedUndo[0], editor);
            reloadedUndoStore.Remove(reloadedUndo, durableEntry.Id);
            Check(!File.Exists(Path.Combine(workspacePath, "src", "Durable.cs")) && reloadedUndoStore.Load().Count == 0, "persisted undo safely restores and removes an approved new file");
            var durableConflictReceipt = editor.Apply(workspacePath, Path.Combine("src", "Durable.cs"), "// approved", null);
            var durableConflictEntry = new WorkspaceUndoEntry("conflict-entry", DateTimeOffset.UtcNow, "Test project", workspacePath,
                Path.Combine("src", "Durable.cs"), durableConflictReceipt.PreviousContent, durableConflictReceipt.AppliedHash);
            reloadedUndoStore.Add([], durableConflictEntry);
            File.WriteAllText(Path.Combine(workspacePath, "src", "Durable.cs"), "// newer user change");
            try
            {
                reloadedUndoStore.Undo(reloadedUndoStore.Load()[0], editor);
                throw new Exception("Expected persisted undo to preserve newer work.");
            }
            catch (IOException)
            {
                Check(File.ReadAllText(Path.Combine(workspacePath, "src", "Durable.cs")) == "// newer user change", "persisted undo refuses to overwrite newer file changes");
            }
            try
            {
                editor.Undo(workspacePath, Path.Combine("..", "outside.txt"), durableConflictReceipt);
                throw new Exception("Expected project-relative undo to reject traversal.");
            }
            catch (InvalidOperationException)
            {
                Check(true, "workspace undo refuses paths outside the selected project");
            }
            var conflictReceipt = editor.Apply(workspacePath, "README.md", "Reviewed change", readmeHash);
            File.WriteAllText(readmePath, "User changed after review");
            try
            {
                editor.Undo(conflictReceipt);
                throw new Exception("Expected undo to preserve a newer user change.");
            }
            catch (IOException)
            {
                Check(File.ReadAllText(readmePath) == "User changed after review", "undo refuses to overwrite edits made after agent approval");
            }

            projectStore.Save([new LocalProject { Id = "project-folders", Name = "Folder project", FolderPaths = [workspacePath] }]);
            Check(projectStore.Load()[0].FolderPaths.SequenceEqual([workspacePath]), "selected project folders survive save and reload");

            File.WriteAllText(conversationsPath, "{invalid json");
            try
            {
                conversationStore.Load();
                throw new Exception("Expected corrupt history to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(File.ReadAllText(conversationsPath) == "{invalid json", "corrupt conversation data is preserved for recovery");
            }
            File.WriteAllText(projectsPath, "{invalid json");
            try
            {
                projectStore.Load();
                throw new Exception("Expected corrupt project data to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(File.ReadAllText(projectsPath) == "{invalid json", "corrupt project data is preserved for recovery");
            }
            File.WriteAllText(undoHistoryPath, "{invalid json");
            try
            {
                reloadedUndoStore.Load();
                throw new Exception("Expected corrupt undo history to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(File.ReadAllText(undoHistoryPath) == "{invalid json", "corrupt undo history is preserved for recovery");
            }
        }
        finally
        {
            if (Directory.Exists(settingsDirectory)) DeleteDirectoryTree(settingsDirectory);
        }

        // Path comparison (moved from MainPage into Core).
        var comparedRoot = Path.Combine(Path.GetTempPath(), "server-rock-same-path");
        Check(WorkspacePath.AreSame(comparedRoot, comparedRoot + Path.DirectorySeparatorChar)
            && !WorkspacePath.AreSame(comparedRoot, comparedRoot + "-other"),
            "workspace path comparison ignores a trailing separator and tells folders apart");
    }
}
