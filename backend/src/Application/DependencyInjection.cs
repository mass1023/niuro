using Loans.Domain;
using Loans.Application.Policies;
using Loans.Application.SubmitLoanApplication;
using Microsoft.Extensions.DependencyInjection;

namespace Loans.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDenialPolicy, NewYorkStatePolicy>();
        services.AddSingleton<IDenialPolicy, BlacklistedSsnPolicy>();
        services.AddSingleton<LoanDecisionEngine>();
        services.AddScoped<SubmitLoanApplicationHandler>();

        return services;
    }
}