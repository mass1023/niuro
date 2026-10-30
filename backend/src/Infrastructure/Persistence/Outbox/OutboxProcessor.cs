using System.Text.Json;
using Loans.Application.Abstractions;
using Loans.Application.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loans.Infrastructure.Persistence.Outbox;

/// <summary>
/// Delivers pending outbox messages to the external customer service, outside any HTTP request.
/// At-least-once: a message stays pending until the service returns success. Messages go out
/// oldest first and a failure stops the batch, so an older snapshot never overwrites a newer one.
/// </summary>
public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);

        do
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Outbox processing failed; retrying on the next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <returns>The number of messages delivered.</returns>
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IExternalCustomerClient>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.OccurredAtUtc)
            .ThenBy(m => m.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var delivered = 0;
        foreach (var message in pending)
        {
            try
            {
                await client.UpsertAsync(Deserialize(message), cancellationToken);
                message.MarkProcessed();
                delivered++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RecordFailure(ex.Message);
                logger.LogWarning(ex, "Outbox message {MessageId} failed (attempt {Attempts}); stopping batch to keep order.",
                    message.Id, message.Attempts);
            }

            // Persist each outcome right away, so a crash mid-batch doesn't resend what was already delivered.
            await dbContext.SaveChangesAsync(cancellationToken);

            if (message.ProcessedAtUtc is null)
                break;
        }

        return delivered;
    }

    private static CustomerApplicationSaved Deserialize(OutboxMessage message) =>
        message.Type == nameof(CustomerApplicationSaved)
            ? JsonSerializer.Deserialize<CustomerApplicationSaved>(message.Payload, JsonSerializerOptions.Web)!
            : throw new InvalidOperationException($"Unknown outbox message type '{message.Type}'.");
}
