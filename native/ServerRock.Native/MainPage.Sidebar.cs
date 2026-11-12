// Sidebar glue for MainPage. The sidebar itself is SidebarView (SidebarView.xaml); MainPage keeps
// using the old element names through the accessor properties below, so feature files didn't
// change when the sidebar moved. Also holds the sidebar-only actions added on main: Scheduled
// tasks (placeholder), More (projects, search, Code Review Agent, health scan, settings), Update.
// Depends on: SidebarView, OpenCodeReviewAgentAsync (MainPage.CodeReview.cs), ShowLocalHealthAsync
// (MainPage.ProjectHealth.cs). Called by: SidebarView events wired in the MainPage constructor.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Infrastructure;
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
        IReadOnlyList<OperationRun> reconciled;
        try
        {
            reconciled = _operationService.ReconcileAfterRestart();
            snapshot = _operationService.Load();
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Activity history is unavailable: {ex.Message}";
            return;
        }

        var content = new StackPanel { Spacing = 10, MaxWidth = 680 };
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
                content.Children.Add(new Border
                {
                    Background = ResourceBrush("SurfaceAltBrush"),
                    BorderBrush = ResourceBrush("LineBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Child = new StackPanel
                    {
                        Spacing = 3,
                        Children =
                        {
                            new TextBlock { Text = $"{OperationStatusLabel(run.Status)} · {run.UpdatedAt.ToLocalTime():g}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                            new TextBlock { Text = preview, TextWrapping = TextWrapping.Wrap },
                            new TextBlock { Text = $"Run {run.RunId[..Math.Min(8, run.RunId.Length)]} · sequence {run.LastEventSequence}", FontSize = 11, Opacity = 0.7 }
                        }
                    }
                });
            }
        }

        var dialog = new ContentDialog
        {
            Title = "Activity & tasks",
            Content = new ScrollViewer { Content = content, MaxHeight = 620, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
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
