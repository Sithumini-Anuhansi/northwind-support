using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

/// <summary>Raises a ticket's priority, recalculates its owner and SLA, and records the decision.</summary>
public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ITicketEscalationRepository escalations,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentQueries agentQueries,
    IUnitOfWork unitOfWork,
    IClock clock,
    TicketTriageService triage,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task<EscalateTicketResponse> HandleAsync(
        int id,
        EscalateTicketRequest request,
        string escalatedBy,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(escalatedBy) || escalatedBy.Trim().Length > 100)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["EscalatedBy"] = ["The authenticated agent name is missing or too long."]
            });
        }

        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new ConflictException($"A {ticket.Status} ticket cannot be escalated.");

        if (ticket.Priority == TicketPriority.Critical)
            throw new ConflictException("A Critical ticket cannot be escalated.");

        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);
        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);
        var agents = await agentQueries.GetAllAsync(ct);
        var now = clock.UtcNow;
        var decision = triage.DecideEscalation(
            category, customer.Tier, ticket.Priority, ticket.AssignedAgentId, agents, now);

        var escalation = TicketEscalation.Record(
            ticket.Id,
            ticket.Priority,
            decision.Priority,
            ticket.AssignedAgentId,
            decision.AssignedAgentId,
            ticket.DueAtUtc,
            decision.DueAtUtc,
            request.Reason,
            escalatedBy.Trim(),
            now);

        ticket.ApplyEscalation(decision.Priority, decision.DueAtUtc, decision.AssignedAgentId, now);
        await escalations.AddAsync(escalation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated from {FromPriority} to {ToPriority} by {Actor}.",
            ticket.Reference, escalation.FromPriority, escalation.ToPriority, escalation.EscalatedBy);

        var updatedTicket = await getTicket.HandleAsync(id, ct);
        var fromAgent = agents.FirstOrDefault(agent => agent.Id == escalation.FromAgentId);
        var history = new TicketEscalationDto(
            escalation.Id,
            escalation.FromPriority,
            escalation.ToPriority,
            fromAgent is null ? null : new AgentSummaryDto(fromAgent.Id, fromAgent.FullName),
            decision.AssignedAgent,
            escalation.FromDueAtUtc,
            escalation.ToDueAtUtc,
            escalation.Reason,
            escalation.EscalatedBy,
            escalation.EscalatedAtUtc);

        return new EscalateTicketResponse(updatedTicket, history);
    }
}
