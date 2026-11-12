// Sidebar glue for MainPage. The sidebar itself is SidebarView (SidebarView.xaml); MainPage keeps
// using the old element names through the accessor properties below, so feature files didn't
// change when the sidebar moved. Also holds the sidebar-only actions added on main: Scheduled
// tasks (placeholder), More (projects, search, Code Review Agent, health scan, settings), Update.
// Depends on: SidebarView, OpenCodeReviewAgentAsync (MainPage.CodeReview.cs), ShowLocalHealthAsync
// (MainPage.ProjectHealth.cs). Called by: SidebarView events wired in the MainPage constructor.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Services;
using Ellipse = Microsoft.UI.Xaml.Shapes.Ellipse;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private FrameworkElement Sidebar => SidebarView;
    private Button ChatModeButton => SidebarView.ChatModeButtonControl;
    private Button CodingModeButton => SidebarView.CodingModeButtonControl;
    private Button NewConversationButton => SidebarView.NewConversationButtonControl;
    private Button NewProjectButton => SidebarView.NewProjectButtonControl;
    private Button GitHubButton => SidebarView.GitHubButtonControl;
    private StackPanel CodingNavigationSection => SidebarView.CodingNavigationSectionControl;
    private StackPanel ProjectList => SidebarView.ProjectListControl;
    private TextBlock CodeFilesHeader => SidebarView.CodeFilesHeaderControl;
    private StackPanel ProjectFileList => SidebarView.ProjectFileListControl;
    private StackPanel ChatFolderList => SidebarView.ChatFolderListControl;
    private StackPanel RecentList => SidebarView.RecentListControl;
    private Ellipse ConnectionDot => SidebarView.ConnectionDotControl;
    private TextBlock ConnectionLabel => SidebarView.ConnectionLabelControl;
    private Button CompanionButton => SidebarView.CompanionButtonControl;
    private Button ImportChatsButton => SidebarView.ImportChatsButtonControl;
    private Button ExportChatsButton => SidebarView.ExportChatsButtonControl;
    private TextBlock NewConversationLabel => SidebarView.NewConversationLabelControl;
    private TextBlock CompanionButtonLabel => SidebarView.CompanionButtonLabelControl;

    private async void ScheduledTasksClick(object sender, RoutedEventArgs e)
    {
        OperationJournalSnapshot snapshot;
        WorkflowRunSnapshot workflowSnapshot;
        IReadOnlyList<OperationRun> reconciled;
        try
        {
            reconciled = _operationService.ReconcileAfterRestart();
            snapshot = _operationService.Load();
            workflowSnapshot = _workflowRunService.Load();
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Activity history is unavailable: {ex.Message}";
            return;
        }

        var content = new StackPanel { Spacing = 10, MaxWidth = 680 };
        ContentDialog? dialog = null;
        if (reconciled.Count > 0)
        {
            content.Children.Add(new Border
            {
                Background = ResourceBrush("WarningBrush"),
                BorderBrush = ResourceBrush("WarningLineBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Child = new TextBlock
                {
                    Text = $"{reconciled.Count} interrupted operation{(reconciled.Count == 1 ? " was" : "s were")} marked needs reconciliation. No provider, shell, Git, or file action was replayed.",
                    TextWrapping = TextWrapping.Wrap
                }
            });
            snapshot = _operationService.Load();
        }

        if (snapshot.Runs.Count == 0)
        {
            content.Children.Add(new TextBlock { Text = "No operations have been recorded yet. Chat and Coding Agent work will appear here as they run.", TextWrapping = TextWrapping.Wrap });
        }
        else
        {
            foreach (var run in snapshot.Runs.OrderByDescending(item => item.UpdatedAt).Take(50))
            {
                var preview = run.UserInstruction.ReplaceLineEndings(" ").Trim();
                if (preview.Length > 140) preview = preview[..140] + "…";
                var runPanel = new StackPanel
                {
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock { Text = $"{OperationStatusLabel(run.Status)} · {run.UpdatedAt.ToLocalTime():g}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        new TextBlock { Text = preview, TextWrapping = TextWrapping.Wrap },
                        new TextBlock { Text = $"Run {run.RunId[..Math.Min(8, run.RunId.Length)]} · sequence {run.LastEventSequence}", FontSize = 11, Opacity = 0.7 }
                    }
                };
                if (run.Status == OperationRunStatuses.NeedsReconciliation)
                {
                    var decisions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 6, 0, 0) };
                    var cancel = new Button { Content = "Mark cancelled", Style = (Style?)Application.Current.Resources["ContextButtonStyle"] };
                    var failed = new Button { Content = "Mark failed", Style = (Style?)Application.Current.Resources["ContextButtonStyle"] };
                    AutomationProperties.SetName(cancel, "Mark interrupted operation cancelled");
                    AutomationProperties.SetName(failed, "Mark interrupted operation failed");
                    cancel.Click += (_, _) => ResolveReconciledOperation(run, markCancelled: true, dialog);
                    failed.Click += (_, _) => ResolveReconciledOperation(run, markCancelled: false, dialog);
                    decisions.Children.Add(cancel);
                    decisions.Children.Add(failed);
                    runPanel.Children.Add(decisions);
                }
                content.Children.Add(new Border
                {
                    Background = ResourceBrush("SurfaceAltBrush"),
                    BorderBrush = ResourceBrush("LineBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Child = runPanel
                });
            }
        }

        if (workflowSnapshot.Runs.Count > 0)
        {
            content.Children.Add(new TextBlock { Text = "WORKFLOW RUNS", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Opacity = 0.75, Margin = new Thickness(0, 8, 0, 0) });
            foreach (var run in workflowSnapshot.Runs.OrderByDescending(item => item.UpdatedAt).Take(50))
            {
                var runPanel = new StackPanel { Spacing = 3 };
                runPanel.Children.Add(new TextBlock { Text = $"{WorkflowStatusLabel(run.Status)} · v{run.WorkflowVersion} · {run.UpdatedAt.ToLocalTime():g}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                runPanel.Children.Add(new TextBlock { Text = $"Workflow {run.WorkflowId} · run {run.RunId[..Math.Min(8, run.RunId.Length)]}", FontSize = 11, Opacity = 0.7 });
                var previewPlan = new Button { Content = "Preview plan", Style = (Style?)Application.Current.Resources["ContextButtonStyle"], HorizontalAlignment = HorizontalAlignment.Left };
                AutomationProperties.SetName(previewPlan, $"Preview workflow plan for {run.WorkflowId}");
                previewPlan.Click += async (_, _) =>
                {
                    dialog?.Hide();
                    await ShowWorkflowPlanAsync(run);
                };
                runPanel.Children.Add(previewPlan);
                foreach (var step in run.Steps)
                {
                    var stepLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(8, 2, 0, 0) };
                    var dependencies = step.DependsOn.Count == 0 ? "root" : "after " + string.Join(", ", step.DependsOn);
                    var result = step.Result is null ? string.Empty : $" · result {step.Result}";
                    var capability = step.NodeType == WorkflowNodeTypes.Condition ? "local condition executor" : "executor unavailable";
                    stepLine.Children.Add(new TextBlock { Text = $"{step.Label} · {step.NodeType} · {step.Status} · {dependencies} · attempt {step.Attempt}{result}\n{capability}", TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
                    if (run.Status == WorkflowRunStatuses.AwaitingApproval && step.Status == WorkflowStepStatuses.WaitingApproval)
                    {
                        var approve = new Button { Content = "Approve", Style = (Style?)Application.Current.Resources["ContextButtonStyle"] };
                        AutomationProperties.SetName(approve, $"Approve workflow step {step.NodeId}");
                        approve.Click += (_, _) => ApproveWorkflowStep(run, step, dialog);
                        stepLine.Children.Add(approve);
                    }
                    runPanel.Children.Add(stepLine);
                }
                var events = workflowSnapshot.Events.Where(item => item.RunId == run.RunId).OrderBy(item => item.Sequence).ToArray();
                if (events.Length > 0)
                {
                    runPanel.Children.Add(new TextBlock { Text = "EFFECT TIMELINE", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Opacity = 0.7, Margin = new Thickness(8, 6, 0, 0) });
                    foreach (var workflowEvent in events.TakeLast(12))
                    {
                        runPanel.Children.Add(new TextBlock
                        {
                            Text = $"#{workflowEvent.Sequence} · {WorkflowEventLabel(workflowEvent)}",
                            FontSize = 11,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(8, 1, 0, 0)
                        });
                    }
                }
                content.Children.Add(new Border
                {
                    Background = ResourceBrush("SurfaceAltBrush"),
                    BorderBrush = ResourceBrush("LineBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Child = runPanel
                });
            }
        }

        dialog = new ContentDialog
        {
            Title = "Activity & tasks",
            Content = new ScrollViewer { Content = content, MaxHeight = 620, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void ResolveReconciledOperation(OperationRun run, bool markCancelled, ContentDialog? dialog)
    {
        try
        {
            _operationService.ResolveReconciliation(run, markCancelled);
            RuntimeStatus.Text = markCancelled
                ? "Interrupted operation marked cancelled after review. No side effect was replayed."
                : "Interrupted operation marked failed after review. No side effect was replayed.";
            dialog?.Hide();
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Could not resolve interrupted operation: {ex.Message}";
        }
    }

    private static Brush? ResourceBrush(string key) => Application.Current.Resources[key] as Brush;

    private static string OperationStatusLabel(string status) => status switch
    {
        OperationRunStatuses.NeedsReconciliation => "Needs review after restart",
        OperationRunStatuses.AwaitingApproval => "Waiting for approval",
        OperationRunStatuses.ReviewReady => "Ready for review",
        OperationRunStatuses.Completed => "Completed",
        OperationRunStatuses.Cancelled => "Cancelled",
        OperationRunStatuses.Failed => "Failed",
        OperationRunStatuses.Running => "Running",
        _ => status.Replace('_', ' ')
    };

    private static string WorkflowStatusLabel(string status) => status switch
    {
        WorkflowRunStatuses.NeedsReconciliation => "Needs review after restart",
        WorkflowRunStatuses.AwaitingApproval => "Waiting for approval",
        WorkflowRunStatuses.Completed => "Completed",
        WorkflowRunStatuses.Cancelled => "Cancelled",
        WorkflowRunStatuses.Failed => "Failed",
        WorkflowRunStatuses.Running => "Running",
        _ => status.Replace('_', ' ')
    };

    private static string WorkflowEventLabel(WorkflowRunEvent workflowEvent) => workflowEvent.Type switch
    {
        "workspace.read_completed" => $"Workspace read · {workflowEvent.Payload.GetValueOrDefault("relativePath", "file")} · {workflowEvent.Payload.GetValueOrDefault("charactersRead", "0")} chars · no mutation",
        "condition.evaluated" => $"Condition evaluated · {workflowEvent.Payload.GetValueOrDefault("nodeId", "step")} = {workflowEvent.Payload.GetValueOrDefault("result", "unknown")}",
        "approval.granted" => $"Approval recorded · {workflowEvent.Payload.GetValueOrDefault("nodeId", "step")}",
        "run.needs_reconciliation" => "Restart reconciliation required · no side effect replay",
        "run.completed" => "Run completed after verification",
        "run.cancelled" => "Run cancelled",
        _ => workflowEvent.Type.Replace('.', ' ')
    };

    private void ApproveWorkflowStep(WorkflowRun run, WorkflowStepRun step, ContentDialog? dialog)
    {
        try
        {
            _workflowRunService.ApproveStep(run.RunId, step.NodeId, $"activity-ui:{run.RunId}:{step.NodeId}");
            RuntimeStatus.Text = $"Approved workflow step {step.NodeId}. No tool or provider action was dispatched by this approval surface.";
            dialog?.Hide();
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Workflow approval could not be recorded: {ex.Message}";
        }
    }

    private async Task ShowWorkflowPlanAsync(WorkflowRun run)
    {
        var plan = new StackPanel { Spacing = 8, MaxWidth = 680 };
        plan.Children.Add(new TextBlock
        {
            Text = "Read-only dry run · nothing will execute",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = ResourceBrush("AccentInkBrush")
        });
        plan.Children.Add(new TextBlock
        {
            Text = "This preview explains the persisted dependency order and policy gates. It does not call a model, tool, shell, Git, network, or file operation.",
            TextWrapping = TextWrapping.Wrap
        });
        foreach (var (step, index) in run.Steps.Select((item, index) => (item, index + 1)))
        {
            var dependencies = step.DependsOn.Count == 0 ? "root step" : $"after {string.Join(", ", step.DependsOn)}";
            var policy = step.RequiresApproval ? "manual approval required" : step.SafeToRetry ? "safe retry eligible" : "retry requires review";
            plan.Children.Add(new Border
            {
                Background = ResourceBrush("SurfaceAltBrush"),
                BorderBrush = ResourceBrush("LineBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Child = new TextBlock
                {
                    Text = $"{index}. {step.Label} · {step.NodeType}\n{dependencies} · {policy}\nCurrent state: {step.Status} · attempt {step.Attempt}",
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }
        await new ContentDialog
        {
            Title = $"Workflow plan · {run.WorkflowId} v{run.WorkflowVersion}",
            Content = new ScrollViewer { Content = plan, MaxHeight = 620, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private async void MoreClick(object sender, RoutedEventArgs e)
    {
        var selected = string.Empty;
        ContentDialog? dialog = null;
        var actions = new StackPanel { Spacing = 6 };
        void AddAction(string label, string value, string description)
        {
            var button = new Button
            {
                Content = new StackPanel
                {
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock { Text = label, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        new TextBlock { Text = description, FontSize = 11, TextWrapping = TextWrapping.Wrap, Opacity = 0.75 }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 8, 10, 8),
                Tag = value
            };
            button.Click += (_, _) => { selected = value; dialog?.Hide(); };
            actions.Children.Add(button);
        }
        AddAction("Projects & folders", "projects", "Create a project, choose local folders, and manage project context.");
        AddAction("Search conversations", "search", "Find saved chats and coding sessions.");
        AddAction("Code Review Agent", "review", "Review a folder, ZIP, or GitHub repository with your selected model.");
        AddAction("Health scan & architecture map", "health", "Run a bounded offline project scan without uploading repository contents.");
        AddAction("Connection & settings", "settings", "Configure model endpoints, privacy, voice, and local preferences.");
        dialog = new ContentDialog { Title = "More workspace options", Content = actions, CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        await dialog.ShowAsync();
        switch (selected)
        {
            case "projects": NewProjectClick(this, new RoutedEventArgs()); break;
            case "search": SearchClick(this, new RoutedEventArgs()); break;
            case "review": await OpenCodeReviewAgentAsync(); break;
            case "health": await ShowLocalHealthAsync(includeMap: false); break;
            case "settings": SettingsClick(this, new RoutedEventArgs()); break;
        }
    }

    private async void UpdateClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Server Rock AI update",
            Content = new TextBlock { Text = "A newer app build is available. Open Settings to review the signed update channel and choose when to install it.", TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = "Open settings",
            CloseButtonText = "Later",
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) SettingsClick(this, new RoutedEventArgs());
    }
}
