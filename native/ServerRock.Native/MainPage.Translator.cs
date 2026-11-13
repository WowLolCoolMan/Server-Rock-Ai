// Review-first translation helper. It uses the selected provider only after the user
// explicitly submits the source text and target language, then requires confirmation before
// putting the translated text into the composer.

using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task OpenTranslatorAsync()
    {
        var source = new TextBox { Header = "Text to translate", PlaceholderText = "Paste or write text to translate", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 150, MaxHeight = 320, MaxLength = 12000 };
        var language = new ComboBox { Header = "Target language", MinWidth = 220 };
        foreach (var item in new[] { "English", "Spanish", "French", "German", "Italian", "Portuguese", "Japanese", "Korean", "Chinese" }) language.Items.Add(item);
        language.SelectedIndex = 0;
        AutomationProperties.SetName(source, "Translation source text");
        AutomationProperties.SetName(language, "Translation target language");
        AutomationProperties.SetHelpText(language, "Choose the language for the reviewable translation.");
        var note = new TextBlock { Text = "Translation is an explicit provider request. The result opens in an editable review dialog and is not sent automatically.", TextWrapping = TextWrapping.Wrap, Opacity = 0.82 };
        var dialog = new ContentDialog
        {
            Title = "Translate text",
            Content = new StackPanel { Spacing = 10, Children = { source, language, note } },
            PrimaryButtonText = "Translate",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };
        AutomationProperties.SetName(dialog, "Translate text");
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var value = source.Text.Trim();
        if (value.Length is < 1 or > 12000)
        {
            RuntimeStatus.Text = "Enter between 1 and 12,000 characters to translate.";
            return;
        }
        var target = language.SelectedItem?.ToString() ?? "English";
        await TranslateAndReviewAsync(value, target);
    }

    private async Task TranslateAndReviewAsync(string source, string target)
    {
        RuntimeStatus.Text = $"Translating to {target} for review…";
        var translation = new StringBuilder();
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var instruction = $"Translate the text below into {target}. Preserve meaning, formatting, and tone. Return only the translation, with no commentary.\n\nText:\n{source}";
            await _provider.StreamChatAsync(_settings, [new ChatMessage("user", instruction)], chunk =>
            {
                if (translation.Length + chunk.Length <= 24000) translation.Append(chunk);
            }, cancellation.Token, maxTokens: 2048, apiKey: GetProviderApiKey());
            var candidate = translation.ToString().Trim();
            if (candidate.Length is < 1 or > 24000) throw new InvalidOperationException("The model returned an empty or oversized translation.");
            var preview = new TextBox { Text = candidate, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 160, MaxHeight = 360, IsSpellCheckEnabled = false };
            AutomationProperties.SetName(preview, "Translation preview");
            var review = new ContentDialog { Title = $"Review {target} translation", Content = preview, PrimaryButtonText = "Use translation", CloseButtonText = "Discard", XamlRoot = XamlRoot };
            AutomationProperties.SetName(review, "Review translation");
            if (await review.ShowAsync() == ContentDialogResult.Primary)
            {
                PromptBox.Text = preview.Text.Trim();
                RuntimeStatus.Text = $"{target} translation inserted into the composer for review. Nothing was sent.";
            }
            else RuntimeStatus.Text = "Translation discarded; no composer text was changed.";
        }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Translation failed: {ex.Message}";
        }
    }
}
