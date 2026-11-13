// Local reusable personas (also called Gems): explicit save and activation, bounded by settings limits.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task OpenPersonasAsync()
    {
        var name = new TextBox { Header = "Persona name", PlaceholderText = "e.g. Careful code reviewer", MaxLength = 80 };
        var instructions = new TextBox { Header = "Role instructions", PlaceholderText = "Describe how the model should help…", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 130, MaxHeight = 280, MaxLength = 4000 };
        var list = new ListView { MaxHeight = 220, SelectionMode = ListViewSelectionMode.Single };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = UiTheme.Brush("MutedInkBrush") };
        AutomationProperties.SetName(name, "Persona name");
        AutomationProperties.SetName(instructions, "Persona instructions");
        AutomationProperties.SetName(list, "Saved personas");
        AutomationProperties.SetName(status, "Persona status");
        ContentDialog? dialog = null;
        SavedPersona? selected = null;

        void RefreshList()
        {
            list.Items.Clear();
            foreach (var persona in _settings.SavedPersonas.OrderByDescending(item => item.UpdatedAtUtc).Take(24))
            {
                var label = persona.Id == _settings.ActivePersonaId ? $"{persona.Name} (active)" : persona.Name;
                list.Items.Add(new ListViewItem { Content = label, Tag = persona });
            }
            list.SelectedIndex = -1;
        }

        list.SelectionChanged += (_, _) =>
        {
            selected = (list.SelectedItem as ListViewItem)?.Tag as SavedPersona;
            if (selected is null) return;
            name.Text = selected.Name;
            instructions.Text = selected.Instructions;
            status.Text = selected.Id == _settings.ActivePersonaId ? "Selected active persona. Changes require Save." : "Selected locally saved persona.";
        };

        var newPersona = new Button { Content = "New" };
        newPersona.Click += (_, _) => { selected = null; list.SelectedIndex = -1; name.Text = string.Empty; instructions.Text = string.Empty; status.Text = "Enter a name and role instructions, then save locally."; };
        var save = new Button { Content = "Save" };
        save.Click += (_, _) =>
        {
            var cleanName = name.Text.Trim();
            var cleanInstructions = instructions.Text.Trim();
            if (cleanName.Length is < 1 or > 80 || cleanInstructions.Length is < 1 or > 4000) { status.Text = "Add a name and between 1 and 4,000 characters of role instructions."; return; }
            if (selected is null)
            {
                if (_settings.SavedPersonas.Count >= 24) { status.Text = "The local persona limit is 24. Delete or reuse a persona before adding another."; return; }
                selected = new SavedPersona();
                _settings.SavedPersonas.Add(selected);
            }
            selected.Name = cleanName;
            selected.Instructions = cleanInstructions;
            selected.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveSettings();
            RefreshList();
            status.Text = $"Saved locally: {selected.Name}.";
        };
        var activate = new Button { Content = "Activate" };
        activate.Click += (_, _) =>
        {
            if (selected is null) { status.Text = "Select a persona first."; return; }
            _settings.ActivePersonaId = selected.Id;
            SaveSettings();
            RefreshList();
            status.Text = $"Active persona: {selected.Name}. It will be included in future chat requests.";
        };
        var deactivate = new Button { Content = "Deactivate" };
        deactivate.Click += (_, _) =>
        {
            _settings.ActivePersonaId = null;
            SaveSettings();
            RefreshList();
            status.Text = "No persona is active. Existing chats are unchanged.";
        };
        var delete = new Button { Content = "Delete" };
        delete.Click += (_, _) =>
        {
            if (selected is null) { status.Text = "Select a persona first."; return; }
            if (_settings.ActivePersonaId == selected.Id) _settings.ActivePersonaId = null;
            _settings.SavedPersonas.Remove(selected);
            selected = null;
            SaveSettings();
            RefreshList();
            name.Text = string.Empty;
            instructions.Text = string.Empty;
            status.Text = "Persona deleted from local settings.";
        };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7, Children = { newPersona, save, activate, deactivate, delete } };
        var content = new StackPanel { Spacing = 10, Children =
        {
            new TextBlock { Text = "Personas are local reusable role instructions. Activation affects future provider requests only; it never changes system or safety instructions.", TextWrapping = TextWrapping.Wrap, Opacity = 0.82 },
            list,
            name,
            instructions,
            actions,
            status
        } };
        dialog = new ContentDialog { Title = "Personas", Content = content, CloseButtonText = "Done", XamlRoot = XamlRoot };
        RefreshList();
        await dialog.ShowAsync();
    }
}
