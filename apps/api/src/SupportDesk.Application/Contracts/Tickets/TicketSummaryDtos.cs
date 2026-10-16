using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>Just enough of a customer to label a ticket.</summary>
public sealed record CustomerSummaryDto(int Id, string Name, CustomerTier Tier);

/// <summary>A customer with the contact details shown on the ticket detail screen.</summary>
public sealed record CustomerContactDto(int Id, string Name, string Email, string? Phone, CustomerTier Tier);

/// <summary>Just enough of a category to label a ticket.</summary>
public sealed record CategorySummaryDto(int Id, string Name);

/// <summary>A category together with the handling rule that matters on a ticket.</summary>
public sealed record CategoryDto(int Id, string Name, bool RequiresSpecialist);

/// <summary>Just enough of an agent to label a ticket.</summary>
public sealed record AgentSummaryDto(int Id, string FullName);

/// <summary>Human-readable reasons behind the decisions made when a ticket is created.</summary>
public sealed record TicketTriageDto(string PriorityReason, string SlaWindowReason, string AssignmentReason);

/// <summary>A historical escalation row, including the agent labels as they are currently known.</summary>
public sealed record TicketEscalationDto(
    int Id,
    TicketPriority FromPriority,
    TicketPriority ToPriority,
    AgentSummaryDto? FromAgent,
    AgentSummaryDto? ToAgent,
    DateTime? FromDueAtUtc,
    DateTime? ToDueAtUtc,
    string Reason,
    string EscalatedBy,
    DateTime EscalatedAtUtc);

/// <summary>The current ticket after escalation and the immutable record just created.</summary>
public sealed record EscalateTicketResponse(TicketDetailDto Ticket, TicketEscalationDto Escalation);
