using ServerRock.Core.Services;

internal static class NavigationHistoryTests
{
    public static void Run()
    {
        var history = new NavigationHistory("chat");
        history.Record("coding");
        history.Record("settings");
        Check(history.CanGoBack && history.PeekBack() == "coding", "navigation history exposes the previous route");
        Check(history.MoveBack() == "coding" && history.CanGoForward, "navigation history moves backward and forward");
        Check(history.MoveForward() == "settings", "navigation history restores the forward route");
        Check(history.MoveBack() == "coding", "navigation history returns to the prior route");
        history.Record("projects");
        Check(!history.CanGoForward && history.Current == "projects", "new navigation branches truncate forward history");

        for (var index = 0; index < NavigationHistory.MaxEntries + 4; index++) history.Record("route-" + index);
        Check(history.Current == "route-35" && !history.CanGoForward, "navigation history remains bounded");
        Console.WriteLine("PASS bounded branch-aware navigation history");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL " + message);
    }
}
