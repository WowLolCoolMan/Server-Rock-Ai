// Local prompt snippets: bounded save/insert plus explicit model expansion preview.
// Expansion never sends the result automatically and never changes a saved snippet without
// an explicit Save action.

using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task OpenSnippetsAsync()
    {
        var title = new TextBox { Header = "Snippet title", PlaceholderText = "A short reusable name", MaxLength = 80 };
        var text = new TextBox { Header = "Snippet text", PlaceholderText = "Reusable prompt text", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 120, MaxHeight = 260, MaxLength = 4000 };
        var list = new ListView { MaxHeight = 220, SelectionMode = ListViewSelectionMode.Single };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = UiTheme.Brush("MutedInkBrush") };
        AutomationProperties.SetName(title, "Snippet title");
        AutomationProperties.SetName(text, "Snippet text");
        AutomationProperties.SetName(list, "Saved prompt snippets");
        AutomationProperties.SetName(status, "Snippet status");
        ContentDialog? dialog = null;
        PromptSnippet? selected = null;

        void RefreshList()
        {
            list.Items.Clear();
            foreach (var snippet in _settings.PromptSnippets.OrderByDescending(item => item.UpdatedAtUtc).Take(24))
                list.Items.Add(new ListViewItem { Content = snippet.Title, Tag = snippet });
            list.SelectedIndex = -1;
        }

        list.SelectionChanged += (_, _) =>
        {
            selected = (list.SelectedItem as ListViewItem)?.Tag as PromptSnippet;
            if (selected is null) return;
            title.Text = selected.Title;
            text.Text = selected.Text;
            status.Text = "Selected locally saved snippet. Review it before inserting or expanding.";
        };

        var newSnippet = new Button { Content = "New" };
        newSnippet.Click += (_, _) => { selected = null; list.SelectedIndex = -1; title.Text = string.Empty; text.Text = string.Empty; status.Text = "Enter a title and text, then save the snippet locally."; };
        var save = new Button { Content = "Save" };
        save.Click += (_, _) =>
        {
            var cleanTitle = title.Text.Trim();
            var cleanText = text.Text.Trim();
            if (cleanTitle.Length is < 1 or > 80 || cleanText.Length is < 1 or > 4000) { status.Text = "Add a title and between 1 and 4,000 characters of snippet text."; return; }
            if (selected is null)
            {
                if (_settings.PromptSnippets.Count >= 24) { status.Text = "The local snippet limit is 24. Delete or reuse a snippet before adding another."; return; }
                selected = new PromptSnippet();
                _settings.PromptSnippets.Add(selected);
            }
            selected.Title = cleanTitle;
            selected.Text = cleanText;
            selected.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveSettings();
            RefreshList();
            status.Text = $"Saved locally: {selected.Title}.";
        };
        var insert = new Button { Content = "Insert into composer" };
        insert.Click += (_, _) =>
        {
            var value = selected?.Text ?? text.Text.Trim();
            if (value.Length == 0) { status.Text = "Select or write a snippet first."; return; }
            PromptBox.Text = value;
            dialog?.Hide();
        };
        var expand = new Button { Content = "Expand with model" };
        expand.Click += async (_, _) => await ExpandSnippetAsync(selected?.Text ?? text.Text.Trim(), dialog, status);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7, Children = { newSnippet, save, insert, expand } };
        var content = new StackPanel { Spacing = 10, Children =
        {
            new TextBlock { Text = "Saved snippets stay on this device. Expansion is an explicit provider request and opens a review dialog before anything is inserted.", TextWrapping = TextWrapping.Wrap, Opacity = 0.82 },
            list,
            title,
            text,
            actions,
            status
        } };
        dialog = new ContentDialog { Title = "Prompt snippets", Content = content, CloseButtonText = "Done", XamlRoot = XamlRoot };
        RefreshList();
        await dialog.ShowAsync();
    }

    private async Task ExpandSnippetAsync(string source, ContentDialog? owner, TextBlock status)
    {
        if (source.Length is < 1 or > 4000) { status.Text = "Select or write between 1 and 4,000 characters before expanding."; return; }
        status.Text = "Requesting a reviewable expansion from the selected model…";
        var expanded = new StringBuilder();
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var instruction = "Expand this reusable prompt snippet into a clearer, more useful version. Preserve its intent. Return only the expanded prompt text, with no commentary.\n\nSnippet:\n" + source;
            await _provider.StreamChatAsync(_settings, [new ChatMessage("user", instruction)], chunk =>
            {
                if (expanded.Length + chunk.Length <= 24000) expanded.Append(chunk);
            }, cancellation.Token, maxTokens: 1024, apiKey: GetProviderApiKey());
            var candidate = expanded.ToString().Trim();
            if (candidate.Length is < 1 or > 24000) throw new InvalidOperationException("The model returned an empty or oversized expansion.");
            var preview = new TextBox { Text = candidate, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 160, MaxHeight = 340, IsSpellCheckEnabled = false };
            AutomationProperties.SetName(preview, "Expanded snippet preview");
            var review = new ContentDialog { Title = "Review expanded snippet", Content = preview, PrimaryButtonText = "Use expanded text", CloseButtonText = "Keep original", XamlRoot = XamlRoot };
            if (await review.ShowAsync() == ContentDialogResult.Primary)
            {
                PromptBox.Text = preview.Text.Trim();
                owner?.Hide();
            }
            else status.Text = "Expansion was reviewed and discarded; the original snippet remains unchanged.";
        }
        catch (Exception ex)
        {
            status.Text = $"Could not expand snippet: {ex.Message}";
        }
    }
}
