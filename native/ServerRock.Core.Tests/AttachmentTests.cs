// Chat attachments: byte limits, allowed names, image signatures, PDF text extraction
// (ChatAttachmentPolicy, PdfAttachmentTextExtractor).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Text;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using static TestSupport;

internal static class AttachmentTests
{
    public static async Task RunAsync()
    {
        var attachmentProbePath = Path.Combine(Path.GetTempPath(), "server-rock-attachment-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            File.WriteAllBytes(attachmentProbePath, new byte[ChatAttachmentPolicy.MaxFileBytes + 1]);
            try
            {
                _ = await ChatAttachmentPolicy.ReadBytesAsync(attachmentProbePath, ChatAttachmentPolicy.MaxFileBytes);
                throw new Exception("Expected oversized attachment to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(true, "native attachment reads enforce byte limits");
            }
        }
        finally { if (File.Exists(attachmentProbePath)) File.Delete(attachmentProbePath); }

        Check(ChatAttachmentPolicy.IsAllowedName("notes.md") && ChatAttachmentPolicy.IsAllowedName("src/Program.cs"), "chat attachment policy allows supported text files");
        Check(ChatAttachmentPolicy.IsAllowedPdfName("brief.pdf") && ChatAttachmentPolicy.IsAllowedName("brief.pdf") && !ChatAttachmentPolicy.IsAllowedPdfName(".env.pdf"), "chat attachment policy allows PDFs but blocks secret-like names");
        Check(ChatAttachmentPolicy.IsAllowedName("photo.png") && ChatAttachmentPolicy.GetImageMediaType("photo.jpg") == "image/jpeg" && ChatAttachmentPolicy.GetImageMediaType("vector.svg") is null, "chat attachment policy accepts PNG and JPEG images and rejects active SVG images");
        Check(!ChatAttachmentPolicy.IsAllowedName(".env.png"), "chat attachment policy blocks secret-named files even with an image extension");
        Check(!ChatAttachmentPolicy.IsAllowedName(".env") && !ChatAttachmentPolicy.IsAllowedName("id_rsa") && !ChatAttachmentPolicy.IsAllowedName("archive.zip"), "chat attachment policy blocks secrets and unsupported binary files");
        Check(ChatAttachmentPolicy.BoundContent(new string('x', ChatAttachmentPolicy.MaxCharactersPerFile + 50)).Length == ChatAttachmentPolicy.MaxCharactersPerFile, "chat attachments are bounded per file");
        var pdfBuilder = new PdfDocumentBuilder();
        var pdfPage = pdfBuilder.AddPage(PageSize.A4);
        var pdfFont = pdfBuilder.AddStandard14Font(Standard14Font.Helvetica);
        var pdfExpectedText = string.Concat(Enumerable.Repeat("Local PDF text stays on this device. ", 12));
        pdfPage.AddText(pdfExpectedText, 12, new PdfPoint(25, 700), pdfFont);
        var generatedPdf = pdfBuilder.Build();
        var extractedPdfText = PdfAttachmentTextExtractor.Extract(generatedPdf, 1200);
        Check(PdfAttachmentTextExtractor.HasPdfSignature(generatedPdf) && extractedPdfText.Contains("Local PDF text stays on this device.", StringComparison.Ordinal), "PDF attachment extractor reads selectable text locally");
        var truncatedPdfText = PdfAttachmentTextExtractor.Extract(generatedPdf, 96);
        Check(truncatedPdfText.Length <= 96 && truncatedPdfText.Contains("omitted", StringComparison.Ordinal), "PDF extraction stays within its requested text bound and marks truncation");
        try
        {
            PdfAttachmentTextExtractor.Extract(Encoding.ASCII.GetBytes("not a PDF"), 100);
            throw new Exception("Expected invalid PDF bytes to be rejected.");
        }
        catch (InvalidDataException) { Check(true, "PDF attachment extractor rejects invalid PDF signatures"); }
        Check(ChatAttachmentPolicy.HasValidImageSignature("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]) && ChatAttachmentPolicy.HasValidImageSignature("image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]) && !ChatAttachmentPolicy.HasValidImageSignature("image/png", [0, 1, 2]), "chat attachment policy checks PNG/JPEG file signatures");
        Check(ChatAttachmentPolicy.RemainingImageBytes([new ChatAttachment("photo.png", Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), "image/png")]) == ChatAttachmentPolicy.MaxTotalImageBytes - 8, "chat attachment policy budgets decoded image bytes");

        // Consent key and prompt formatting (moved from MainPage into Core).
        var notes = new ChatAttachment("notes.md", "alpha");
        var spec = new ChatAttachment("spec.txt", "beta");
        Check(ChatAttachmentPolicy.ConsentKey("https://remote.example/v1", [notes, spec]) == ChatAttachmentPolicy.ConsentKey("https://remote.example/v1", [spec, notes]),
            "attachment consent key ignores attachment order");
        Check(ChatAttachmentPolicy.ConsentKey("https://remote.example/v1", [notes]) != ChatAttachmentPolicy.ConsentKey("https://remote.example/v1", [notes with { Content = "changed" }])
            && ChatAttachmentPolicy.ConsentKey("https://a.example/v1", [notes]) != ChatAttachmentPolicy.ConsentKey("https://b.example/v1", [notes]),
            "attachment consent key changes with file content and endpoint");
        var attachmentDraftPath = Path.Combine(Path.GetTempPath(), "server-rock-attachment-drafts-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var draftStore = new WorkspaceAttachmentDraftStore(attachmentDraftPath);
            draftStore.Save("ChatBot", [notes, spec]);
            var restored = draftStore.Load("ChatBot");
            Check(restored.Count == 2 && restored[0].Content == "alpha" && restored[1].Content == "beta", "workspace attachment drafts round-trip locally");
            draftStore.Save("ChatBot", []);
            Check(draftStore.Load("ChatBot").Count == 0, "clearing workspace attachment drafts removes only the local pending set");
        }
        finally
        {
            if (File.Exists(attachmentDraftPath)) File.Delete(attachmentDraftPath);
        }
        var fencedPrompt = ChatAttachmentPrompt.AppendTo("Review this", [
            new ChatAttachment("snippet.md", "before\n```\ninside\n```"),
            new ChatAttachment("photo.png", "iVBORw0KGgo=", "image/png"),
            new ChatAttachment("brief.pdf", "PDF words")]);
        Check(fencedPrompt.StartsWith("Review this\n\nAttached text file (untrusted): snippet.md\n````text\n", StringComparison.Ordinal)
            && fencedPrompt.Contains("Extracted PDF text (untrusted): brief.pdf", StringComparison.Ordinal)
            && !fencedPrompt.Contains("photo.png", StringComparison.Ordinal),
            "attachment prompt uses a fence longer than any backtick run, labels PDFs, and skips images");
        var largeAttachment = string.Join("\n", Enumerable.Range(0, 8).Select(index => index == 5
            ? "The release rollback checklist requires an explicit owner and verification window."
            : $"Unrelated archive section {index} with no matching terms.".PadRight(2100, 'x')));
        var retrievedPrompt = ChatAttachmentPrompt.AppendWithBoundedRetrieval("Find the release rollback checklist", [new ChatAttachment("large.md", largeAttachment)]);
        Check(retrievedPrompt.Contains("release rollback checklist", StringComparison.OrdinalIgnoreCase)
            && retrievedPrompt.Contains("Retrieved attachment text (untrusted): large.md", StringComparison.Ordinal)
            && retrievedPrompt.Length < largeAttachment.Length + 2000,
            "oversized attachment context uses bounded query-relevant retrieval");
    }
}
