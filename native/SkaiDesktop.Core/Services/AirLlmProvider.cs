using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SkaiDesktop.Core.Infrastructure;
using SkaiDesktop.Core.Models;

namespace SkaiDesktop.Core.Services;

/// <summary>
/// Local AirLLM bridge. The Python worker is deliberately kept behind a small
/// JSON-lines protocol so the native app owns cancellation, lifecycle, and
/// provenance while AirLLM owns layer-wise inference.
/// </summary>
public sealed class AirLlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _requestGate = new(1, 1);

    public bool CanHandle(AppSettings settings)
        => IsAirLlmProvider(settings.ModelProvider) || (settings.AirLlmAutoFallbackEnabled && IsHuggingFaceModel(settings.Model));

    public bool IsInstalled => ResolveScriptPath() is not null;

    public bool IsRuntimeAvailable(AppSettings settings)
    {
        var script = ResolveScriptPath();
        if (script is null) return false;
        try
        {
            var start = new ProcessStartInfo
            {
                FileName = ResolvePythonPath(settings.AirLlmPythonPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(script) ?? AppContext.BaseDirectory,
            };
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("import airllm, torch");
            using var process = Process.Start(start);
            if (process is null) return false;
            if (!process.WaitForExit(10_000))
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                return false;
            }
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool IsAirLlmProvider(string? provider)
        => string.Equals(provider, "AirLLM", StringComparison.OrdinalIgnoreCase);

    public static bool IsHuggingFaceModel(string? model)
        => !string.IsNullOrWhiteSpace(model) && model.Contains('/', StringComparison.Ordinal)
            && !model.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            && !model.Contains(':', StringComparison.Ordinal)
            && !model.Any(char.IsControl) && !model.Any(char.IsWhiteSpace);

    public static bool IsCapacityFailure(Exception exception)
    {
        var text = exception.ToString();
        return text.Contains("out of memory", StringComparison.OrdinalIgnoreCase)
            || text.Contains("cuda out of memory", StringComparison.OrdinalIgnoreCase)
            || text.Contains("memory", StringComparison.OrdinalIgnoreCase) && text.Contains("model", StringComparison.OrdinalIgnoreCase)
            || text.Contains("does not fit", StringComparison.OrdinalIgnoreCase)
            || text.Contains("model compatibility", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldAutoFallback(AppSettings settings, string endpoint, Exception exception, bool runtimeInstalled)
        => settings.AirLlmAutoFallbackEnabled
            && runtimeInstalled
            && LocalEndpoint.IsLocal(endpoint)
            && !IsAirLlmProvider(settings.ModelProvider)
            && IsHuggingFaceModel(settings.Model)
            && IsCapacityFailure(exception);

    public async Task StreamChatAsync(AppSettings settings, IReadOnlyList<ChatMessage> messages, Action<string> onText,
        CancellationToken cancellationToken, int? maxTokens = null, string? hfToken = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(onText);
        if (!IsHuggingFaceModel(settings.Model) && !Directory.Exists(settings.Model))
            throw new InvalidOperationException("AirLLM models must be a Hugging Face repository ID such as Qwen/Qwen3-32B or a local model directory.");

        var script = ResolveScriptPath();
        if (script is null)
            throw new InvalidOperationException("The built-in AirLLM runtime is not installed. Install the Skai AirLLM runtime or set SKAI_AIRLLM_SCRIPT to its worker path.");

        await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = new
            {
                op = "chat",
                model = settings.Model,
                messages = messages.Select(message => new { role = message.Role, content = message.Content }).ToArray(),
                max_new_tokens = Math.Clamp(maxTokens ?? 1024, 1, 4096),
                temperature = Math.Clamp(settings.Temperature, 0, 2),
                compression = NormalizeCompression(settings.AirLlmCompression),
                device = NormalizeDevice(settings.AirLlmDevice),
                cache_path = settings.AirLlmCachePath,
                delete_original = settings.AirLlmDeleteOriginal,
                allow_remote_code = settings.AirLlmAllowRemoteCode,
                hf_token = string.IsNullOrWhiteSpace(hfToken) ? null : hfToken.Trim()
            };
            await RunWorkerAsync(ResolvePythonPath(settings.AirLlmPythonPath), script, JsonSerializer.Serialize(request, JsonOptions), onText, cancellationToken).ConfigureAwait(false);
        }
        finally { _requestGate.Release(); }
    }

    private static async Task RunWorkerAsync(string python, string script, string payload, Action<string> onText, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(python) ? "python" : python,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(script) ?? AppContext.BaseDirectory
        };
        start.ArgumentList.Add(script);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the AirLLM Python runtime.");
        _ = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteLineAsync(payload).ConfigureAwait(false);
        await process.StandardInput.FlushAsync().ConfigureAwait(false);
        process.StandardInput.Close();
        var gotText = false;
        try
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (line.Length > 2_000_000) throw new InvalidDataException("AirLLM returned an oversized event.");
                using var eventJson = JsonDocument.Parse(line);
                var root = eventJson.RootElement;
                var type = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
                if (type == "text")
                {
                    var text = root.GetProperty("text").GetString() ?? string.Empty;
                    if (text.Length > 0) { gotText = true; onText(text); }
                }
                else if (type == "error")
                    throw new InvalidOperationException(root.TryGetProperty("message", out var message) ? message.GetString() : "AirLLM inference failed.");
                else if (type == "done") break;
            }
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0) throw new InvalidOperationException($"AirLLM exited with code {process.ExitCode}.");
            if (!gotText) throw new InvalidOperationException("AirLLM closed without returning a response.");
        }
        catch
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw;
        }
    }

    private static string? ResolveScriptPath()
    {
        var configured = Environment.GetEnvironmentVariable("SKAI_AIRLLM_SCRIPT");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return Path.GetFullPath(configured);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "airllm", "skai_airllm.py"),
            Path.Combine(AppContext.BaseDirectory, "services", "airllm", "skai_airllm.py"),
            Path.Combine(Environment.CurrentDirectory, "services", "airllm", "skai_airllm.py")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string ResolvePythonPath(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && !configured.Equals("python", StringComparison.OrdinalIgnoreCase) && !configured.Equals("python3", StringComparison.OrdinalIgnoreCase))
            return configured;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Skai", "AirLLM", "venv");
        var managed = OperatingSystem.IsWindows() ? Path.Combine(root, "Scripts", "python.exe") : Path.Combine(root, "bin", "python");
        return File.Exists(managed) ? managed : (string.IsNullOrWhiteSpace(configured) ? "python" : configured);
    }

    private static string NormalizeCompression(string? value) => value?.ToLowerInvariant() switch { "4bit" => "4bit", "8bit" => "8bit", _ => "none" };
    private static string NormalizeDevice(string? value) => value?.ToLowerInvariant() switch { "cpu" => "cpu", "cuda" => "cuda", "mps" => "mps", _ => "auto" };
}
