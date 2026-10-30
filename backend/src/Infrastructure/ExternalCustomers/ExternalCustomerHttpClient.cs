using System.Net.Http.Json;
using System.Text.Json;
using Loans.Application.Abstractions;
using Loans.Application.Events;

namespace Loans.Infrastructure.ExternalCustomers;

internal sealed class ExternalCustomerHttpClient(HttpClient httpClient) : IExternalCustomerClient
{
    public async Task UpsertAsync(CustomerApplicationSaved snapshot, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"customers/{snapshot.CustomerId}", snapshot, JsonSerializerOptions.Web, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
