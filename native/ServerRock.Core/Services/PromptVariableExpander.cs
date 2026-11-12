namespace ServerRock.Core.Services;

/// <summary>
/// Expands the small, deterministic set of date/time variables supported in
/// user-reviewed skill prompts. Unknown variables are intentionally preserved
/// so a future extension cannot silently rewrite a prompt it does not own.
/// </summary>
public static class PromptVariableExpander
{
    public const int MaxPromptCharacters = 100_000;

    public static string Expand(string prompt, DateTimeOffset localNow)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        if (prompt.Length > MaxPromptCharacters || prompt.Any(char.IsControl))
            throw new ArgumentException($"A prompt must contain at most {MaxPromptCharacters:N0} non-control characters.", nameof(prompt));

        return prompt
            .Replace("{{DATE}}", localNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{{TIME}}", localNow.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{{DATETIME}}", localNow.ToString("yyyy-MM-dd HH:mm zzz", System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }
}
