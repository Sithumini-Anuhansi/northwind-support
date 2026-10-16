using SupportDesk.Application.Contracts.Agents;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Services;

/// <summary>Applies the priority, response-window, and assignment rules for ticket triage.</summary>
public sealed class TicketTriageService(SlaOptions slaOptions)
{
    public TicketTriageDecision Decide(
        Category category,
        CustomerTier customerTier,
        TicketPriority? requestedPriority,
        IReadOnlyList<AgentDto> agents,
        DateTime nowUtc)
    {
        var priority = category.ForcesCriticalPriority
            ? TicketPriority.Critical
            : requestedPriority ?? TicketPriority.Medium;
        var priorityReason = category.ForcesCriticalPriority
            ? $"The {category.Name} category forces Critical priority."
            : requestedPriority is null
                ? "No priority was requested; Medium is the default."
                : $"The requested {priority} priority was applied.";

        var window = slaOptions.WindowFor(priority, customerTier);
        var baseHours = slaOptions.BaseWindowHours[priority.ToString()];
        var windowReason = customerTier == CustomerTier.Premium
            ? $"Premium customer: {baseHours:0.##} hours × {slaOptions.PremiumCustomerMultiplier:0.##}, with a {slaOptions.MinimumWindowHours:0.##}-hour minimum, gives {window.TotalHours:0.##} hours."
            : $"{priority} priority has a {window.TotalHours:0.##}-hour response window.";

        var eligible = SelectEligibleAgent(category, agents);

        var assignmentReason = eligible is null
            ? "No active, qualified agent is below their maximum open-ticket limit; the ticket remains unassigned."
            : $"Assigned to {eligible.FullName}, who has {eligible.OpenTicketCount} open tickets (ties go to the lower agent ID).";

        return new TicketTriageDecision(
            priority,
            nowUtc.Add(window),
            eligible?.Id,
            eligible is null ? null : new AgentSummaryDto(eligible.Id, eligible.FullName),
            SlaEvaluator.Evaluate(nowUtc, nowUtc.Add(window), null, nowUtc, slaOptions.AtRiskThresholdPercent),
            priorityReason,
            windowReason,
            assignmentReason);
    }

    /// <summary>Raises priority once, renews the SLA window, and keeps/reselects a qualified owner.</summary>
    public TicketEscalationDecision DecideEscalation(
        Category category,
        CustomerTier customerTier,
        TicketPriority currentPriority,
        int? currentAgentId,
        IReadOnlyList<AgentDto> agents,
        DateTime nowUtc)
    {
        var priority = (TicketPriority)((int)currentPriority + 1);
        var window = slaOptions.WindowFor(priority, customerTier);
        var dueAtUtc = nowUtc.Add(window);
        var currentAgent = agents.FirstOrDefault(agent => agent.Id == currentAgentId);
        var currentAgentOpenCount = currentAgent is null ? 0 : Math.Max(0, currentAgent.OpenTicketCount - 1);
        var currentAgentEligible = currentAgent is { IsActive: true }
            && currentAgentOpenCount < currentAgent.MaxOpenTickets
            && (!category.RequiresSpecialist
                || currentAgent.Specializations.Any(specialization => specialization.Id == category.Id));

        if (currentAgentEligible)
        {
            return new TicketEscalationDecision(
                priority,
                dueAtUtc,
                currentAgent!.Id,
                new AgentSummaryDto(currentAgent.Id, currentAgent.FullName),
                SlaEvaluator.Evaluate(nowUtc, dueAtUtc, null, nowUtc, slaOptions.AtRiskThresholdPercent),
                $"Priority raised from {currentPriority} to {priority}.",
                $"The {priority} priority response window was reset to {window.TotalHours:0.##} hours from escalation.",
                $"Kept {currentAgent.FullName}; the agent remains active, qualified, and within capacity.");
        }

        var replacement = SelectEligibleAgent(category, agents);
        var assignmentReason = replacement is null
            ? "No active, qualified agent is below their maximum open-ticket limit; the ticket remains unassigned."
            : $"Assigned to {replacement.FullName}, who has {replacement.OpenTicketCount} open tickets (ties go to the lower agent ID).";

        return new TicketEscalationDecision(
            priority,
            dueAtUtc,
            replacement?.Id,
            replacement is null ? null : new AgentSummaryDto(replacement.Id, replacement.FullName),
            SlaEvaluator.Evaluate(nowUtc, dueAtUtc, null, nowUtc, slaOptions.AtRiskThresholdPercent),
            $"Priority raised from {currentPriority} to {priority}.",
            $"The {priority} priority response window was reset to {window.TotalHours:0.##} hours from escalation.",
            assignmentReason);
    }

    private static AgentDto? SelectEligibleAgent(Category category, IReadOnlyList<AgentDto> agents) => agents
        .Where(agent => agent.IsActive && agent.OpenTicketCount < agent.MaxOpenTickets)
        .Where(agent => !category.RequiresSpecialist
            || agent.Specializations.Any(specialization => specialization.Id == category.Id))
        .OrderBy(agent => agent.OpenTicketCount)
        .ThenBy(agent => agent.Id)
        .FirstOrDefault();
}

public sealed record TicketTriageDecision(
    TicketPriority Priority,
    DateTime DueAtUtc,
    int? AssignedAgentId,
    AgentSummaryDto? AssignedAgent,
    SlaStatus SlaStatus,
    string PriorityReason,
    string SlaWindowReason,
    string AssignmentReason);

public sealed record TicketEscalationDecision(
    TicketPriority Priority,
    DateTime DueAtUtc,
    int? AssignedAgentId,
    AgentSummaryDto? AssignedAgent,
    SlaStatus SlaStatus,
    string PriorityReason,
    string SlaWindowReason,
    string AssignmentReason);
