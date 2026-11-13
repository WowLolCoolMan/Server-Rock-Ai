using Microsoft.UI.Xaml;
using SkaiDesktop.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void VoiceInputClick(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        try
        {
            VoiceButton.IsEnabled = false;
            VoiceButtonIcon.Glyph = "\uE916"; // stopwatch: listening
            var recognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();
            await recognizer.CompileConstraintsAsync();
            var result = await recognizer.RecognizeAsync();
            if (result.Status == Windows.Media.SpeechRecognition.SpeechRecognitionResultStatus.Success && !string.IsNullOrWhiteSpace(result.Text))
            {
                PromptBox.Text = result.Text.Trim();
                PromptBox.SelectionStart = PromptBox.Text.Length;
                RuntimeStatus.Text = "Voice draft ready · review it, then send to any model.";
            }
            else RuntimeStatus.Text = "No speech was detected. Try again or type your message.";
            recognizer.Dispose();
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Voice input unavailable: {ex.Message}"; }
        finally { VoiceButton.IsEnabled = true; VoiceButtonIcon.Glyph = "\uE720"; }
#else
        RuntimeStatus.Text = "Voice input is available on the Windows packaged build; type your prompt on this host.";
#endif
    }

    private void VoiceOutputClick(object sender, RoutedEventArgs e)
    {
        _settings.VoiceOutputEnabled = !_settings.VoiceOutputEnabled;
        SaveSettings();
        ApplySavedCustomization();
        RuntimeStatus.Text = _settings.VoiceOutputEnabled ? "Spoken responses enabled for every model." : "Spoken responses disabled.";
    }

    private async Task SpeakResponseAsync(string response)
    {
        if (!_settings.VoiceOutputEnabled || string.IsNullOrWhiteSpace(response)) return;
        var profile = VoiceProfileCatalog.Resolve(_settings.VoiceProfiles, _settings.VoiceProfileId);
        var selectedModel = VoiceModelCatalog.Resolve(_settings.VoiceLowResourceModeEnabled
            ? _settings.VoiceLowResourceModelId
            : _settings.VoicePrimaryModelId);
        if (!selectedModel.Id.Equals("os-native", StringComparison.OrdinalIgnoreCase))
        {
            RuntimeStatus.Text = $"{selectedModel.Name} is selected but not installed; using OS-native speech.";
        }
        var text = System.Text.RegularExpressions.Regex.Replace(response, "[`*_>#]", string.Empty);
        text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
        if (text.Length > 4000) text = text[..4000];
#if WINDOWS
        try
        {
            var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            if (!string.IsNullOrWhiteSpace(profile.VoiceName))
            {
                var voice = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.FirstOrDefault(item => item.DisplayName.Equals(profile.VoiceName, StringComparison.OrdinalIgnoreCase));
                if (voice is not null) synthesizer.Voice = voice;
            }
            var stream = await synthesizer.SynthesizeTextToStreamAsync(text);
            _voicePlayer?.Dispose();
            _voicePlayer = new Windows.Media.Playback.MediaPlayer
            {
                Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, stream.ContentType)
            };
            _voicePlayer.Play();
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Spoken response unavailable: {ex.Message}"; }
#elif SRAI_DESKTOP
        try
        {
            if (!await _nativeTts.SpeakLinuxAsync(text, profile, CancellationToken.None))
                RuntimeStatus.Text = "Linux OS speech is unavailable; install espeak-ng or speech-dispatcher.";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Linux spoken response unavailable: {ex.Message}"; }
#endif
    }
}
