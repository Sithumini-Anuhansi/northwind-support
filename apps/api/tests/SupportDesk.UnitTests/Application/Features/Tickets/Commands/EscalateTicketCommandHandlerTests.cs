using Microsoft.Extensions.Logging.Abstractions;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Agents;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public sealed class EscalateTicketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_RaisesPriorityRenewsSlaKeepsOwnerAndRecordsHistory()
    {
        var clock = new FixedClock();
        var ticket = new TicketBuilder().WithPriority(TicketPriority.Medium).AssignedTo(5).Build();
        SetId(ticket, 42);
        var category = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        SetId(category, 1);
        var agent = new AgentDto(5, "Alex Turner", "alex@example.test", true, 5, 1, []);
        TicketEscalation? addedHistory = null;

        var tickets = new Mock<ITicketRepository>();
        tickets.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var escalations = new Mock<ITicketEscalationRepository>();
        escalations.Setup(repository => repository.AddAsync(It.IsAny<TicketEscalation>(), It.IsAny<CancellationToken>()))
            .Callback<TicketEscalation, CancellationToken>((record, _) => addedHistory = record)
            .Returns(Task.CompletedTask);
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer("Contoso", "contoso@example.test", null, CustomerTier.Premium, clock.UtcNow));
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var agentQueries = new Mock<IAgentQueries>();
        agentQueries.Setup(query => query.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([agent]);
        var unitOfWork = new Mock<IUnitOfWork>();
        var ticketQueries = new Mock<ITicketQueries>();
        ticketQueries.Setup(query => query.GetDetailAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketDetailDto
            {
                Id = 42,
                Reference = "TCK-0042",
                Customer = new CustomerContactDto(1, "Contoso", "contoso@example.test", null, CustomerTier.Premium),
                Category = new CategoryDto(1, "General", false)
            });

        var handler = new EscalateTicketCommandHandler(
            tickets.Object, escalations.Object, customers.Object, categories.Object, agentQueries.Object,
            unitOfWork.Object, clock, new TicketTriageService(new SlaOptions()),
            new GetTicketQueryHandler(ticketQueries.Object),
            NullLogger<EscalateTicketCommandHandler>.Instance);

        var response = await handler.HandleAsync(42,
            new EscalateTicketRequest("  Customer impact increased  ", "  Team Lead  "), CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(clock.UtcNow.AddHours(4), ticket.DueAtUtc);
        Assert.Equal(5, ticket.AssignedAgentId);
        Assert.NotNull(addedHistory);
        Assert.Equal(TicketPriority.Medium, addedHistory.FromPriority);
        Assert.Equal(TicketPriority.High, addedHistory.ToPriority);
        Assert.Equal(5, addedHistory.FromAgentId);
        Assert.Equal(5, addedHistory.ToAgentId);
        Assert.Equal("Customer impact increased", addedHistory.Reason);
        Assert.Equal("Team Lead", addedHistory.EscalatedBy);
        Assert.Equal(clock.UtcNow, addedHistory.EscalatedAtUtc);
        Assert.Equal(addedHistory.Id, response.Escalation.Id);
        Assert.Equal("Alex Turner", response.Escalation.ToAgent?.FullName);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, TicketStatus.New, "Critical")]
    [InlineData(TicketPriority.Medium, TicketStatus.Resolved, "Resolved")]
    [InlineData(TicketPriority.Medium, TicketStatus.Closed, "Closed")]
    public async Task HandleAsync_RejectsTicketsThatCannotBeEscalated(
        TicketPriority priority, TicketStatus status, string message)
    {
        var ticket = new TicketBuilder().WithPriority(priority).WithStatus(status).Build();
        var tickets = new Mock<ITicketRepository>();
        tickets.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new EscalateTicketCommandHandler(
            tickets.Object, Mock.Of<ITicketEscalationRepository>(), Mock.Of<ICustomerRepository>(),
            Mock.Of<ICategoryRepository>(), Mock.Of<IAgentQueries>(), unitOfWork.Object,
            new FixedClock(), new TicketTriageService(new SlaOptions()),
            new GetTicketQueryHandler(Mock.Of<ITicketQueries>()),
            NullLogger<EscalateTicketCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(
            42, new EscalateTicketRequest("Valid escalation reason", "Team Lead"), CancellationToken.None));

        Assert.Contains(message, exception.Message);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetId(Entity entity, int id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
}
