// Slash commands typed in the composer (/help, /model, /new, /mode, /web, …).
// The command list and suggestion filter live in Core (SlashCommandCatalog); PromptTextChanged uses it as
// you type, and ExecuteSlashCommandAsync runs a command when the prompt is sent.
// Also holds the small handlers only reached from commands: /help, /status, /research,
// /reverse-engineer.
// Depends on: most feature files (each command calls into its feature).
// Called by: PromptBox TextChanged and the / button in MainPage.xaml, PromptKeyDown (Up/Down/Enter
// in the popup), and SendClick (prompts that start with /).

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private void CommandsClick(object sender, RoutedEventArgs e)
    {
        PromptBox.Text = "/";
        PromptBox.Focus(FocusState.Programmatic);
    }

    private void PromptTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = PromptBox.Text;
        CommandSuggestions.Children.Clear();
        _selectedSlashSuggestion = -1;
        if (!text.StartsWith('/'))
        {
            CommandHintPanel.Visibility = Visibility.Collapsed;
            return;
        }
        var firstSpace = text.IndexOf(' ');
        var commandName = firstSpace < 0 ? text : text[..firstSpace];
        var argumentPrefix = firstSpace < 0 ? null : text[(firstSpace + 1)..].TrimStart();
        var matches = SlashCommandCatalog.Suggest(commandName, argumentPrefix);
        if (matches.Length == 0)
        {
            CommandSuggestions.Children.Add(new TextBlock { Text = "No matching commands or options", Foreground = UiTheme.Brush("MutedInkBrush"), Margin = new Thickness(8, 6, 8, 6) });
        }
        foreach (var (name, description) in matches)
        {
            var button = new Button
            {
                Tag = name,
                // Rows span the full popup width (they used to be ragged) and start untinted;
                // the keyboard-selected row gets the selected state (UiTheme.ApplyNavItemState).
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 7, 10, 7),
                Content = new StackPanel { Spacing = 2, Children =
                {
                    new TextBlock { Text = name, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = UiTheme.Brush("InkBrush") },
                    new TextBlock { Text = description, FontSize = UiTheme.FontSize("CaptionFontSize"), Foreground = UiTheme.Brush("MutedInkBrush") }
                } }
            };
            AutomationProperties.SetName(button, name);
            AutomationProperties.SetHelpText(button, description);
            button.Click += SlashSuggestionClick;
            CommandSuggestions.Children.Add(button);
        }
        CommandHintPanel.Visibility = Visibility.Visible;
    }

    private void SlashSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string command }) return;
        PromptBox.Text = command + (command.Contains(' ') ? string.Empty : " ");
        PromptBox.Focus(FocusState.Programmatic);
        PromptBox.SelectionStart = PromptBox.Text.Length;
        CommandHintPanel.Visibility = Visibility.Collapsed;
    }

    private async Task ExecuteSlashCommandAsync(string commandText)
    {
        var command = commandText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
        PromptBox.Text = string.Empty;
        switch (command)
        {
            case "/help": await HelpClickAsync(); break;
            case "/model": await ModelClickAsync(); break;
            case "/new": NewChatClick(this, new RoutedEventArgs()); break;
            case "/clear": NewChatClick(this, new RoutedEventArgs()); break;
            case "/search": await SearchClickAsync(); break;
            case "/folder": await ChooseCurrentChatFolderAsync(); break;
            case "/rename":
                if (_activeSessionId is null || _isTemporaryChat) RuntimeStatus.Text = "Save a conversation before renaming it.";
                else await RenameSessionAsync(_activeSessionId);
                break;
            case "/delete":
                if (_activeSessionId is null || _isTemporaryChat) RuntimeStatus.Text = "Save a conversation before deleting it.";
                else await DeleteSessionAsync(_activeSessionId);
                break;
            case "/project": await ChooseProjectAsync(); break;
            case "/reverse-engineer": case "/reserse-engineer": await ReverseEngineerAsync(); break;
            case "/health": case "/audit": await ShowLocalHealthAsync(includeMap: false); break;
            case "/map": await ShowLocalHealthAsync(includeMap: true); break;
            case "/plugins": await PluginStoreClickAsync(); break;
            case "/skills": await SkillsDialogAsync(); break;
            case "/research": EnableResearchMode(); break;
            case "/tools": ComposerToolsClick(this, new RoutedEventArgs()); break;
            case "/status": ShowWorkspaceStatus(); break;
            case "/pet": ToggleCompanionClick(this, new RoutedEventArgs()); break;
            case "/newproject": await NewProjectClickAsync(); break;
            case "/folders": await ProjectFoldersClickAsync(); break;
            case "/settings": case "/connect": await SettingsDialogAsync(); break;
            case "/temporary":
            {
                var argument = commandText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
                if (argument is null) await SetTemporaryModeAsync(!_isTemporaryChat);
                else if (argument.Equals("on", StringComparison.OrdinalIgnoreCase)) await SetTemporaryModeAsync(true);
                else if (argument.Equals("off", StringComparison.OrdinalIgnoreCase)) await SetTemporaryModeAsync(false);
                else RuntimeStatus.Text = "Use /temporary on or /temporary off.";
                break;
            }
            case "/mode":
                var mode = commandText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
                if (mode?.Equals("code", StringComparison.OrdinalIgnoreCase) == true || mode?.Equals("coding", StringComparison.OrdinalIgnoreCase) == true) await SwitchModeAsync(WorkspaceModes.CodingAgent);
                else if (mode?.Equals("chat", StringComparison.OrdinalIgnoreCase) == true) await SwitchModeAsync(WorkspaceModes.Chat);
                else RuntimeStatus.Text = "Use /mode chat or /mode code.";
                break;
            case "/web":
                if (_mode != WorkspaceModes.Chat) RuntimeStatus.Text = "Web search is available in the Chat workspace.";
                else
                {
                    var argument = commandText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
                    _settings.WebSearchEnabled = argument?.Equals("on", StringComparison.OrdinalIgnoreCase) == true ||
                        (argument is null && !_settings.WebSearchEnabled);
                    if (argument?.Equals("off", StringComparison.OrdinalIgnoreCase) == true) _settings.WebSearchEnabled = false;
                    SaveSettings();
                    UpdateWebSearchButton();
                    RuntimeStatus.Text = _settings.WebSearchEnabled
                        ? "Web search on · query goes to DuckDuckGo; result snippets go to the selected model."
                        : "Web search off · prompts go only to the selected model server.";
                }
                break;
            default: RuntimeStatus.Text = "Unknown command. Type / to see available commands."; break;
        }
        PromptBox.Focus(FocusState.Programmatic);
    }

    private async Task ReverseEngineerAsync()
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        if (project is null || project.FolderPaths.Count == 0)
        {
            RuntimeStatus.Text = "Choose a project with at least one local folder before running reverse engineering.";
            await ChooseProjectAsync();
            project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
            if (project is null || project.FolderPaths.Count == 0) return;
        }
        if (_mode != WorkspaceModes.CodingAgent) await SwitchModeAsync(WorkspaceModes.CodingAgent);
        PromptBox.Text = "Reverse-engineer this project as a senior software archaeologist. Inspect the selected project files and produce a clear report with: (1) system purpose and user workflows, (2) architecture and module boundaries, (3) runtime/data flow, (4) persistence and external integrations, (5) entry points and important commands, (6) testing and release posture, (7) risks, inconsistencies, and likely failure points, and (8) a prioritized improvement plan. Cite relevant file paths and distinguish observed facts from reasonable inferences. Do not modify files or run destructive commands.";
        PromptBox.Focus(FocusState.Programmatic);
        SendClick(this, new RoutedEventArgs());
    }

    private void EnableResearchMode()
    {
        if (_mode != WorkspaceModes.Chat)
        {
            RuntimeStatus.Text = "Research mode is available in ChatBot. Switch to ChatBot before researching.";
            return;
        }
        _settings.WebSearchEnabled = true;
        SaveSettings();
        UpdateWebSearchButton();
        RuntimeStatus.Text = "Research mode on · the next Chat prompt will include bounded web search snippets and source links.";
    }

    private void ShowWorkspaceStatus()
    {
        var project = _projects.FirstOrDefault(item => item.Id == _activeProjectId);
        var projectLabel = project is null ? "No project" : $"Project: {project.Name} ({project.FolderPaths.Count} folder(s))";
        RuntimeStatus.Text = $"{_settings.HostLabel} · {_mode} · {_settings.ModelProvider} · {_settings.Model} · {projectLabel} · Web research {(_settings.WebSearchEnabled ? "on" : "off")} · {( _settings.RequireLocalModelEndpoints ? "local-only" : "remote endpoints allowed")}";
    }

    private async Task HelpClickAsync()
    {
        await new ContentDialog
        {
            Title = _mode == WorkspaceModes.CodingAgent ? "Using Coding Agent" : "Using Chat",
            Content = _mode == WorkspaceModes.CodingAgent
                ? "Select a project and folders from the left. The agent reads a bounded set of text files. Ask for a change, inspect the proposed current and replacement contents, then approve or cancel each file. Approved file changes are saved only after review and can be undone from Undo history if no newer edit has changed them. Proposed commands require your review before they run. Use Terminal for an interactive shell in the selected project."
                : "Enter a prompt and press Enter to send; Shift+Enter adds a line. Type / to browse commands, use Up and Down to choose, and press Enter to complete one. Commands such as /web and /mode show their available options as you type. Web search is off by default. Temporary chats stay in memory and are not saved to Recents or local history.",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }
}
