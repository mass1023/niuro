using Loans.Infrastructure.Persistence;
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

        services.AddDbContext<LoansDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
