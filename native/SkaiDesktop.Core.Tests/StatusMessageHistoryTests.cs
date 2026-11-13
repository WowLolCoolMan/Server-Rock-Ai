using SkaiDesktop.Core.Services;
using static TestSupport;

internal static class StatusMessageHistoryTests
{
    public static void Run()
    {
        var history = new StatusMessageHistory(2);
        history.Record("  first failure  ");
        history.Record("first failure");
        history.Record("second failure");
        history.Record("third failure");
        var snapshot = history.Snapshot();
        Check(snapshot.Count == 2 && snapshot[0] == "third failure" && snapshot[1] == "second failure", "status history is newest-first and bounded");

        history.Record(" ");
        Check(history.Snapshot().Count == 2, "status history ignores blank messages");

        history.Record(new string('x', StatusMessageHistory.MaxMessageCharacters + 20));
        Check(history.Snapshot()[0].Length == StatusMessageHistory.MaxMessageCharacters + 1 && history.Snapshot()[0].EndsWith('…'), "status history truncates oversized messages");
    }
}
