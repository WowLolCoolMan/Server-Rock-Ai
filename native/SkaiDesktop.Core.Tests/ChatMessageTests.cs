// Editing an earlier turn and the model-facing message shape
// (ConversationTurnEditor, ChatMessage.ForModel).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;
using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class ChatMessageTests
{
    public static void Run()
    {
        var editedTurn = ConversationTurnEditor.ReplaceUserTurn(
            [new ChatMessage("user", "Original question"), new ChatMessage("assistant", "Old answer"), new ChatMessage("user", "Later question"), new ChatMessage("assistant", "Later answer")],
            0,
            "Revised question",
            [new ChatAttachment("notes.md", "context")]);
        Check(editedTurn.Count == 1 && editedTurn[0].Content == "Revised question", "editing an earlier turn drops only that turn and its later branch");
        Check(editedTurn[0].Attachments is [{ FileName: "notes.md", Content: "context" }], "editing a turn keeps its selected attachment context");
        var laterTurnEdit = ConversationTurnEditor.ReplaceUserTurn(
            [new ChatMessage("user", "First"), new ChatMessage("assistant", "First answer"), new ChatMessage("user", "Second"), new ChatMessage("assistant", "Second answer")],
            2,
            "Revised second");
        Check(laterTurnEdit.Count == 3 && laterTurnEdit[0].Content == "First" && laterTurnEdit[^1].Content == "Revised second", "editing a later turn preserves earlier conversation context");
        try
        {
            ConversationTurnEditor.ReplaceUserTurn([new ChatMessage("assistant", "Not editable")], 0, "replacement");
            throw new Exception("Expected assistant messages to be rejected by the turn editor.");
        }
        catch (ArgumentException)
        {
            Check(true, "turn editor rejects attempts to edit assistant messages");
        }
        var localOnlyMessage = new ChatMessage("assistant", "With references", Sources: [new ChatSource("Docs", "https://example.org", "A snippet")]);
        Check(localOnlyMessage.ForModel(localOnlyMessage.Content).Sources is null, "web source metadata stays out of model requests");
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var modelImageMessage = new ChatMessage("user", "Describe this", [new ChatAttachment("sample.png", Convert.ToBase64String(pngSignature), "image/png")]).ForModel("Describe this");
        Check(modelImageMessage.Attachments is [{ IsImage: true }], "model messages retain image attachments while stripping text attachment metadata");

        // Fixed model instruction text (moved from MainPage into Core).
        Check(ModelInstructions.CodingAgent.StartsWith("You are the Coding Agent", StringComparison.Ordinal)
            && ModelInstructions.Chat.StartsWith("You are the Chat assistant", StringComparison.Ordinal)
            && ModelInstructions.UntrustedAttachments.Contains("untrusted", StringComparison.Ordinal)
            && ModelInstructions.UntrustedProjectExcerpts.Contains("untrusted project file data", StringComparison.Ordinal)
            && ModelInstructions.DocumentsOnly.Contains("attached documents", StringComparison.Ordinal)
            && ModelInstructions.DocumentsOnly.Contains("outside knowledge", StringComparison.Ordinal),
            "model instructions keep the workspace prompts and untrusted-data warnings");
    }
}
