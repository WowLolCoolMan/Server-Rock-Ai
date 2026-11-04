using System.Text;
using Porta.Pty;
using XTerm;
using XTerm.Common;
using XTerm.Options;

namespace ServerRock.Core.Infrastructure;

/// <summary>A real pseudo-terminal and VT screen buffer scoped to a selected workspace folder.</summary>
public sealed class WorkspaceTerminalSession : IAsyncDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly TaskCompletionSource<int?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IPtyConnection? _connection;
    private Task? _readLoop;
    private bool _disposed;
    // Settings for waiting out late output after the shell exits (see OnProcessExited).
    private static readonly TimeSpan OutputQuietPeriod = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MaxOutputSettleTime = TimeSpan.FromSeconds(3);
    private long _lastOutputTicks;
    private int _settleStarted;

    public WorkspaceTerminalSession(int columns = 100, int rows = 26)
    {
        Terminal = new Terminal(new TerminalOptions
        {
            Cols = Math.Clamp(columns, 40, 240),
            Rows = Math.Clamp(rows, 12, 80),
            Scrollback = 2_000,
            TermName = "xterm-256color",
            CursorBlink = true
        });
        Terminal.DataReceived += OnTerminalDataReceived;
        Terminal.WindowInfoRequested += OnWindowInfoRequested;
    }

    public Terminal Terminal { get; }
    public string ScreenText
    {
        get
        {
            var start = Math.Max(0, Terminal.Buffer.YDisp);
            return string.Join("\n", Enumerable.Range(start, Terminal.Rows)
                .Select(index => Terminal.Buffer.Lines[index]?.TranslateToString(trimRight: true) ?? string.Empty));
        }
    }
    public Task<int?> Completion => _completed.Task;
    public int? ExitCode { get; private set; }
    public event EventHandler? ScreenChanged;

    public async Task StartAsync(string selectedRoot, string relativeWorkingDirectory = ".", string? shell = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connection is not null) throw new InvalidOperationException("This terminal session has already started.");
        var workingDirectory = WorkspaceCommandRunner.ResolveWorkingDirectory(selectedRoot, relativeWorkingDirectory);
        var selectedShell = ResolveShell(shell);
        var options = new PtyOptions
        {
            Name = "Server Rock Coding Terminal",
            Cols = Terminal.Cols,
            Rows = Terminal.Rows,
            Cwd = workingDirectory,
            App = selectedShell.App,
            CommandLine = selectedShell.Arguments
        };
        _connection = await PtyProvider.SpawnAsync(options, cancellationToken).ConfigureAwait(false);
        _connection.ProcessExited += OnProcessExited;
        _readLoop = ReadOutputAsync(_stopping.Token);
    }

    public async ValueTask SendInputAsync(ReadOnlyMemory<byte> input, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var connection = _connection ?? throw new InvalidOperationException("The terminal session has not started.");
        if (input.IsEmpty) return;
        await connection.WriterStream.WriteAsync(input, cancellationToken).ConfigureAwait(false);
        await connection.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask SendTextAsync(string text, CancellationToken cancellationToken = default) =>
        SendInputAsync(Encoding.UTF8.GetBytes(text), cancellationToken);

    public async Task ResizeAsync(int columns, int rows)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var safeColumns = Math.Clamp(columns, 40, 240);
        var safeRows = Math.Clamp(rows, 12, 80);
        Terminal.Resize(safeColumns, safeRows);
        var connection = _connection;
        if (connection is not null) await Task.Run(() => connection.Resize(safeColumns, safeRows)).ConfigureAwait(false);
        ScreenChanged?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _stopping.Cancel();
        try { _connection?.Kill(); }
        catch (InvalidOperationException) { }
        if (_readLoop is not null)
        {
            try { await _readLoop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (IOException) { }
        }
        if (_connection is not null)
        {
            _connection.ProcessExited -= OnProcessExited;
            _connection.Dispose();
        }
        _connection = null;
        Terminal.DataReceived -= OnTerminalDataReceived;
        Terminal.WindowInfoRequested -= OnWindowInfoRequested;
        Terminal.Dispose();
        _stopping.Dispose();
    }

    private void OnProcessExited(object? sender, PtyExitedEventArgs args)
    {
        ExitCode = args.ExitCode;
        // Don't report completion yet. On Windows, ConPTY can deliver the shell's final output
        // after this exit event, and with blocking pipes the output stream stays open until the
        // session is disposed. Completing here let callers read ScreenText before the last output
        // arrived (this failed the Windows CI terminal check). Wait for output to go quiet instead;
        // the read loop completes immediately if the stream ends first (the usual case on Linux).
        Interlocked.Exchange(ref _lastOutputTicks, Environment.TickCount64);
        if (Interlocked.Exchange(ref _settleStarted, 1) == 0) _ = CompleteAfterOutputSettlesAsync();
        ScreenChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task CompleteAfterOutputSettlesAsync()
    {
        var deadline = Environment.TickCount64 + (long)MaxOutputSettleTime.TotalMilliseconds;
        while (!_completed.Task.IsCompleted && Environment.TickCount64 < deadline)
        {
            var quietFor = Environment.TickCount64 - Interlocked.Read(ref _lastOutputTicks);
            if (quietFor >= OutputQuietPeriod.TotalMilliseconds) break;
            try { await Task.Delay(50, _stopping.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
        _completed.TrySetResult(ExitCode);
    }

    // Process.ExitCode throws until the process has exited, so the read loop can't read it blindly.
    private int? TryReadExitCode(IPtyConnection connection)
    {
        if (ExitCode is not null) return ExitCode;
        try { return connection.ExitCode; }
        catch (InvalidOperationException) { return null; }
    }

    private async Task ReadOutputAsync(CancellationToken cancellationToken)
    {
        var connection = _connection!;
        var buffer = new byte[8192];
        try
        {
            while (true)
            {
                var read = await connection.ReaderStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read <= 0) break;
                Interlocked.Exchange(ref _lastOutputTicks, Environment.TickCount64);
                Terminal.Write(buffer.AsSpan(0, read));
                ScreenChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) when (cancellationToken.IsCancellationRequested) { }
        finally
        {
            // The output stream ended, so all output has been rendered and it is safe to complete.
            ExitCode = TryReadExitCode(connection);
            _completed.TrySetResult(ExitCode);
            ScreenChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnTerminalDataReceived(object? sender, XTerm.Events.TerminalEvents.DataEventArgs args)
    {
        if (_connection is null || _disposed || string.IsNullOrEmpty(args.Data)) return;
        _ = ForwardTerminalReplyAsync(args.Data);
    }

    private async Task ForwardTerminalReplyAsync(string data)
    {
        try { await SendTextAsync(data, _stopping.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    private void OnWindowInfoRequested(object? sender, XTerm.Events.TerminalEvents.WindowInfoRequestedEventArgs args)
    {
        switch (args.Request)
        {
            case WindowInfoRequest.SizePixels:
            case WindowInfoRequest.ScreenSizePixels:
                args.WidthPixels = Terminal.Cols * Terminal.Options.CellWidthPixels;
                args.HeightPixels = Terminal.Rows * Terminal.Options.CellHeightPixels;
                args.Handled = true;
                break;
            case WindowInfoRequest.CellSizePixels:
                args.CellWidth = Terminal.Options.CellWidthPixels;
                args.CellHeight = Terminal.Options.CellHeightPixels;
                args.Handled = true;
                break;
            case WindowInfoRequest.Title:
            case WindowInfoRequest.IconTitle:
                args.Title = "Server Rock terminal";
                args.Handled = true;
                break;
        }
    }

    private static (string App, string[] Arguments) ResolveShell(string? shell)
    {
        if (OperatingSystem.IsWindows())
        {
            if (string.Equals(shell, "Command Prompt", StringComparison.OrdinalIgnoreCase))
                return (Path.Combine(Environment.SystemDirectory, "cmd.exe"), []);
            return ("powershell.exe", ["-NoLogo", "-NoProfile"]);
        }
        var selected = string.IsNullOrWhiteSpace(shell) ? Environment.GetEnvironmentVariable("SHELL") : shell;
        var app = !string.IsNullOrWhiteSpace(selected) && Path.IsPathRooted(selected) && File.Exists(selected)
            ? selected
            : File.Exists("/bin/bash") ? "/bin/bash" : "/bin/sh";
        return (app, []);
    }
}
