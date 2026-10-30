using Loans.Application.SubmitLoanApplication;
using Loans.Infrastructure.Persistence;
using Loans.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Loans.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class OutboxProcessorTests(PostgresFixture fixture) : IAsyncLifetime
{
    private OutboxProcessor Processor =>
        new(fixture.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance);

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PendingMessages_AreDeliveredInOrderAndMarkedProcessed()
    {
        await SubmitAsync(10_000m);
        await SubmitAsync(25_000m);

        var delivered = await Processor.ProcessPendingAsync();

        Assert.Equal(2, delivered);
        Assert.Equal([10_000m, 25_000m], fixture.ExternalCustomers.Received.Select(s => s.RequestedAmount));
        Assert.All(await OutboxAsync(), m => Assert.NotNull(m.ProcessedAtUtc));

        // Nothing left to send.
        Assert.Equal(0, await Processor.ProcessPendingAsync());
    }

    [Fact]
    public async Task WhenServiceIsDown_MessagesStayPendingAndBatchStops_ThenAreRetriedInOrder()
    {
        await SubmitAsync(10_000m);
        await SubmitAsync(25_000m);
        fixture.ExternalCustomers.IsDown = true;

        Assert.Equal(0, await Processor.ProcessPendingAsync());

        var afterFailure = await OutboxAsync();
        Assert.All(afterFailure, m => Assert.Null(m.ProcessedAtUtc));
        Assert.Equal(1, afterFailure[0].Attempts);
        Assert.NotNull(afterFailure[0].LastError);
        Assert.Equal(0, afterFailure[1].Attempts); // batch stopped at the first failure

        fixture.ExternalCustomers.IsDown = false;

        Assert.Equal(2, await Processor.ProcessPendingAsync());
        Assert.Equal([10_000m, 25_000m], fixture.ExternalCustomers.Received.Select(s => s.RequestedAmount));
    }

    private async Task SubmitAsync(decimal requestedAmount)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SubmitLoanApplicationHandler>().HandleAsync(
            new SubmitLoanApplicationCommand("Jane", "Doe", "1 Main St", "Springfield", "CA", "90210", "Acme", "222-33-4444", requestedAmount));
    }

    private async Task<List<OutboxMessage>> OutboxAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LoansDbContext>().OutboxMessages
            .OrderBy(m => m.OccurredAtUtc)
            .ToListAsync();
    }
}
