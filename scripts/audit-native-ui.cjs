const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const nativeDir = path.join(root, "native", "ServerRock.Native");
const xamlPath = path.join(nativeDir, "MainPage.xaml");
const xaml = fs.readFileSync(xamlPath, "utf8");

// MainPage and the GitHub workspace UI are partial classes split across several
// files (MainPage.xaml.cs, MainPage.Chat.cs, GitHubWorkspaceUi.Changes.cs, …).
// Read every file of each group so a contract still passes after code moves
// between those files.
const readPartials = (prefix) =>
  fs.readdirSync(nativeDir)
    .filter((name) => name.startsWith(prefix) && name.endsWith(".cs"))
    .sort()
    .map((name) => fs.readFileSync(path.join(nativeDir, name), "utf8"))
    .join("\n");
const code = readPartials("MainPage.");
const githubCode = readPartials("GitHubWorkspaceUi");

const requiredXaml = [
  ['Connection setup action', 'AutomationProperties.Name="Connect a model"'],
  ['Navigation menu', 'AutomationProperties.Name="Open navigation menu"'],
  ['Message composer', 'AutomationProperties.Name="Message composer"'],
  ['Composer responsive hook', 'SizeChanged="ComposerToolbarSizeChanged"'],
  ['Coding workspace responsive hook', 'SizeChanged="CodingWorkspaceToolbarSizeChanged"'],
];
const requiredCode = [
  ['Model source label', 'AutomationProperties.SetName(source, "Model source")'],
  ['Model search label', 'AutomationProperties.SetName(query, "Search models")'],
  ['Model results label', 'AutomationProperties.SetName(list, "Model search results")'],
  ['Project name label', 'AutomationProperties.SetName(name, "Project name")'],
  ['Project folder action label', 'AutomationProperties.SetName(addFolder, "Add local project folder")'],
  ['Live model status', 'AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite)'],
  ['Folder toggle focus tracking', '_chatFolderToggleButtons[folder] = open;'],
  ['Folder toggle focus restoration', 'FocusChatFolderToggleOrFallback(folder)'],
  ['Archived toggle focus restoration', 'DispatcherQueue.TryEnqueue(FocusArchivedChatsToggleOrFallback);'],
  ['Compact drawer landing focus', 'ChatModeButton.Focus(FocusState.Programmatic);'],
  ['Compact drawer close focus', 'ToggleSidebarButton.Focus(FocusState.Programmatic);'],
  ['Settings dialog focus return', 'RestoreNavigationDialogFocus(returnToCompactSidebar, sender as Control);'],
  ['Settings trigger focus restoration', 'trigger.Focus(FocusState.Programmatic)'],
  ['Settings section search accessible label', 'AutomationProperties.SetName(search, "Search settings sections")'],
  ['Settings chats and history category', 'AddSection("Chats & history", "archive import export conversations"'],
  ['Settings archive restore actions', 'AutomationProperties.SetName(restore, $"Restore archived chat {session.Title}")'],
  ['Settings archive restore keyboard focus', 'chatRestoreButtons.FirstOrDefault()?.Focus(FocusState.Programmatic)'],
  ['Settings model library routing', 'actionAfterClose = () => OpenModelLibraryAndRestoreFocusAsync(false, ModelButton)'],
  ['Settings GitHub workspace routing', 'actionAfterClose = () => RunUiActionAsync(GitHubWorkspaceDialogAsync)'],
  ['Model library focus return', 'OpenModelLibraryAndRestoreFocusAsync(returnToCompactSidebar, sender as Control)'],
  ['Model hotswap library focus', 'OpenModelLibraryAndRestoreFocusAsync(false, ModelButton)'],
  ['Slash model library focus', 'OpenModelLibraryAndRestoreFocusAsync(false, PromptBox)'],
  ['Chat mode drawer focus return', 'SwitchModeFromSidebarAsync("ChatBot")'],
  ['Coding Agent drawer focus return', 'SwitchModeFromSidebarAsync("Coding Agent")'],
  ['Compact workspace focus restoration', 'DispatcherQueue.TryEnqueue(() => PromptBox.Focus(FocusState.Programmatic));'],
  ['Compact project action focus restoration', 'RunCompactProjectActionAsync'],
  ['Compact project cancellation focus fallback', 'if (restoreComposerFocus)\n                DispatcherQueue.TryEnqueue(() => PromptBox.Focus(FocusState.Programmatic));'],
  ['Project folder dialog focus return', 'if (managesExistingProject)\n                RestoreNavigationDialogFocus(false, sender as Control);'],
  ['Project file editor opening focus', 'dialog.Opened += (_, _) => editor.Focus(FocusState.Programmatic);'],
  ['Project file selection focus restoration', 'finally { RestoreProjectFileFocus(selection.Item1, selection.Item2); }'],
  ['Terminal input opening focus', 'dialog.Opened += (_, _) => input.Focus(FocusState.Programmatic);'],
  ['Terminal launcher focus restoration', 'finally { RestoreNavigationDialogFocus(false, sender as Control); }'],
  ['Terminal focused input accessible name', 'AutomationProperties.SetName(input, "Project terminal input")'],
  ['Command review accessible preview', 'AutomationProperties.SetName(commandText, "Exact project command to run")'],
  ['Command review opening focus', 'dialog.Opened += (_, _) => commandText.Focus(FocusState.Programmatic);'],
  ['Command review cancel focus target', 'Control focusAfterReview = reviewButton;'],
  ['Command review completion focus restoration', 'finally\n        {\n            DispatcherQueue.TryEnqueue(() =>\n            {\n                if (focusAfterReview.Visibility == Visibility.Visible && focusAfterReview.IsEnabled'],
  ['Command approval result action focus', 'setFocusAfterReview(addResult);'],
  ['Command output accessible preview label', 'AutomationProperties.SetName(outputPreview, "Approved local command output")'],
  ['Untrusted result insertion disclosure', 'AutomationProperties.SetHelpText(addResult, "Insert this untrusted command output into the message for review. It will not be sent automatically.")'],
  ['Project terminal output keyboard navigation', 'IsTabStop = true,\n            VerticalScrollBarVisibility'],
  ['Project terminal output accessible name', 'AutomationProperties.SetName(outputScroll, "Project terminal output pane")'],
  ['Project terminal output accessible description', 'AutomationProperties.SetHelpText(outputScroll, "Scrollable, read-only history from the shell running in the active project folder.")'],
  ['Project terminal status accessible name', 'AutomationProperties.SetName(status, "Project terminal status")'],
  ['Project terminal state live announcements', 'AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite)'],
];
const requiredGitHubCode = [
  ['GitHub review status accessible name', 'AutomationProperties.SetName(actionStatus, "GitHub changes status")'],
  ['GitHub review status polite announcements', 'AutomationProperties.SetLiveSetting(actionStatus, AutomationLiveSetting.Polite)'],
  ['GitHub review initial focus registration', 'dialog.Opened += (_, _) => FocusInitialReviewTarget();'],
  ['GitHub review first selectable file focus', 'var firstSelectableChange = changeChecks.FirstOrDefault(check => check.IsEnabled);'],
  ['GitHub operation error survives status refresh', 'try { Render(await _githubWorkspaceService.GetStatusAsync(repositoryPath)); }\n                catch { RestoreIdleState(); }\n                actionStatus.Text = exception.Message;\n                FocusRetryTarget(preferAction: true);'],
  ['GitHub commit/push launcher focus return', 'finally { RestoreNavigationDialogFocus(false, sender as Control); }'],
  ['GitHub client ID accessible label', 'AutomationProperties.SetName(clientId, "GitHub App client ID")'],
  ['GitHub dialog focus by connection state', 'if (_githubCredential is not null && repoPanel.Visibility == Visibility.Visible) FocusRepositoryEntry();\n            else clientId.Focus(FocusState.Programmatic);'],
  ['GitHub connection state accessible name', 'AutomationProperties.SetName(connection, "GitHub connection state")'],
  ['GitHub connection state polite announcements', 'AutomationProperties.SetLiveSetting(connection, AutomationLiveSetting.Polite)'],
  ['GitHub device code focus after creation', 'code.Focus(FocusState.Programmatic);'],
  ['GitHub device flow start announcement', 'signInStatus.Text = "Starting GitHub device sign-in…";'],
  ['GitHub repository chooser accessible label', 'AutomationProperties.SetName(repositoryPicker, "GitHub repositories")'],
  ['GitHub connection and repository status accessible name', 'AutomationProperties.SetName(signInStatus, "GitHub connection and repository status")'],
  ['GitHub sign-in live status', 'AutomationProperties.SetLiveSetting(signInStatus, AutomationLiveSetting.Polite)'],
  ['GitHub canceled sign-in clears one-time controls', 'code.Text = string.Empty;\n            code.Visibility = Visibility.Collapsed;\n            verification.Visibility = Visibility.Collapsed;\n            cancelSignIn.Visibility = Visibility.Collapsed;'],
  ['GitHub sign-in recovery keyboard focus', 'connect.Focus(FocusState.Programmatic);'],
  ['GitHub repository refresh failure focus recovery', 'if (repositoryRefreshFailed) repositoryReference.Focus(FocusState.Programmatic);\n                    else FocusRepositoryEntry();'],
  ['GitHub repository refresh failure clears stale choices', '_githubRepositories.Clear();\n                repositoryPicker.Items.Clear();\n                signInStatus.Text = $"Repository list could not be loaded:'],
  ['GitHub repository refresh loading announcement', 'signInStatus.Text = "Loading repositories from GitHub…";'],
  ['GitHub repository focus after successful refresh', 'else FocusRepositoryEntry();'],
  ['GitHub clone readiness guard', 'clone.IsEnabled = _githubCredential is not null && hasValidRepository && destinationReady && !string.IsNullOrWhiteSpace(parentPath.Text) && !cloneInProgress;'],
  ['GitHub clone destination collision preflight', 'Directory.EnumerateFileSystemEntries(destination).Any()'],
  ['GitHub clone retry destination guidance', 'Choose another parent folder to retry.'],
  ['GitHub manual repository readiness update', 'repositoryReference.TextChanged += (_, _) => UpdateCloneAvailability(repositoryValidation.Visibility == Visibility.Visible);'],
  ['GitHub manual repository format validation', 'repositoryFullName = GitHubApiClient.ParseRepositoryReference(candidate);'],
  ['GitHub invalid reference feedback on blur', 'repositoryReference.LostFocus += (_, _) => UpdateCloneAvailability(revealRepositoryError: true);'],
  ['GitHub repository validation accessible name', 'AutomationProperties.SetName(repositoryValidation, "Repository format guidance")'],
  ['GitHub repository error polite announcement', 'AutomationProperties.SetLiveSetting(repositoryValidation, AutomationLiveSetting.Polite)'],
  ['GitHub validation visible before announcement', 'repositoryValidation.Visibility = Visibility.Visible;\n                repositoryValidation.Text = validationError;'],
  ['GitHub destination readiness update', 'parentPath.TextChanged += (_, _) => UpdateCloneAvailability();'],
  ['GitHub clone progress live status', 'AutomationProperties.SetLiveSetting(cloneStatus, AutomationLiveSetting.Polite)'],
  ['GitHub clone retry focus recovery', 'if (workspaceDialogOpen)\n                {\n                    DispatcherQueue.TryEnqueue(() =>\n                    {\n                        if (clone.IsEnabled && clone.Focus(FocusState.Programmatic)) return;\n                        FocusRepositoryEntry();'],
];

const missing = [
  ...requiredXaml.filter(([, text]) => !xaml.includes(text)),
  ...requiredCode.filter(([, text]) => !code.includes(text)),
  ...requiredGitHubCode.filter(([, text]) => !githubCode.includes(text)),
];

if (missing.length) {
  console.error("Native UI audit failed:");
  for (const [label, text] of missing) console.error(`- ${label}: missing ${text}`);
  process.exit(1);
}

console.log(`Native UI audit passed (${requiredXaml.length + requiredCode.length + requiredGitHubCode.length} contracts).`);
