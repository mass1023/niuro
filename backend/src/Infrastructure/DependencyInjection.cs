using Loans.Application.Abstractions;
using Loans.Infrastructure.ExternalCustomers;
using Loans.Infrastructure.Persistence;
using Loans.Infrastructure.Persistence.Outbox;
using Loans.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Loans.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Loans")
            ?? throw new InvalidOperationException("Connection string 'Loans' is not configured.");

        var externalCustomerServiceUrl = configuration["ExternalCustomerService:BaseUrl"]
            ?? throw new InvalidOperationException("'ExternalCustomerService:BaseUrl' is not configured.");

        services.AddDbContext<LoansDbContext>(options => options.UseNpgsql(connectionString));

        // All of these share the request's scoped LoansDbContext, so one SaveChanges commits them together.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LoansDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IEventOutbox, EfEventOutbox>();

        services.AddHttpClient<IExternalCustomerClient, ExternalCustomerHttpClient>(client =>
        {
            client.BaseAddress = new Uri(externalCustomerServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
