using SkaiDesktop.Core.Services;
using static TestSupport;

public static class PromptVariableTests
{
    public static void Run()
    {
        var now = new DateTimeOffset(2026, 11, 12, 14, 35, 0, TimeSpan.FromHours(-5));
        var expanded = PromptVariableExpander.Expand("Report for {{DATE}} at {{TIME}} ({{DATETIME}}) for {{USER_NAME}}. {{UNKNOWN}}", now, "Alex");
        Check(expanded == "Report for 2026-11-12 at 14:35 (2026-11-12 14:35 -05:00) for Alex. {{UNKNOWN}}",
            "prompt date/time and explicit user-name variables expand while unknown variables remain visible");

        Check(PromptVariableExpander.Expand("{{date}}/{{Time}}", now) == "2026-11-12/14:35",
            "prompt variable names are case-insensitive");
        Check(PromptVariableExpander.Expand("Hello {{USER_NAME}}", now) == "Hello {{USER_NAME}}",
            "unset user-name variables remain visible for review");

        try
        {
            PromptVariableExpander.Expand("bad\ninput", now);
            throw new Exception("Expected control characters to be rejected.");
        }
        catch (ArgumentException)
        {
            Check(true, "prompt variable expansion rejects control characters before use");
        }

        try
        {
            PromptVariableExpander.Expand("{{USER_NAME}}", now, "bad\nname");
            throw new Exception("Expected an invalid user-name variable to be rejected.");
        }
        catch (ArgumentException)
        {
            Check(true, "prompt variable expansion rejects unsafe explicit user-name values");
        }
    }
}
