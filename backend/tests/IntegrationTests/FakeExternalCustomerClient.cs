using System.Collections.Concurrent;
using Loans.Application.Abstractions;
using Loans.Application.Events;

namespace Loans.IntegrationTests;

/// <summary>
/// Stands in for the external customer service: records what was sent and can be switched to fail.
/// </summary>
public sealed class FakeExternalCustomerClient : IExternalCustomerClient
{
    public ConcurrentQueue<CustomerApplicationSaved> Received { get; } = new();

    public bool IsDown { get; set; }

    public Task UpsertAsync(CustomerApplicationSaved snapshot, CancellationToken cancellationToken = default)
    {
        if (IsDown)
            throw new HttpRequestException("External customer service unavailable.");

        Received.Enqueue(snapshot);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        IsDown = false;
        Received.Clear();
    }
}
