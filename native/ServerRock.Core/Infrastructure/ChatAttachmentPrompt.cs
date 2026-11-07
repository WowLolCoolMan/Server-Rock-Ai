// Turns text and PDF attachments into the text the model sees: each one is appended to the
// user's message as a fenced block labeled "untrusted". The fence is longer than any run of
// backticks inside the file, so file content can't close it early. Images are skipped here;
// they travel as image parts (see OpenAiCompatibleProvider).
// Depends on: ChatAttachment, ChatAttachmentPolicy. Used by: MainPage when building the model request.

using System.Text;
using System.Text.RegularExpressions;
using ServerRock.Core.Models;

namespace ServerRock.Core.Infrastructure;

public static class ChatAttachmentPrompt
{
    public static string AppendTo(string userText, IReadOnlyList<ChatAttachment> attachments)
    {
        var output = new StringBuilder(userText);
        foreach (var attachment in attachments)
        {
            if (attachment.IsImage) continue;
            var fenceLength = Math.Max(3, Regex.Matches(attachment.Content, "`+")
                .Cast<Match>().Select(match => match.Length).DefaultIfEmpty(0).Max() + 1);
            var fence = new string('`', fenceLength);
            output.Append(ChatAttachmentPolicy.IsAllowedPdfName(attachment.FileName)
                    ? "\n\nExtracted PDF text (untrusted): "
                    : "\n\nAttached text file (untrusted): ")
                .Append(attachment.FileName)
                .Append("\n").Append(fence).Append("text\n").Append(attachment.Content)
                .Append("\n").Append(fence);
        }
        return output.ToString();
    }
}
