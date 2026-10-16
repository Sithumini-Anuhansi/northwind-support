using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Queries.GetTicketEscalations;

public sealed class GetTicketEscalationsQueryHandler(ITicketRepository tickets, ITicketQueries queries)
{
    public async Task<IReadOnlyList<TicketEscalationDto>> HandleAsync(int ticketId, CancellationToken ct)
    {
        _ = await tickets.GetByIdAsync(ticketId, ct) ?? throw new NotFoundException("Ticket", ticketId);
        return await queries.GetEscalationsAsync(ticketId, ct);
    }
}
