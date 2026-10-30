using Loans.Domain;

namespace Loans.Application.Abstractions;

public interface ILoanApplicationRepository
{
    Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    void Add(LoanApplication application);
}
