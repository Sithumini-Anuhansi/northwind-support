using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Aggregates.Customers;

namespace SupportDesk.Application.Features.Tickets.Services;

/// <summary>Configurable SLA windows and thresholds from the Sla configuration section.</summary>
public sealed class SlaOptions
{
    public Dictionary<string, double> BaseWindowHours { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(TicketPriority.Low)] = 72,
        [nameof(TicketPriority.Medium)] = 24,
        [nameof(TicketPriority.High)] = 8,
        [nameof(TicketPriority.Critical)] = 4
    };

    public double PremiumCustomerMultiplier { get; init; } = 0.5;

    public double MinimumWindowHours { get; init; } = 1;

    public double AtRiskThresholdPercent { get; init; } = 25;

    public void Validate()
    {
        foreach (var priority in Enum.GetNames<TicketPriority>())
        {
            if (!BaseWindowHours.TryGetValue(priority, out var hours) || hours <= 0)
                throw new InvalidOperationException($"Sla:BaseWindowHours:{priority} must be a positive number.");
        }

        if (PremiumCustomerMultiplier <= 0)
            throw new InvalidOperationException("Sla:PremiumCustomerMultiplier must be positive.");

        if (MinimumWindowHours <= 0)
            throw new InvalidOperationException("Sla:MinimumWindowHours must be positive.");

        if (AtRiskThresholdPercent is < 0 or > 100)
            throw new InvalidOperationException("Sla:AtRiskThresholdPercent must be between 0 and 100.");
    }

    public TimeSpan WindowFor(TicketPriority priority, CustomerTier tier)
    {
        Validate();
        var hours = BaseWindowHours[priority.ToString()];
        if (tier == CustomerTier.Premium)
            hours = Math.Max(MinimumWindowHours, hours * PremiumCustomerMultiplier);

        return TimeSpan.FromHours(hours);
    }
}
