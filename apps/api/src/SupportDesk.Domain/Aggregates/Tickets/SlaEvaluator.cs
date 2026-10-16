namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(
        DateTime createdAtUtc,
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        double atRiskThresholdPercent)
    {
        if (dueAtUtc is null)
            return SlaStatus.NotApplicable;

        if (resolvedAtUtc is not null)
            return resolvedAtUtc <= dueAtUtc ? SlaStatus.Met : SlaStatus.Breached;

        if (nowUtc > dueAtUtc)
            return SlaStatus.Breached;

        var totalWindow = (dueAtUtc.Value - createdAtUtc).TotalSeconds;
        var remainingWindow = (dueAtUtc.Value - nowUtc).TotalSeconds;
        var remainingPercent = totalWindow <= 0 ? 0 : remainingWindow / totalWindow * 100;

        return remainingPercent <= atRiskThresholdPercent
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }
}
