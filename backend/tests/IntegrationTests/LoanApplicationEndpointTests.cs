using System.Net;
using System.Net.Http.Json;
using Loans.Api.Contracts;
using Loans.Application.Abstractions;
using Loans.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loans.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class LoanApplicationEndpointTests(PostgresFixture fixture) : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private static object Body(string ssn = "222-33-4444", string state = "CA") => new
    {
        firstName = "Jane",
        lastName = "Doe",
        street = "1 Main St",
        city = "Springfield",
        state,
        zipCode = "90210",
        companyName = "Acme",
        ssn,
        requestedAmount = 10_000m
    };

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:Loans", fixture.ConnectionString)
            // The real outbox processor runs inside the app; point it at the fake instead of the mock service.
            .ConfigureTestServices(services => services.AddSingleton<IExternalCustomerClient>(fixture.ExternalCustomers)));
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Post_ValidApplication_ReturnsApprovedAndPersists()
    {
        var response = await _client.PostAsJsonAsync("/loan-applications", Body());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decision = await response.Content.ReadFromJsonAsync<LoanDecisionResponse>();
        Assert.True(decision!.Approved);
        Assert.Empty(decision.DenialReasons);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoansDbContext>();
        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(1, await db.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Post_NewYorkWithBlacklistedSsn_ReturnsDeniedWithBothReasons()
    {
        // "123-45-6789" is in the API's appsettings.json blacklist.
        var response = await _client.PostAsJsonAsync("/loan-applications", Body(ssn: "123-45-6789", state: "NY"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decision = await response.Content.ReadFromJsonAsync<LoanDecisionResponse>();
        Assert.False(decision!.Approved);
        Assert.Equal(2, decision.DenialReasons.Count);
    }

    [Fact]
    public async Task Post_InvalidInput_ReturnsFieldLevelValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/loan-applications", Body(ssn: "123", state: "California"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problem!.Errors.Keys, k => k.Equals("ssn", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(problem.Errors.Keys, k => k.Equals("state", StringComparison.OrdinalIgnoreCase));
    }
}
