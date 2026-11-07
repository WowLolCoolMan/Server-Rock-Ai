// Coding Agent proposals inside a reply: <workspace-change> (replace or create one file) and
// <workspace-command> (run one command). AttachWorkspaceProposals adds a "Review …" button
// under the reply for each proposal; nothing runs or is written until the user approves it in
// the review dialog (diff preview for changes; exact command, folder, and timeout for commands).
// Depends on: WorkspaceTextDiff, WorkspaceFileEditor, WorkspaceCommandProposalParser,
// WorkspaceCommandRunner (ServerRock.Core), RecordWorkspaceUndo/UndoWorkspaceEdit
// (MainPage.CodingAgent.cs).
// Called by: SendClick (MainPage.Chat.cs) after a Coding Agent reply finishes.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Infrastructure;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private sealed record ProposedWorkspaceChange(string RelativePath, string Content);

    private void AttachWorkspaceProposals(TextBlock response, int assistantIndex)
    {
        var fullResponse = _messages[assistantIndex].Content;
        var commands = WorkspaceCommandProposalParser.Parse(fullResponse);
        var proposals = System.Text.RegularExpressions.Regex.Matches(fullResponse, "<workspace-change\\s+path=\"([^\"]+)\">(.*?)</workspace-change>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Cast<System.Text.RegularExpressions.Match>()
            .Take(5)
            .Select(match => new ProposedWorkspaceChange(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim('\r', '\n')))
            .Where(item => item.RelativePath.Length > 0 && item.Content.Length <= WorkspaceFileEditor.MaxCharacters)
            .ToArray();
        if (proposals.Length == 0 && commands.Count == 0) return;

        var displayText = System.Text.RegularExpressions.Regex.Replace(fullResponse, "<workspace-change\\s+path=\"([^\"]+)\">(.*?)</workspace-change>", "", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        displayText = WorkspaceCommandProposalParser.RemoveProposals(displayText);
        _messages[assistantIndex] = new ChatMessage("assistant", displayText, Sources: _messages[assistantIndex].Sources);
        response.Text = displayText;
        if (response.Parent is not StackPanel messageStack) return;
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (project is null || project.FolderPaths.Count == 0) return;
        foreach (var command in commands)
        {
            var review = new Button { Content = $"Review command · {ShortCommandLabel(command.Command)}", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            AutomationProperties.SetName(review, $"Review local command: {command.Command}");
            review.Click += async (_, _) => await ReviewWorkspaceCommandAsync(project, command, messageStack, review);
            messageStack.Children.Add(review);
        }
        foreach (var proposal in proposals)
        {
            var review = new Button { Content = $"Review file change · {proposal.RelativePath}", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            review.Click += async (_, _) => await ReviewWorkspaceChangeAsync(project, proposal, messageStack);
            messageStack.Children.Add(review);
        }
    }

    private static string ShortCommandLabel(string command) => command.Length > 52 ? command[..49] + "…" : command;

    private async Task ReviewWorkspaceCommandAsync(LocalProject project, WorkspaceCommandProposal proposal, StackPanel messageStack, Button reviewButton)
    {
        Control focusAfterReview = reviewButton;
        try
        {
            await ReviewWorkspaceCommandCoreAsync(project, proposal, messageStack, target => focusAfterReview = target);
        }
        finally
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (focusAfterReview.Visibility == Visibility.Visible && focusAfterReview.IsEnabled &&
                    focusAfterReview.Focus(FocusState.Programmatic))
                    return;
                PromptBox.Focus(FocusState.Programmatic);
            });
        }
    }

    private async Task ReviewWorkspaceCommandCoreAsync(LocalProject project, WorkspaceCommandProposal proposal, StackPanel messageStack, Action<Control> setFocusAfterReview)
    {
        if (_requestCancellation is not null)
        {
            RuntimeStatus.Text = "Wait for the current response to finish before running a project command.";
            return;
        }
        var availableFolders = project.FolderPaths.Where(Directory.Exists).ToArray();
        if (availableFolders.Length == 0)
        {
            RuntimeStatus.Text = "The selected project folders are unavailable. Reconnect a folder before running a command.";
            return;
        }
        var selectedFolder = new ComboBox
        {
            ItemsSource = availableFolders.Select((path, index) => $"Folder {index + 1} · {path}").ToArray(),
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var commandText = new TextBox
        {
            Text = proposal.Command,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            MaxHeight = 150
        };
        AutomationProperties.SetName(commandText, "Exact project command to run");
        AutomationProperties.SetHelpText(commandText, "Read-only command preview. Review this exact command before selecting Run once.");
        var content = new StackPanel { Spacing = 9 };
        content.Children.Add(new TextBlock { Text = "Review the exact command and project folder. Nothing runs until you approve this command.", TextWrapping = TextWrapping.Wrap });
        content.Children.Add(selectedFolder);
        content.Children.Add(new TextBlock { Text = $"Working directory inside project: {proposal.RelativeWorkingDirectory} · timeout {proposal.TimeoutSeconds}s", TextWrapping = TextWrapping.Wrap, Opacity = 0.8 });
        content.Children.Add(commandText);
        content.Children.Add(new Border
        {
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 247, 230)),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 239, 222, 184)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Child = new TextBlock
            {
                Text = "Commands run on this device with your account permissions. They are not sandboxed and may read or change files, start processes, or use the network. Approve only commands you understand.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 113, 82, 35))
            }
        });
        var dialog = new ContentDialog
        {
            Title = "Approve project command",
            Content = content,
            PrimaryButtonText = "Run once",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        dialog.Opened += (_, _) => commandText.Focus(FocusState.Programmatic);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || selectedFolder.SelectedIndex < 0) return;
        setFocusAfterReview(PromptBox);
        var root = availableFolders[selectedFolder.SelectedIndex];
        RuntimeStatus.Text = "Running approved project command…";
        try
        {
            var result = await _commandRunner.RunAsync(root, proposal.RelativeWorkingDirectory, proposal.Command,
                TimeSpan.FromSeconds(proposal.TimeoutSeconds));
            var heading = result.TimedOut
                ? $"Command timed out · {proposal.Command}"
                : result.ExitCode == 0 ? $"Command finished · exit code 0 · {proposal.Command}" : $"Command failed · exit code {result.ExitCode} · {proposal.Command}";
            messageStack.Children.Add(new TextBlock { Text = heading, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 9, 0, 0), TextWrapping = TextWrapping.Wrap });
            messageStack.Children.Add(new TextBlock { Text = $"Working directory: {result.WorkingDirectory}", FontSize = 11, Opacity = 0.75, TextWrapping = TextWrapping.Wrap });
            if (!string.IsNullOrEmpty(result.Output))
            {
                var outputPreview = new TextBox
                {
                    Text = result.Output + (result.OutputTruncated ? "\n… output shortened at 30,000 characters" : ""),
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.NoWrap,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    MaxHeight = 240,
                    MinHeight = 52,
                    Margin = new Thickness(0, 3, 0, 0)
                };
                AutomationProperties.SetName(outputPreview, "Approved local command output");
                AutomationProperties.SetHelpText(outputPreview, "Untrusted output from the command you approved. Review it before adding it to a prompt.");
                messageStack.Children.Add(outputPreview);
            }
            else messageStack.Children.Add(new TextBlock { Text = "No output.", FontSize = 11, Opacity = 0.75 });
            var outputForReview = result.Output + (result.OutputTruncated ? "\n… output shortened at 30,000 characters" : "");
            messageStack.Children.Add(new TextBlock
            {
                Text = "Command output is untrusted. Review it before adding it to a prompt.",
                FontSize = 11,
                Opacity = 0.78,
                Margin = new Thickness(0, 3, 0, 0)
            });
            var addResult = new Button { Content = "Add result to composer", HorizontalAlignment = HorizontalAlignment.Left };
            AutomationProperties.SetName(addResult, "Add approved command result to message composer");
            AutomationProperties.SetHelpText(addResult, "Insert this untrusted command output into the message for review. It will not be sent automatically.");
            setFocusAfterReview(addResult);
            addResult.Click += (_, _) =>
            {
                var resultContext = $"[Untrusted output from approved local command]\nCommand: {proposal.Command}\nExit code: {result.ExitCode}{(result.TimedOut ? " (timed out)" : "")}\nWorking directory: {result.WorkingDirectory}\nOutput:\n{(string.IsNullOrEmpty(outputForReview) ? "(no output)" : outputForReview)}\n[End untrusted command output]";
                PromptBox.Text = string.IsNullOrWhiteSpace(PromptBox.Text)
                    ? resultContext + "\n\nWhat should I do next with this result?"
                    : PromptBox.Text.TrimEnd() + "\n\n" + resultContext;
                PromptBox.Focus(FocusState.Programmatic);
                PromptBox.SelectionStart = PromptBox.Text.Length;
                RuntimeStatus.Text = "Command result added to the composer. Review it before sending.";
            };
            messageStack.Children.Add(addResult);
            RuntimeStatus.Text = result.TimedOut ? "Command timed out and was stopped." : result.ExitCode == 0 ? "Approved command completed." : $"Approved command exited with code {result.ExitCode}.";
            ConversationScroll.ChangeView(null, double.MaxValue, null);
        }
        catch (Exception exception)
        {
            messageStack.Children.Add(new TextBlock { Text = $"Command could not run: {exception.Message}", TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 150, 58, 52)), Margin = new Thickness(0, 8, 0, 0) });
            RuntimeStatus.Text = "Approved command could not run.";
        }
    }

    private async Task ReviewWorkspaceChangeAsync(LocalProject project, ProposedWorkspaceChange proposal, StackPanel messageStack)
    {
        var rootPicker = new ComboBox { ItemsSource = project.FolderPaths.Select((path, index) => $"Folder {index + 1} · {path}").ToArray(), SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        var currentText = "";
        string? expectedHash;
        string root;
        try
        {
            root = project.FolderPaths[0];
            expectedHash = _workspaceEditor.GetCurrentHash(root, proposal.RelativePath);
            if (expectedHash is not null) currentText = _workspaceEditor.ReadText(root, proposal.RelativePath);
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"File change cannot be reviewed: {ex.Message}";
            return;
        }
        var existing = expectedHash is not null;
        var current = new TextBox { Text = expectedHash is null ? "(New file)" : currentText, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Height = 180 };
        var proposed = new TextBox { Text = proposal.Content, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Height = 180 };
        var currentHeading = new TextBlock { Text = existing ? "Current file" : "Current file · does not exist yet", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        var diffSummary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var diffPreview = new StackPanel { Spacing = 0 };
        var diffViewport = new ScrollViewer { Content = diffPreview, Height = 260, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, BorderThickness = new Thickness(1), BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) };
        var fullFiles = new StackPanel { Spacing = 6, Visibility = Visibility.Collapsed };
        var fullFilesButton = new Button { Content = "Show full files", HorizontalAlignment = HorizontalAlignment.Left };
        fullFilesButton.Click += (_, _) =>
        {
            var show = fullFiles.Visibility == Visibility.Collapsed;
            fullFiles.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            fullFilesButton.Content = show ? "Hide full files" : "Show full files";
        };
        fullFiles.Children.Add(currentHeading);
        fullFiles.Children.Add(current);
        fullFiles.Children.Add(new TextBlock { Text = "Proposed replacement", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        fullFiles.Children.Add(proposed);

        void RefreshDiff()
        {
            diffPreview.Children.Clear();
            var diff = WorkspaceTextDiff.Create(currentText, proposal.Content);
            diffSummary.Text = diff.IsTooLarge
                ? $"Inline preview is limited to {WorkspaceTextDiff.MaxComparableLines:N0} lines per file. Compare the full files below."
                : diff.HasTextChanges
                    ? $"{diff.BeforeLineCount:N0} → {diff.AfterLineCount:N0} lines · +{diff.AddedLineCount:N0} · −{diff.RemovedLineCount:N0}"
                    : "No text changes detected.";
            if (diff.IsTooLarge) return;
            foreach (var line in diff.Lines)
            {
                var (prefix, foreground, background) = line.Kind switch
                {
                    WorkspaceDiffLineKind.Added => ("+", Microsoft.UI.Colors.DarkGreen, Microsoft.UI.Colors.Honeydew),
                    WorkspaceDiffLineKind.Removed => ("−", Microsoft.UI.Colors.DarkRed, Microsoft.UI.Colors.MistyRose),
                    WorkspaceDiffLineKind.Metadata => ("!", Microsoft.UI.Colors.DarkGoldenrod, Microsoft.UI.Colors.Transparent),
                    WorkspaceDiffLineKind.Omitted => ("…", Microsoft.UI.Colors.Gray, Microsoft.UI.Colors.Transparent),
                    _ => (" ", Microsoft.UI.Colors.DimGray, Microsoft.UI.Colors.Transparent)
                };
                var numbers = line.Kind switch
                {
                    WorkspaceDiffLineKind.Added => $"     {line.NewLine,4} ",
                    WorkspaceDiffLineKind.Removed => $"{line.OldLine,4}      ",
                    WorkspaceDiffLineKind.Context => $"{line.OldLine,4} {line.NewLine,4} ",
                    _ => "           "
                };
                var body = line.Text.Length > 500 ? line.Text[..500] + " … [line preview shortened]" : line.Text;
                diffPreview.Children.Add(new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(background),
                    Child = new TextBlock
                    {
                        Text = line.Kind == WorkspaceDiffLineKind.Omitted || line.Kind == WorkspaceDiffLineKind.Metadata
                            ? $" {prefix} {line.Text}"
                            : $"{numbers}{prefix} {body}",
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(foreground),
                        TextWrapping = TextWrapping.NoWrap,
                        Padding = new Thickness(4, 1, 4, 1)
                    }
                });
            }
        }
        RefreshDiff();
        string? reviewError = null;
        ContentDialog? dialog = null;
        rootPicker.SelectionChanged += (_, _) =>
        {
            if (rootPicker.SelectedIndex < 0) return;
            try
            {
                var selectedRoot = project.FolderPaths[rootPicker.SelectedIndex];
                expectedHash = _workspaceEditor.GetCurrentHash(selectedRoot, proposal.RelativePath);
                existing = expectedHash is not null;
                currentHeading.Text = existing ? "Current file" : "Current file · does not exist yet";
                currentText = existing ? _workspaceEditor.ReadText(selectedRoot, proposal.RelativePath) : "";
                current.Text = existing ? currentText : "(New file)";
                RefreshDiff();
                if (dialog is not null)
                {
                    dialog.Title = existing ? $"Review replacement · {proposal.RelativePath}" : $"Review new file · {proposal.RelativePath}";
                    dialog.PrimaryButtonText = existing ? "Replace file" : "Create file";
                }
                reviewError = null;
            }
            catch (Exception ex)
            {
                expectedHash = null;
                reviewError = ex.Message;
                existing = false;
                currentHeading.Text = "Current file · cannot be read safely";
                currentText = "This path cannot be reviewed safely.";
                current.Text = currentText;
                RefreshDiff();
            }
        };
        var content = new StackPanel { Spacing = 9 };
        content.Children.Add(new TextBlock { Text = "Choose where this relative path should be applied. Nothing is written until you approve below.", TextWrapping = TextWrapping.Wrap });
        content.Children.Add(rootPicker);
        content.Children.Add(new TextBlock { Text = "Proposed file changes", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        content.Children.Add(diffSummary);
        content.Children.Add(diffViewport);
        content.Children.Add(fullFilesButton);
        content.Children.Add(fullFiles);
        dialog = new ContentDialog
        {
            Title = existing ? $"Review replacement · {proposal.RelativePath}" : $"Review new file · {proposal.RelativePath}",
            Content = content,
            PrimaryButtonText = existing ? "Replace file" : "Create file",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || rootPicker.SelectedIndex < 0) return;
        if (reviewError is not null) { RuntimeStatus.Text = $"File change cannot be applied: {reviewError}"; return; }
        root = project.FolderPaths[rootPicker.SelectedIndex];
        try
        {
            var receipt = _workspaceEditor.Apply(root, proposal.RelativePath, proposal.Content, expectedHash);
            var undoEntry = RecordWorkspaceUndo(project.Name, root, proposal.RelativePath, receipt);
            if (_lastUndoButton is not null) _lastUndoButton.IsEnabled = false;
            _lastEditReceipt = receipt;
            var undo = new Button { Content = $"Undo · {proposal.RelativePath}", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0) };
            undo.Click += (_, _) =>
            {
                if (_lastEditReceipt != receipt) { undo.IsEnabled = false; return; }
                UndoWorkspaceEdit(undoEntry, undo);
            };
            _lastUndoButton = undo;
            messageStack.Children.Add(new TextBlock { Text = $"Saved {proposal.RelativePath} to {Path.GetFullPath(root)}", TextWrapping = TextWrapping.Wrap, Opacity = 0.78, Margin = new Thickness(0, 6, 0, 0) });
            messageStack.Children.Add(undo);
            RuntimeStatus.Text = _persistedUndoIds.Contains(undoEntry.Id)
                ? $"Approved change saved · {proposal.RelativePath}"
                : $"Approved change saved · {proposal.RelativePath} · undo is available only while this app remains open";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"File was not changed: {ex.Message}"; }
    }
}
