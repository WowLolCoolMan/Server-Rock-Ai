// Sidebar glue for MainPage. The sidebar itself is SidebarView (SidebarView.xaml); MainPage keeps
// using the old element names through the accessor properties below, so feature files didn't
// change when the sidebar moved. Also holds the sidebar-only actions added on main: Scheduled
// tasks (placeholder), More (projects, search, Code Review Agent, health scan, settings), Update.
// Depends on: SidebarView, OpenCodeReviewAgentAsync (MainPage.CodeReview.cs), ShowLocalHealthAsync
// (MainPage.ProjectHealth.cs). Called by: SidebarView events wired in the MainPage constructor.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        var dialog = new ContentDialog
        {
            Title = "Scheduled tasks",
            Content = new TextBlock { Text = "Scheduled and recurring task controls are being prepared for this workspace. Nothing runs in the background yet.", TextWrapping = TextWrapping.Wrap },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
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
