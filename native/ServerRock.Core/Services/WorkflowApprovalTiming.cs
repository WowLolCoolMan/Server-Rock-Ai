namespace ServerRock.Core.Services;

public static class WorkflowApprovalTiming
{
    public static string RemainingLabel(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        var remaining = expiresAt - now;
        if (remaining <= TimeSpan.Zero) return "Expired";
        if (remaining < TimeSpan.FromMinutes(1)) return "Less than 1 minute remaining";
        if (remaining < TimeSpan.FromHours(1)) return $"{Math.Ceiling(remaining.TotalMinutes):0} minutes remaining";
        return $"{Math.Ceiling(remaining.TotalHours):0} hours remaining";
    }
}
