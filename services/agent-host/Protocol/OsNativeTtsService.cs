using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace ServerRock.AgentHost.Protocol;

/// <summary>
/// Starts and interrupts the operating system's local text-to-speech process.
/// </summary>
/// <remarks>
/// The service owns one speech process at a time. Starting a new utterance stops
/// the prior one, which gives the desktop voice interaction barge-in semantics.
/// </remarks>
internal sealed class OsNativeTtsService : IDisposable
{
    private readonly object gate = new();
    private Process? activeProcess;

    /// <summary>
    /// Starts speech for the supplied bounded text.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="rate">The normalized speaking rate from 0.5 through 2.</param>
    /// <returns>The identifier of the operating-system speech engine.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the platform has no supported local speech engine.</exception>
    public string Speak(string text, double rate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Stop();
        var startInfo = OperatingSystem.IsWindows()
            ? CreateWindowsStartInfo(text, rate)
            : CreateLinuxStartInfo(text, rate);
        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new InvalidOperationException("The OS speech process could not start.");
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("A supported OS-native speech engine is unavailable.", exception);
        }

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => ClearCompletedProcess(process);
        lock (gate)
        {
            activeProcess = process;
        }

        if (startInfo.RedirectStandardInput)
        {
            process.StandardInput.Write(text);
            process.StandardInput.Close();
        }

        return OperatingSystem.IsWindows() ? "windows-speech" : "linux-speech";
    }

    /// <summary>
    /// Interrupts the current utterance, if one is active.
    /// </summary>
    public void Stop()
    {
        Process? process;
        lock (gate)
        {
            process = activeProcess;
            activeProcess = null;
        }

        if (process is null) return;
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The speech process completed between the state check and kill.
        }
        finally
        {
            process.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    private static ProcessStartInfo CreateWindowsStartInfo(string text, double rate)
    {
        const string script = "$t=$args[0]; Add-Type -AssemblyName System.Speech; $s=New-Object System.Speech.Synthesis.SpeechSynthesizer; $s.Rate=[Math]::Max(-10,[Math]::Min(10,[int][Math]::Round((([double]$env:SKAI_TTS_RATE)-1)*10))); $s.Speak($t)";
        var startInfo = CreateBaseStartInfo("powershell.exe");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);
        startInfo.ArgumentList.Add(text);
        startInfo.Environment["SKAI_TTS_RATE"] = rate.ToString(CultureInfo.InvariantCulture);
        return startInfo;
    }

    private static ProcessStartInfo CreateLinuxStartInfo(string text, double rate)
    {
        foreach (var engine in new[] { "espeak-ng", "espeak" })
        {
            if (!IsAvailableOnPath(engine)) continue;
            var startInfo = CreateBaseStartInfo(engine);
            startInfo.ArgumentList.Add("-s");
            startInfo.ArgumentList.Add(Math.Clamp((int)Math.Round(175 * rate), 80, 450).ToString(CultureInfo.InvariantCulture));
            startInfo.RedirectStandardInput = true;
            return startInfo;
        }

        if (IsAvailableOnPath("spd-say"))
        {
            var startInfo = CreateBaseStartInfo("spd-say");
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add(Math.Clamp((int)Math.Round((rate - 1) * 100), -100, 100).ToString(CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add(text);
            return startInfo;
        }

        throw new InvalidOperationException("Install espeak-ng, espeak, or speech-dispatcher to use OS-native speech on Linux.");
    }

    private static ProcessStartInfo CreateBaseStartInfo(string fileName) => new(fileName)
    {
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
    };

    private static bool IsAvailableOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return false;
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => File.Exists(Path.Combine(directory, fileName)));
    }

    private void ClearCompletedProcess(Process completedProcess)
    {
        lock (gate)
        {
            if (!ReferenceEquals(activeProcess, completedProcess)) return;
            activeProcess = null;
        }
        completedProcess.Dispose();
    }
}
