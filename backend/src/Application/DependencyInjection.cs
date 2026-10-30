using Loans.Domain;
using Loans.Application.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Loans.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDenialPolicy, NewYorkStatePolicy>();
        services.AddSingleton<IDenialPolicy, BlacklistedSsnPolicy>();
        services.AddSingleton<LoanDecisionEngine>();

        return services;
    }
}