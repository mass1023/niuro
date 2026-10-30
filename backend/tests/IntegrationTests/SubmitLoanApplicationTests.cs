using System.Text.Json;
using Loans.Application;
using Loans.Application.Events;
using Loans.Application.SubmitLoanApplication;
using Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loans.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SubmitLoanApplicationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static SubmitLoanApplicationCommand Command(
        string ssn = "222-33-4444",
        string state = "CA",
        string firstName = "Jane",
        string companyName = "Acme",
        decimal requestedAmount = 10_000m) =>
        new(firstName, "Doe", "1 Main St", "Springfield", state, "90210", companyName, ssn, requestedAmount);

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task NewCustomer_SavesCustomerApplicationAndOutboxMessage()
    {
        var decision = await SubmitAsync(Command());

        Assert.True(decision.IsApproved);

        await using var db = NewDbContext(out var scope);
        await using var _ = scope;

        var customer = Assert.Single(await db.Customers.ToListAsync());
        var application = Assert.Single(await db.LoanApplications.ToListAsync());
        var message = Assert.Single(await db.OutboxMessages.ToListAsync());

        Assert.Equal(customer.Id, application.CustomerId);
        Assert.Equal(10_000m, application.RequestedAmount);
        Assert.Equal(nameof(CustomerApplicationSaved), message.Type);
        Assert.Null(message.ProcessedAtUtc);

        var @event = JsonSerializer.Deserialize<CustomerApplicationSaved>(message.Payload, JsonSerializerOptions.Web)!;
        Assert.Equal(customer.Id, @event.CustomerId);
        Assert.Equal(application.Id, @event.LoanApplicationId);
    }

    [Fact]
    public async Task ReturningCustomer_UpdatesTheSameCustomerAndApplication()
    {
        await SubmitAsync(Command(ssn: "222-33-4444", firstName: "Jane", companyName: "Acme", requestedAmount: 10_000m));
        await SubmitAsync(Command(ssn: "222334444", firstName: "Janet", companyName: "Globex", requestedAmount: 25_000m));

        await using var db = NewDbContext(out var scope);
        await using var _ = scope;

        var customer = Assert.Single(await db.Customers.ToListAsync());
        var application = Assert.Single(await db.LoanApplications.ToListAsync());

        Assert.Equal("Janet", customer.FirstName);
        Assert.Equal("Globex", customer.CompanyName);
        Assert.Equal(25_000m, application.RequestedAmount);

        // Two events, both for the same customer and application, so the external service updates rather than creates.
        var events = (await db.OutboxMessages.OrderBy(m => m.OccurredAtUtc).ToListAsync())
            .Select(m => JsonSerializer.Deserialize<CustomerApplicationSaved>(m.Payload, JsonSerializerOptions.Web)!)
            .ToList();

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(customer.Id, e.CustomerId));
        Assert.All(events, e => Assert.Equal(application.Id, e.LoanApplicationId));
        Assert.Equal(25_000m, events[^1].RequestedAmount);
    }

    [Theory]
    [InlineData(PostgresFixture.BlacklistedSsn, "CA")]
    [InlineData("222-33-4444", "NY")]
    public async Task DeniedApplication_SavesNothing(string ssn, string state)
    {
        var decision = await SubmitAsync(Command(ssn: ssn, state: state));

        Assert.False(decision.IsApproved);
        await AssertDatabaseIsEmptyAsync();
    }

    [Fact]
    public async Task WhenOutboxInsertFails_CustomerAndApplicationAreRolledBack()
    {
        // Make Postgres itself reject the outbox row, after the customer and application inserts in the same transaction.
        await ExecuteSqlAsync("""
            CREATE FUNCTION reject_outbox() RETURNS trigger AS $$
            BEGIN RAISE EXCEPTION 'outbox unavailable'; END $$ LANGUAGE plpgsql;
            CREATE TRIGGER reject_outbox BEFORE INSERT ON "OutboxMessages"
            FOR EACH ROW EXECUTE FUNCTION reject_outbox();
            """);

        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => SubmitAsync(Command()));
        }
        finally
        {
            await ExecuteSqlAsync("""
                DROP TRIGGER reject_outbox ON "OutboxMessages";
                DROP FUNCTION reject_outbox();
                """);
        }

        await AssertDatabaseIsEmptyAsync();
    }

    private async Task<LoanDecision> SubmitAsync(SubmitLoanApplicationCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<SubmitLoanApplicationHandler>();
        return await handler.HandleAsync(command);
    }

    private LoansDbContext NewDbContext(out AsyncServiceScope scope)
    {
        scope = fixture.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<LoansDbContext>();
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LoansDbContext>().Database.ExecuteSqlRawAsync(sql);
    }

    private async Task AssertDatabaseIsEmptyAsync()
    {
        await using var db = NewDbContext(out var scope);
        await using var _ = scope;

        Assert.Empty(await db.Customers.ToListAsync());
        Assert.Empty(await db.LoanApplications.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
    }
}
