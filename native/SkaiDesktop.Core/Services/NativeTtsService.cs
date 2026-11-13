using System.Diagnostics;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>Linux-side adapter for the operating system speech stack.</summary>
public sealed class NativeTtsService
{
    private static readonly string[] LinuxEngines = ["espeak-ng", "espeak", "spd-say"];

    public bool IsLinuxRuntime => OperatingSystem.IsLinux();

    public async Task<bool> SpeakLinuxAsync(string text, VoiceProfile profile, CancellationToken cancellationToken = default)
    {
        if (!IsLinuxRuntime || string.IsNullOrWhiteSpace(text)) return false;
        var engine = FindLinuxEngine();
        if (engine is null) return false;
        var startInfo = new ProcessStartInfo
        {
            FileName = engine,
            RedirectStandardInput = !engine.Equals("spd-say", StringComparison.OrdinalIgnoreCase),
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (engine.Equals("spd-say", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add(profile.Rate.ToString());
            startInfo.ArgumentList.Add(text);
        }
        else
        {
            startInfo.ArgumentList.Add("-s");
            startInfo.ArgumentList.Add(Math.Clamp(175 + profile.Rate * 2, 80, 350).ToString());
        }
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {engine}.");
        if (!engine.Equals("spd-say", StringComparison.OrdinalIgnoreCase))
        {
            await process.StandardInput.WriteAsync(text.AsMemory(), cancellationToken);
            process.StandardInput.Close();
        }
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }

    public static string? FindLinuxEngine()
    {
        if (!OperatingSystem.IsLinux()) return null;
        foreach (var candidate in LinuxEngines)
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    ArgumentList = { candidate },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (probe is not null)
                {
                    probe.WaitForExit(500);
                    if (probe.ExitCode == 0) return candidate;
                }
            }
            catch { }
        }
        return null;
    }
}
