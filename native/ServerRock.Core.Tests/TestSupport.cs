// Shared helpers for the Core test runner: the Check assertion, git/temp-folder
// utilities, and fake HTTP/stream types. Used by every *Tests.cs file.

using System.Diagnostics;
using ServerRock.Core.Infrastructure;
using ServerRock.Core.Models;

internal static class TestSupport
{
    public static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Check failed: " + description);
        Console.WriteLine("PASS " + description);
    }

    public static async Task<List<ChatSession>> ReadElectronFixtureAsync(string path)
    {
        await using var source = File.OpenRead(path);
        return await ConversationArchive.ParseAsync(source);
    }

    public static void RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Git could not be started for the native test fixture.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"Git test fixture command failed: {string.Join(' ', arguments)} {stderr} {stdout}");
    }

    public static void DeleteDirectoryTree(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        foreach (var directory in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).OrderByDescending(item => item.Length))
            File.SetAttributes(directory, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }
}

sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
}

sealed class NonSeekableReadStream(byte[] content) : Stream
{
    private readonly MemoryStream _inner = new(content, writable: false);

    public int BytesRead { get; private set; }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        BytesRead += read;
        return read;
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken);
        BytesRead += read;
        return read;
    }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}
