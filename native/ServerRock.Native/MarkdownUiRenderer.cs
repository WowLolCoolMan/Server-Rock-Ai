using System.Xml.Linq;
using Markdig;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace ServerRock.Native;

/// <summary>Turns common Markdown output into native, selectable XAML content.</summary>
internal static class MarkdownUiRenderer
{
    private const int MaxRenderCharacters = 240_000;
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static StackPanel Render(string markdown)
    {
        var content = markdown.Length > MaxRenderCharacters
            ? markdown[..MaxRenderCharacters] + "\n\n[Message display shortened]"
            : markdown;
        var result = new StackPanel { Spacing = 10, MaxWidth = 800 };
        XElement root;
        try
        {
            var html = Markdown.ToHtml(content, Pipeline);
            root = XElement.Parse("<root>" + html + "</root>", LoadOptions.None);
        }
        catch
        {
            AddParagraph(result, content);
            return result;
        }
        foreach (var node in root.Nodes())
            if (node is XElement element) RenderBlock(element, result);
            else if (node is XText text && !string.IsNullOrWhiteSpace(text.Value)) AddParagraph(result, text.Value);
        return result;
    }

    private static void RenderBlock(XElement element, Panel parent)
    {
        var tag = element.Name.LocalName.ToLowerInvariant();
        if (tag is "p" or "div")
        {
            AddInlineText(parent, element, 15, false);
            return;
        }
        if (tag.Length == 2 && tag[0] == 'h' && tag[1] is >= '1' and <= '6')
        {
            var level = tag[1] - '0';
            AddInlineText(parent, element, level switch { 1 => 25, 2 => 22, 3 => 19, _ => 17 }, true);
            return;
        }
        if (tag == "pre")
        {
            AddCodeBlock(element, parent);
            return;
        }
        if (tag is "ul" or "ol")
        {
            AddList(element, parent, tag == "ol");
            return;
        }
        if (tag == "blockquote")
        {
            var quote = new StackPanel { Spacing = 8, Margin = new Thickness(8, 2, 0, 2) };
            foreach (var child in element.Elements()) RenderBlock(child, quote);
            parent.Children.Add(new Border
            {
                BorderBrush = Brush(106, 177, 188),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 4, 8, 4),
                Child = quote
            });
            return;
        }
        if (tag == "hr")
        {
            parent.Children.Add(new Border { Height = 1, Background = Brush(222, 231, 233), Margin = new Thickness(0, 4, 0, 4) });
            return;
        }
        if (tag == "table")
        {
            AddTable(element, parent);
            return;
        }
        foreach (var child in element.Elements()) RenderBlock(child, parent);
    }

    private static void AddParagraph(Panel parent, string text) =>
        parent.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 15, IsTextSelectionEnabled = true, LineHeight = 22 });

    private static void AddInlineText(Panel parent, XElement element, double fontSize, bool bold)
    {
        var block = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = bold ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
            Foreground = Brush(38, 56, 62),
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
            LineHeight = fontSize * 1.48
        };
        foreach (var node in element.Nodes()) AppendInline(block.Inlines, node);
        parent.Children.Add(block);
    }

    private static void AppendInline(InlineCollection output, XNode node)
    {
        if (node is XText text)
        {
            output.Add(new Run { Text = text.Value });
            return;
        }
        if (node is not XElement element) return;
        switch (element.Name.LocalName.ToLowerInvariant())
        {
            case "strong":
            case "b":
            {
                var span = new Bold();
                AppendChildren(span.Inlines, element);
                output.Add(span);
                break;
            }
            case "em":
            case "i":
            {
                var span = new Italic();
                AppendChildren(span.Inlines, element);
                output.Add(span);
                break;
            }
            case "code":
                output.Add(new Run { Text = element.Value, FontFamily = new FontFamily("monospace"), Foreground = Brush(28, 119, 130) });
                break;
            case "a":
            {
                var label = element.Value;
                if (Uri.TryCreate((string?)element.Attribute("href"), UriKind.Absolute, out var target)
                    && target.Scheme is "http" or "https")
                {
                    var link = new Hyperlink { NavigateUri = target };
                    link.Click += async (_, _) =>
                    {
                        try { await Windows.System.Launcher.LaunchUriAsync(target); }
                        catch { }
                    };
                    link.Inlines.Add(new Run { Text = label });
                    output.Add(link);
                }
                else output.Add(new Run { Text = label, Foreground = Brush(38, 56, 62) });
                break;
            }
            case "br":
                output.Add(new LineBreak());
                break;
            case "img":
                output.Add(new Run { Text = $"[Image: {(string?)element.Attribute("alt") ?? "preview unavailable"}]", Foreground = Brush(105, 126, 132) });
                break;
            default:
                AppendChildren(output, element);
                break;
        }
    }

    private static void AppendChildren(InlineCollection output, XElement element)
    {
        foreach (var child in element.Nodes()) AppendInline(output, child);
    }

    private static void AddCodeBlock(XElement pre, Panel parent)
    {
        var code = pre.Element("code");
        var source = code?.Value ?? pre.Value;
        var languageClass = (string?)code?.Attribute("class") ?? "";
        var language = languageClass.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(item => item.StartsWith("language-", StringComparison.Ordinal))?[9..] ?? "Code";
        var body = new TextBlock
        {
            Text = source.TrimEnd('\r', '\n'),
            FontFamily = new FontFamily("monospace"),
            FontSize = 13,
            Foreground = Brush(38, 56, 62),
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
            LineHeight = 19
        };
        var copy = new Button
        {
            Content = "Copy",
            Tag = source,
            Padding = new Thickness(8, 3, 8, 3),
            Background = Brush(244, 248, 249),
            Foreground = Brush(77, 100, 106),
            BorderBrush = Brush(222, 233, 234),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7)
        };
        copy.Click += (_, _) =>
        {
            try
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText((string)copy.Tag);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
                copy.Content = "Copied";
            }
            catch { copy.Content = "Copy unavailable"; }
        };
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } } };
        header.Children.Add(new TextBlock { Text = language, FontSize = 11, Foreground = Brush(105, 126, 132), VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(copy, 1);
        header.Children.Add(copy);
        var stack = new StackPanel { Spacing = 8, Children = { header, body } };
        parent.Children.Add(new Border
        {
            Background = Brush(248, 250, 250),
            BorderBrush = Brush(229, 236, 237),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Child = stack
        });
    }

    private static void AddList(XElement list, Panel parent, bool ordered)
    {
        var items = list.Elements("li").ToArray();
        var listPanel = new StackPanel { Spacing = 7, Margin = new Thickness(0, 2, 0, 2) };
        var index = 1;
        foreach (var item in items)
        {
            var marker = ordered ? $"{index++}." : "•";
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(28) }, new ColumnDefinition() } };
            row.Children.Add(new TextBlock { Text = marker, FontSize = 15, Foreground = Brush(88, 114, 121), VerticalAlignment = VerticalAlignment.Top });
            var itemContent = new StackPanel { Spacing = 6 };
            var inlineNodes = item.Nodes().Where(node => node is XText || node is XElement child && child.Name.LocalName is not "ul" and not "ol").ToArray();
            if (inlineNodes.Length > 0)
            {
                var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 15, IsTextSelectionEnabled = true, LineHeight = 22 };
                foreach (var node in inlineNodes) AppendInline(textBlock.Inlines, node);
                itemContent.Children.Add(textBlock);
            }
            foreach (var nested in item.Elements().Where(child => child.Name.LocalName is "ul" or "ol"))
                AddList(nested, itemContent, nested.Name.LocalName == "ol");
            Grid.SetColumn(itemContent, 1);
            row.Children.Add(itemContent);
            listPanel.Children.Add(row);
        }
        parent.Children.Add(listPanel);
    }

    private static void AddTable(XElement table, Panel parent)
    {
        var rows = table.Descendants("tr").ToArray();
        if (rows.Length == 0) return;
        var columns = rows.Max(row => row.Elements().Count());
        var tablePanel = new StackPanel { Spacing = 0 };
        foreach (var rowElement in rows)
        {
            var row = new Grid { ColumnSpacing = 0 };
            for (var column = 0; column < columns; column++)
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var cells = rowElement.Elements().ToArray();
            for (var index = 0; index < columns; index++)
            {
                var cell = index < cells.Length ? cells[index] : null;
                var text = new TextBlock { Text = cell?.Value ?? "", TextWrapping = TextWrapping.Wrap, FontSize = 13, IsTextSelectionEnabled = true, FontWeight = cell?.Name.LocalName == "th" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal };
                var border = new Border { BorderBrush = Brush(225, 233, 234), BorderThickness = new Thickness(1), Padding = new Thickness(9, 7, 9, 7), Child = text };
                Grid.SetColumn(border, index);
                row.Children.Add(border);
            }
            tablePanel.Children.Add(row);
        }
        parent.Children.Add(new Border { BorderBrush = Brush(225, 233, 234), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Child = tablePanel });
    }

    private static SolidColorBrush Brush(byte red, byte green, byte blue) =>
        new(ColorHelper.FromArgb(255, red, green, blue));
}
