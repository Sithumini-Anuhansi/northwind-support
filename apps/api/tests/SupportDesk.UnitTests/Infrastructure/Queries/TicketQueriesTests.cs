using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Services;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public sealed class TicketQueriesTests
{
    [Fact]
    public async Task GetPagedAsync_filters_before_count_and_page_and_searches_customer_name()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SupportDbContext>().UseSqlite(connection).Options;

        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var contoso = new Customer("Contoso", "contoso@example.test", null, CustomerTier.Premium, FixedClock.DefaultNow);
        var fabrikam = new Customer("Fabrikam", "fabrikam@example.test", null, CustomerTier.Standard, FixedClock.DefaultNow);
        var general = new Category("General", false, false);
        db.AddRange(contoso, fabrikam, general);
        await db.SaveChangesAsync();

        db.AddRange(
            Ticket.Raise("TCK-0001", "Invoice question", "Description", contoso.Id, general.Id, TicketPriority.High, FixedClock.DefaultNow),
            Ticket.Raise("TCK-0002", "Delivery issue", "Description", contoso.Id, general.Id, TicketPriority.Low, FixedClock.DefaultNow),
            Ticket.Raise("TCK-0003", "Invoice question", "Description", fabrikam.Id, general.Id, TicketPriority.High, FixedClock.DefaultNow));
        await db.SaveChangesAsync();

        var queries = new TicketQueries(db, new FixedClock(), new SlaOptions());

        var priority = await queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High }, CancellationToken.None);
        Assert.Equal(2, priority.TotalCount);
        Assert.All(priority.Items, item => Assert.Equal(TicketPriority.High, item.Priority));

        var combined = await queries.GetPagedAsync(new TicketQuery
        {
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CustomerId = contoso.Id,
            PageSize = 1
        }, CancellationToken.None);
        Assert.Equal(1, combined.TotalCount);
        Assert.Equal(1, combined.TotalPages);
        Assert.Equal("TCK-0001", Assert.Single(combined.Items).Reference);

        var customerSearch = await queries.GetPagedAsync(
            new TicketQuery { Search = "  contoso  " }, CancellationToken.None);
        Assert.Equal(2, customerSearch.TotalCount);
        Assert.All(customerSearch.Items, item => Assert.Equal("Contoso", item.Customer.Name));

        var firstPage = await queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High, PageSize = 1, Page = 1 }, CancellationToken.None);
        var secondPage = await queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High, PageSize = 1, Page = 2 }, CancellationToken.None);
        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Single(firstPage.Items);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);
    }
}
