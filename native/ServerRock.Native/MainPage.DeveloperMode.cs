// Local developer diagnostics. This surface is explicitly gated by a saved preference;
// it reports bounded non-secret facts and never runs commands or changes approval policy.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task OpenDeveloperDiagnosticsAsync()
    {
        if (!_settings.DeveloperMode)
        {
            RuntimeStatus.Text = "Developer mode is off. Enable it in Settings → Advanced before opening diagnostics.";
            return;
        }

        var summary = $"{ProductIdentity.ShortName} developer diagnostics\n\n" +
            $"Runtime: {Environment.OSVersion.Platform} · {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} · {Environment.Version}\n" +
            $"Endpoint class: {(LocalEndpoint.IsLocal(_settings.Endpoint) ? "on-device/local" : "remote or non-local")}\n" +
            $"Selected provider: {_settings.ModelProvider}\n" +
            $"Selected model: {_settings.Model}\n" +
            $"Web search: {(_settings.WebSearchEnabled ? "enabled" : "off")} · memory injection: {(_settings.MemoryInjectionEnabled ? "enabled" : "off")}\n" +
            $"Saved chats: {_sessions.Count} · projects: {_projects.Count} · quicklinks: {_settings.Quicklinks.Count}\n\n" +
            "This is a local, bounded inspection view. It does not expose API keys, run commands, change approvals, or send data.";
        var preview = new TextBox { Text = summary, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 360 };
        AutomationProperties.SetName(preview, "Developer diagnostics summary");
        var dialog = new ContentDialog
        {
            Title = "Developer diagnostics",
            Content = preview,
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        AutomationProperties.SetName(dialog, "Developer diagnostics");
        await dialog.ShowAsync();
    }
}
