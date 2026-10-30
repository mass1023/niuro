using Loans.Application.Events;

namespace Loans.Application.Abstractions;

/// <summary>
/// The external customer service. Upsert keyed by our customer id: creates the record the first time,
/// replaces it afterwards, so redelivering the same snapshot is harmless.
/// </summary>
public interface IExternalCustomerClient
{
    Task UpsertAsync(CustomerApplicationSaved snapshot, CancellationToken cancellationToken = default);
}
