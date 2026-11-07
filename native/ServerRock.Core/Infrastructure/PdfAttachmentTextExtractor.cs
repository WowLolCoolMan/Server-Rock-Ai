// Extracts bounded, page-ordered text from a PDF on-device (PdfPig).
// Used by: MainPage attachments.

using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ServerRock.Core.Infrastructure;

/// <summary>Extracts bounded, page-ordered text from a PDF selected by the user.</summary>
public static class PdfAttachmentTextExtractor
{
    public const int MaxPages = 80;
    private const string TruncationNotice = "\n[Further PDF text omitted to fit the local attachment limit.]";

    public static bool HasPdfSignature(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8);

    public static string Extract(ReadOnlyMemory<byte> bytes, int maxCharacters)
    {
        if (!HasPdfSignature(bytes.Span))
            throw new InvalidDataException("This file does not have a valid PDF signature.");
        if (maxCharacters <= 0) return string.Empty;

        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        using var document = PdfDocument.Open(stream);
        var output = new StringBuilder(Math.Min(maxCharacters, 4096));
        var pageLimit = Math.Min(document.NumberOfPages, MaxPages);
        for (var pageNumber = 1; pageNumber <= pageLimit; pageNumber++)
        {
            var text = ContentOrderTextExtractor.GetText(document.GetPage(pageNumber)).Trim();
            if (text.Length == 0) continue;
            var pagePrefix = $"\n\n[Page {pageNumber}]\n";
            var required = pagePrefix.Length + text.Length;
            var remaining = maxCharacters - output.Length;
            if (required <= remaining)
            {
                output.Append(pagePrefix).Append(text);
                continue;
            }

            var markerLength = Math.Min(TruncationNotice.Length, Math.Max(0, remaining));
            var textRoom = Math.Max(0, remaining - markerLength - Math.Min(pagePrefix.Length, remaining - markerLength));
            if (remaining > markerLength)
                output.Append(pagePrefix.AsSpan(0, Math.Min(pagePrefix.Length, remaining - markerLength)))
                    .Append(text.AsSpan(0, Math.Min(text.Length, textRoom)))
                    .Append(TruncationNotice.AsSpan(0, markerLength));
            break;
        }

        if (document.NumberOfPages > MaxPages && output.Length < maxCharacters)
        {
            var marker = "\n[Only the first 80 pages were included.]";
            output.Append(marker.AsSpan(0, Math.Min(marker.Length, maxCharacters - output.Length)));
        }
        var extracted = output.ToString().Trim();
        if (extracted.Length == 0)
            throw new InvalidDataException("No selectable text was found in this PDF. Scanned documents need OCR before they can be read as text.");
        return extracted.Length <= maxCharacters ? extracted : extracted[..maxCharacters];
    }
}
