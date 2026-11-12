using ServerRock.Core.Services;
using static TestSupport;

internal static class WorkflowApprovalTimingTests
{
    public static void Run()
    {
        var now = DateTimeOffset.UtcNow;
        Check(WorkflowApprovalTiming.RemainingLabel(now.AddSeconds(20), now) == "Less than 1 minute remaining", "approval countdown reports sub-minute urgency");
        Check(WorkflowApprovalTiming.RemainingLabel(now.AddMinutes(4).AddSeconds(1), now) == "5 minutes remaining", "approval countdown rounds up incomplete minutes");
        Check(WorkflowApprovalTiming.RemainingLabel(now.AddHours(2).AddSeconds(1), now) == "3 hours remaining", "approval countdown reports bounded hours");
        Check(WorkflowApprovalTiming.RemainingLabel(now.AddSeconds(-1), now) == "Expired", "approval countdown reports expiry");
    }
}
