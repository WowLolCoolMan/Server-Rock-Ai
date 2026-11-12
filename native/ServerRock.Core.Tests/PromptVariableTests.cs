using ServerRock.Core.Services;
using static TestSupport;

public static class PromptVariableTests
{
    public static void Run()
    {
        var now = new DateTimeOffset(2026, 11, 12, 14, 35, 0, TimeSpan.FromHours(-5));
        var expanded = PromptVariableExpander.Expand("Report for {{DATE}} at {{TIME}} ({{DATETIME}}). {{UNKNOWN}}", now);
        Check(expanded == "Report for 2026-11-12 at 14:35 (2026-11-12 14:35 -05:00). {{UNKNOWN}}",
            "prompt date/time variables expand deterministically while unknown variables remain visible");

        Check(PromptVariableExpander.Expand("{{date}}/{{Time}}", now) == "2026-11-12/14:35",
            "prompt variable names are case-insensitive");

        try
        {
            PromptVariableExpander.Expand("bad\ninput", now);
            throw new Exception("Expected control characters to be rejected.");
        }
        catch (ArgumentException)
        {
            Check(true, "prompt variable expansion rejects control characters before use");
        }
    }
}
