using SupportDesk.Application.Contracts.Agents;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Services;

public sealed class TicketTriageServiceTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    [Theory]
    [InlineData(TicketPriority.Low, 72)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Critical, 4)]
    public void Decide_UsesConfiguredWindowForPriority(TicketPriority priority, double hours)
    {
        var service = new TicketTriageService(new SlaOptions());
        var category = new Category("General", false, false);

        var result = service.Decide(category, CustomerTier.Standard, priority, [], Now);

        Assert.Equal(priority, result.Priority);
        Assert.Equal(Now.AddHours(hours), result.DueAtUtc);
        Assert.Null(result.AssignedAgentId);
    }

    [Fact]
    public void Decide_PremiumCustomerGetsHalfWindowButNeverLessThanConfiguredFloor()
    {
        var options = new SlaOptions
        {
            BaseWindowHours = new Dictionary<string, double> { ["High"] = 1, ["Low"] = 72, ["Medium"] = 24, ["Critical"] = 4 },
            PremiumCustomerMultiplier = 0.5,
            MinimumWindowHours = 1
        };
        var service = new TicketTriageService(options);

        var result = service.Decide(new Category("General", false, false), CustomerTier.Premium,
            TicketPriority.High, [], Now);

        Assert.Equal(Now.AddHours(1), result.DueAtUtc);
        Assert.Contains("minimum", result.SlaWindowReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decide_CategoryFlagForcesCriticalRegardlessOfRequest()
    {
        var result = new TicketTriageService(new SlaOptions()).Decide(
            new Category("Security", true, true), CustomerTier.Standard, TicketPriority.Low, [], Now);

        Assert.Equal(TicketPriority.Critical, result.Priority);
        Assert.Equal(Now.AddHours(4), result.DueAtUtc);
        Assert.Contains("forces Critical", result.PriorityReason);
    }

    [Fact]
    public void Decide_ChoosesLeastLoadedEligibleSpecialistAndSkipsInactiveFullAndUnqualifiedAgents()
    {
        var category = new Category("Billing", true, false);
        var agents = new[]
        {
            Agent(1, open: 5, limit: 10, categoryId: category.Id),
            Agent(2, open: 3, limit: 8, categoryId: category.Id),
            Agent(3, open: 0, limit: 8, categoryId: null),
            Agent(4, open: 0, limit: 8, categoryId: category.Id, active: false),
            Agent(5, open: 6, limit: 6, categoryId: category.Id)
        };

        var result = new TicketTriageService(new SlaOptions()).Decide(
            category, CustomerTier.Standard, null, agents, Now);

        Assert.Equal(2, result.AssignedAgentId);
        Assert.Equal("Agent 2", result.AssignedAgent?.FullName);
    }

    [Fact]
    public void Decide_BreaksEqualWorkloadByLowestAgentId()
    {
        var result = new TicketTriageService(new SlaOptions()).Decide(
            new Category("General", false, false), CustomerTier.Standard, null,
            [Agent(8, 2, 10, null), Agent(3, 2, 10, null)], Now);

        Assert.Equal(3, result.AssignedAgentId);
    }

    [Fact]
    public void Decide_LeavesTicketUnassignedWhenNobodyIsEligible()
    {
        var category = new Category("Billing", true, false);
        var agents = new[]
        {
            Agent(1, 0, 8, category.Id, active: false),
            Agent(2, 8, 8, category.Id),
            Agent(3, 0, 8, null)
        };

        var result = new TicketTriageService(new SlaOptions()).Decide(
            category, CustomerTier.Standard, null, agents, Now);

        Assert.Null(result.AssignedAgentId);
        Assert.Contains("remains unassigned", result.AssignmentReason);
    }

    private static AgentDto Agent(int id, int open, int limit, int? categoryId, bool active = true) =>
        new(id, $"Agent {id}", $"agent{id}@example.test", active, limit, open,
            categoryId is null ? [] : [new CategorySummaryDto(categoryId.Value, "Category")]);
}
