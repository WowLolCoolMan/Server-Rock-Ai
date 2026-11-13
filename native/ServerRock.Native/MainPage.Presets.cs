using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SkaiDesktop.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task OpenPresetsAsync()
    {
        var name = new TextBox { Header = "Preset name", PlaceholderText = "e.g. Project launch reviewer", MaxLength = 80 };
        var instructions = new TextBox { Header = "Instructions", PlaceholderText = "How should the model approach this kind of work?", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 100, MaxHeight = 220, MaxLength = 4000 };
        var knowledge = new TextBox { Header = "Reference knowledge (optional)", PlaceholderText = "Stable local facts, terminology, or project conventions…", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 80, MaxHeight = 180, MaxLength = 8000 };
        var tools = new TextBox { Header = "Tool guidance (optional)", PlaceholderText = "Preferred tools or workflow order; this does not grant permissions.", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 70, MaxHeight = 160, MaxLength = 2000 };
        var list = new ListView { MaxHeight = 180, SelectionMode = ListViewSelectionMode.Single };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = UiTheme.Brush("MutedInkBrush") };
        AutomationProperties.SetName(name, "Preset name"); AutomationProperties.SetName(instructions, "Preset instructions"); AutomationProperties.SetName(knowledge, "Preset reference knowledge"); AutomationProperties.SetName(tools, "Preset tool guidance"); AutomationProperties.SetName(list, "Saved AI presets"); AutomationProperties.SetName(status, "Preset status");
        SavedPreset? selected = null;
        void RefreshList()
        {
            list.Items.Clear();
            foreach (var preset in _settings.SavedPresets.OrderByDescending(item => item.UpdatedAtUtc).Take(12))
                list.Items.Add(new ListViewItem { Content = preset.Id == _settings.ActivePresetId ? $"{preset.Name} (active)" : preset.Name, Tag = preset });
            list.SelectedIndex = -1;
        }
        list.SelectionChanged += (_, _) =>
        {
            selected = (list.SelectedItem as ListViewItem)?.Tag as SavedPreset;
            if (selected is null) return;
            name.Text = selected.Name; instructions.Text = selected.Instructions; knowledge.Text = selected.Knowledge; tools.Text = selected.ToolGuidance;
            status.Text = selected.Id == _settings.ActivePresetId ? "Selected active preset. Changes require Save." : "Selected locally saved preset.";
        };
        var newPreset = new Button { Content = "New" };
        newPreset.Click += (_, _) => { selected = null; list.SelectedIndex = -1; name.Text = instructions.Text = knowledge.Text = tools.Text = string.Empty; status.Text = "Enter a name, instructions, and optional reference material."; };
        var save = new Button { Content = "Save" };
        save.Click += (_, _) =>
        {
            var cleanName = name.Text.Trim(); var cleanInstructions = instructions.Text.Trim(); var cleanKnowledge = knowledge.Text.Trim(); var cleanTools = tools.Text.Trim();
            if (cleanName.Length is < 1 or > 80 || cleanInstructions.Length is < 1 or > 4000) { status.Text = "Add a name and between 1 and 4,000 characters of instructions."; return; }
            if (selected is null)
            {
                if (_settings.SavedPresets.Count >= 12) { status.Text = "The local preset limit is 12. Delete or reuse a preset first."; return; }
                selected = new SavedPreset(); _settings.SavedPresets.Add(selected);
            }
            selected.Name = cleanName; selected.Instructions = cleanInstructions; selected.Knowledge = cleanKnowledge; selected.ToolGuidance = cleanTools; selected.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveSettings(); RefreshList(); status.Text = $"Saved locally: {selected.Name}.";
        };
        var activate = new Button { Content = "Activate" };
        activate.Click += (_, _) => { if (selected is null) { status.Text = "Select a preset first."; return; } _settings.ActivePresetId = selected.Id; SaveSettings(); RefreshList(); status.Text = $"Active preset: {selected.Name}. It will be included in future chat requests."; };
        var deactivate = new Button { Content = "Deactivate" };
        deactivate.Click += (_, _) => { _settings.ActivePresetId = null; SaveSettings(); RefreshList(); status.Text = "No AI preset is active. Existing chats are unchanged."; };
        var delete = new Button { Content = "Delete" };
        delete.Click += (_, _) => { if (selected is null) { status.Text = "Select a preset first."; return; } if (_settings.ActivePresetId == selected.Id) _settings.ActivePresetId = null; _settings.SavedPresets.Remove(selected); selected = null; SaveSettings(); RefreshList(); name.Text = instructions.Text = knowledge.Text = tools.Text = string.Empty; status.Text = "Preset deleted from local settings."; };
        var content = new StackPanel { Spacing = 10, Children = { new TextBlock { Text = "Presets bundle reusable instructions, local reference knowledge, and tool guidance. They stay on this device and activate only for future requests; tool guidance never grants permissions.", TextWrapping = TextWrapping.Wrap, Opacity = 0.82 }, list, name, instructions, knowledge, tools, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7, Children = { newPreset, save, activate, deactivate, delete } }, status } };
        var dialog = new ContentDialog { Title = "AI presets", Content = content, CloseButtonText = "Done", XamlRoot = XamlRoot };
        RefreshList(); await dialog.ShowAsync();
    }
}
