// Bounded local quicklinks. Links are validated before persistence and opened only
// after an explicit user click; they are never fetched or sent to a model here.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private const int MaxQuicklinks = 32;
    private const int MaxQuicklinkTitle = 120;
    private const int MaxQuicklinkUrl = 2048;

    private async Task OpenQuicklinksAsync()
    {
        var title = new TextBox { Header = "Name", PlaceholderText = "Project documentation", MaxLength = MaxQuicklinkTitle };
        var url = new TextBox { Header = "Web address", PlaceholderText = "https://example.com", MaxLength = MaxQuicklinkUrl };
        var add = new Button { Content = "Add quicklink", HorizontalAlignment = HorizontalAlignment.Left };
        var list = new ListView { MinHeight = 150, MaxHeight = 300, SelectionMode = ListViewSelectionMode.Single };
        var open = new Button { Content = "Open selected", IsEnabled = false };
        var remove = new Button { Content = "Remove selected", IsEnabled = false };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };

        AutomationProperties.SetName(title, "Quicklink name");
        AutomationProperties.SetName(url, "Quicklink web address");
        AutomationProperties.SetName(add, "Add quicklink");
        AutomationProperties.SetName(list, "Saved quicklinks");
        AutomationProperties.SetName(open, "Open selected quicklink");
        AutomationProperties.SetName(remove, "Remove selected quicklink");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);

        void Refresh()
        {
            list.ItemsSource = null;
            list.ItemsSource = _settings.Quicklinks.Take(MaxQuicklinks).ToList();
            status.Text = $"{list.Items.Count} saved quicklink{(list.Items.Count == 1 ? "" : "s")} · local only";
        }

        list.ItemTemplate = new DataTemplate(() =>
        {
            var panel = new StackPanel { Spacing = 2 };
            var name = new TextBlock();
            name.SetBinding(TextBlock.TextProperty, new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath(nameof(Quicklink.Title)) });
            var address = new TextBlock { Opacity = 0.76, TextWrapping = TextWrapping.Wrap };
            address.SetBinding(TextBlock.TextProperty, new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath(nameof(Quicklink.Url)) });
            panel.Children.Add(name);
            panel.Children.Add(address);
            return panel;
        });
        list.SelectionChanged += (_, _) =>
        {
            var selected = list.SelectedItem is Quicklink;
            open.IsEnabled = selected;
            remove.IsEnabled = selected;
        };
        add.Click += (_, _) =>
        {
            var name = title.Text.Trim();
            var address = url.Text.Trim();
            if (name.Length is < 1 or > MaxQuicklinkTitle) { status.Text = "Enter a quicklink name up to 120 characters."; return; }
            if (address.Length > MaxQuicklinkUrl || !Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
            {
                status.Text = "Use an http or https address without embedded credentials.";
                return;
            }
            if (_settings.Quicklinks.Count >= MaxQuicklinks) { status.Text = $"Quicklinks are limited to {MaxQuicklinks}. Remove one before adding another."; return; }
            if (_settings.Quicklinks.Any(item => item.Url.Equals(uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))) { status.Text = "That web address is already saved."; return; }
            _settings.Quicklinks.Add(new Quicklink { Title = name, Url = uri.AbsoluteUri });
            if (!SaveSettings()) return;
            title.Text = string.Empty;
            url.Text = string.Empty;
            Refresh();
            status.Text = "Quicklink saved locally. Opening it requires an explicit Open action.";
        };
        open.Click += async (_, _) =>
        {
            if (list.SelectedItem is not Quicklink selected || !Uri.TryCreate(selected.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return;
            try { await Windows.System.Launcher.LaunchUriAsync(uri); status.Text = $"Opened {selected.Title}."; }
            catch { status.Text = "The selected quicklink could not be opened."; }
        };
        remove.Click += (_, _) =>
        {
            if (list.SelectedItem is not Quicklink selected) return;
            _settings.Quicklinks.RemoveAll(item => item.Id == selected.Id);
            if (!SaveSettings()) return;
            Refresh();
            status.Text = "Quicklink removed locally.";
        };
        Refresh();

        var dialog = new ContentDialog
        {
            Title = "Quicklinks",
            Content = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 9,
                    Children =
                    {
                        new TextBlock { Text = "Save frequent-access web addresses locally. Nothing is opened or fetched until you choose Open selected.", TextWrapping = TextWrapping.Wrap },
                        title,
                        url,
                        add,
                        list,
                        new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { open, remove } },
                        status
                    }
                },
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            },
            CloseButtonText = "Done",
            XamlRoot = XamlRoot
        };
        AutomationProperties.SetName(dialog, "Quicklinks");
        await dialog.ShowAsync();
    }
}
