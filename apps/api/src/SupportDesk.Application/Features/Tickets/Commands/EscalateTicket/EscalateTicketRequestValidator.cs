using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public EscalateTicketRequestValidator()
    {
        RuleFor(request => request.Reason)
            .Must(reason => reason is not null && reason.Trim().Length >= 5)
            .WithMessage("Reason must be between 5 and 500 characters.")
            .Must(reason => reason is not null && reason.Trim().Length <= 500)
            .WithMessage("Reason must be between 5 and 500 characters.");

        RuleFor(request => request.EscalatedBy)
            .Must(actor => actor is not null && actor.Trim().Length > 0)
            .WithMessage("EscalatedBy is required.")
            .Must(actor => actor is not null && actor.Trim().Length <= 100)
            .WithMessage("EscalatedBy must not exceed 100 characters.");
    }
}
