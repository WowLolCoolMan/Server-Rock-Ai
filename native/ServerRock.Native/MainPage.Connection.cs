// Model server connection status: on page load (and after Settings changes) asks the
// configured endpoint for its model list, then updates the Local/Remote ready labels, the
// connection dots, and the data-route label. If the saved model isn't installed, it reports the
// mismatch and waits for explicit user selection. LocalEndpoint.IsLocal (Core) decides what counts
// as "on this device" (loopback only).
// Depends on: OpenAiCompatibleProvider (_provider), GetProviderApiKey, SaveSettings, header
// elements in MainPage.xaml.
// Called by: Loaded in MainPage.xaml, Settings/model selection (RefreshModelConnectionAsync), and
// Model Library and composer code that shows the connection.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async void MainPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_companionState.IsVisible)
        {
            UpdateCompanionDisplay();
        }
        if (_connectionCheckRunning) return;
        _connectionCheckRunning = true;
        try { await RefreshModelConnectionAsync(); }
        finally { _connectionCheckRunning = false; }
    }

    private async Task RefreshModelConnectionAsync()
    {
        ConnectionLabel.Text = "Checking…";
        HeaderConnectionLabel.Text = "Checking…";
        try
        {
            if (AirLlmProvider.IsAirLlmProvider(_settings.ModelProvider))
            {
                if (!_airLlmProvider.CanHandle(_settings) || !_airLlmProvider.IsInstalled) throw new InvalidOperationException("Enter a Hugging Face model ID and install the built-in AirLLM runtime.");
                _advertisedModels = [_settings.Model];
                _providerConnected = true;
                ConnectionLabel.Text = "AirLLM ready";
                HeaderConnectionLabel.Text = "AirLLM ready";
                ConnectionDot.Fill = UiTheme.Brush("SuccessBrush");
                HeaderConnectionDot.Fill = ConnectionDot.Fill;
                ProcessingLabel.Text = "● AirLLM · local layer-wise inference";
                RuntimeStatus.Text = $"AirLLM ready · {_settings.Model} · local-only";
                UpdateModeControls();
                return;
            }
            var profiles = await _provider.DiscoverModelProfilesAsync(_settings.Endpoint, apiKey: GetProviderApiKey());
            var models = profiles.Select(profile => profile.Model).ToArray();
            if (!LocalEndpoint.IsLocal(_settings.Endpoint))
                throw new InvalidOperationException("Skai Desktop only supports model servers on this computer.");
            _advertisedModels = models;
            _providerConnected = true;
            ConnectionLabel.Text = "Local ready";
            HeaderConnectionLabel.Text = ConnectionLabel.Text;
            ConnectionDot.Fill = UiTheme.Brush("SuccessBrush");
            HeaderConnectionDot.Fill = ConnectionDot.Fill;
            ProcessingLabel.Text = "● On-device model";
            var selection = ModelSelectionPolicy.Evaluate(_settings.Model, models);
            if (!selection.IsAvailable)
            {
                RuntimeStatus.Text = $"Connected · selected model {_settings.Model} is unavailable · choose a model in Model Library";
            }
            else
            {
                var selectedProfile = profiles.FirstOrDefault(profile => profile.Model.Equals(_settings.Model, StringComparison.Ordinal));
                var capability = selectedProfile?.Vision switch
                {
                    CapabilitySupport.Supported => "vision advertised",
                    CapabilitySupport.Unsupported => "non-vision metadata",
                    _ => "vision capability unknown"
                };
                RuntimeStatus.Text = $"Connected · {_settings.Model} · {capability}";
            }
        }
        catch (Exception ex)
        {
            _advertisedModels = [];
            _providerConnected = false;
            ConnectionLabel.Text = "Not connected";
            HeaderConnectionLabel.Text = "Not connected";
            ConnectionDot.Fill = UiTheme.Brush("WarningBrush");
            HeaderConnectionDot.Fill = ConnectionDot.Fill;
            ProcessingLabel.Text = "● Model not connected";
            RuntimeStatus.Text = $"Set up your model connection · {ex.Message}";
            if (_companionState.IsVisible)
                UpdateCompanionSpeech("Model connection failed · open Settings to reconnect.");
        }
        UpdateModeControls();
    }

}
