using Loans.Application;
using Loans.Application.Abstractions;
using Loans.Infrastructure;
using Loans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Loans.IntegrationTests;

/// <summary>
/// One throwaway Postgres container for the whole test run, with the real migrations and DI wiring.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string BlacklistedSsn = "123-45-6789";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public ServiceProvider Services { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public FakeExternalCustomerClient ExternalCustomers { get; } = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Loans"] = _container.GetConnectionString(),
                [$"{SsnBlacklistOptions.SectionName}:Ssns:0"] = BlacklistedSsn,
                ["ExternalCustomerService:BaseUrl"] = "http://external-customers.test/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<SsnBlacklistOptions>(configuration.GetSection(SsnBlacklistOptions.SectionName));
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton<IExternalCustomerClient>(ExternalCustomers);
        Services = services.BuildServiceProvider(validateScopes: true);

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LoansDbContext>().Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        ExternalCustomers.Reset();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LoansDbContext>().Database.ExecuteSqlRawAsync(
            """TRUNCATE "LoanApplications", "Customers", "OutboxMessages";""");
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
