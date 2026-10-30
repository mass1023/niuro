using System.Text.Json;
using Loans.Application.Abstractions;

namespace Loans.Infrastructure.Persistence.Outbox;

/// <summary>
/// Adds the event as an <see cref="OutboxMessage"/> to the same DbContext as the business changes,
/// so it is committed (or rolled back) together with them on SaveChanges.
/// </summary>
internal sealed class EfEventOutbox(LoansDbContext dbContext) : IEventOutbox
{
    public void Enqueue<TEvent>(TEvent @event) where TEvent : notnull
    {
        var payload = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(typeof(TEvent).Name, payload));
    }
}
