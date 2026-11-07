// Size-limited reads of HTTP bodies, stream lines, and local store files
// (BoundedHttpContent, BoundedFileReader).
// Uses TestSupport (Check, StubHandler, …). Run by Program.cs.

using System.Text;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;
using ServerRock.Core.Services;
using static TestSupport;

internal static class BoundedReadTests
{
    public static async Task RunAsync()
    {
        var oversizedNativeJson = new StringContent("{}");
        oversizedNativeJson.Headers.ContentLength = BoundedHttpContent.MaxJsonBytes + 1L;
        try
        {
            _ = await BoundedHttpContent.ReadJsonAsync(oversizedNativeJson, label: "Native test response");
            throw new Exception("Expected oversized native HTTP JSON to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "native HTTP JSON bodies are bounded before parsing");
        }
        using (var lineStream = new MemoryStream(Encoding.UTF8.GetBytes("first\nsecond\n")))
        using (var lineSource = new StreamReader(lineStream))
        {
            var lineReader = BoundedHttpContent.CreateLineReader(lineSource);
            Check(await lineReader.ReadLineAsync() == "first" && await lineReader.ReadLineAsync() == "second" && await lineReader.ReadLineAsync() is null,
                "native stream line reader preserves adjacent lines");
        }
        try
        {
            using var oversizedLineStream = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', BoundedHttpContent.MaxStreamLineCharacters + 1)));
            using var oversizedLineSource = new StreamReader(oversizedLineStream);
            var oversizedLineReader = BoundedHttpContent.CreateLineReader(oversizedLineSource);
            _ = await oversizedLineReader.ReadLineAsync();
            throw new Exception("Expected an oversized native stream line to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "native stream line reader rejects oversized lines before JSON parsing");
        }
        var oversizedStorePath = Path.Combine(Path.GetTempPath(), "server-rock-oversized-store-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(oversizedStorePath, new string('x', BoundedFileReader.MaxJsonBytes + 1));
            try
            {
                _ = BoundedFileReader.ReadText(oversizedStorePath);
                throw new Exception("Expected oversized native persistence to be rejected.");
            }
            catch (InvalidDataException)
            {
                Check(true, "native persistence files are bounded before deserialization");
            }
        }
        finally { if (File.Exists(oversizedStorePath)) File.Delete(oversizedStorePath); }
        var invalidUtf8Path = Path.Combine(Path.GetTempPath(), "server-rock-invalid-utf8-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllBytes(invalidUtf8Path, [0xC3, 0x28]);
            _ = BoundedFileReader.ReadText(invalidUtf8Path);
            throw new Exception("Expected invalid UTF-8 persistence to be rejected.");
        }
        catch (InvalidDataException)
        {
            Check(true, "native persistence rejects invalid UTF-8 before deserialization");
        }
        finally { if (File.Exists(invalidUtf8Path)) File.Delete(invalidUtf8Path); }
    }
}
