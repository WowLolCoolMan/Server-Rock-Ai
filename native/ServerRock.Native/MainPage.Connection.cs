// Model server connection status: on page load (and after Settings changes) asks the
// configured endpoint for its model list, then updates the Local/Remote ready labels, the
// connection dots, and the data-route label. If the saved model isn't installed, it switches to
// the first available one. LocalEndpoint.IsLocal (Core) decides what counts as "on this device" (loopback only).
// Depends on: OpenAiCompatibleProvider (_provider), GetProviderApiKey, SaveSettings, header
// elements in MainPage.xaml.
// Called by: Loaded in MainPage.xaml, Settings/model selection (RefreshModelConnectionAsync), and
// Model Library and composer code that shows the connection.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ServerRock.Core.Infrastructure;

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
            var models = await _provider.DiscoverModelsAsync(_settings.Endpoint, apiKey: GetProviderApiKey());
            _providerConnected = true;
            var local = LocalEndpoint.IsLocal(_settings.Endpoint);
            ConnectionLabel.Text = local ? "Local ready" : "Remote ready";
            HeaderConnectionLabel.Text = ConnectionLabel.Text;
            ConnectionDot.Fill = UiTheme.Brush("SuccessBrush");
            HeaderConnectionDot.Fill = ConnectionDot.Fill;
            ProcessingLabel.Text = local ? "● On-device model" : "● Remote model server";
            if (!models.Contains(_settings.Model, StringComparer.Ordinal))
            {
                var previouslySelected = _settings.Model;
                _settings.Model = models[0];
                SaveSettings();
                UpdateModelLabel();
                RuntimeStatus.Text = $"Selected installed model {_settings.Model}; {previouslySelected} was unavailable";
            }
            else RuntimeStatus.Text = $"Connected · {_settings.Model}";
        }
        catch (Exception ex)
        {
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
