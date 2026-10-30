using Loans.Application.Abstractions;
using Loans.Domain;
using Microsoft.EntityFrameworkCore;

namespace Loans.Infrastructure.Persistence.Repositories;

internal sealed class LoanApplicationRepository(LoansDbContext dbContext) : ILoanApplicationRepository
{
    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        dbContext.LoanApplications.SingleOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);

    public void Add(LoanApplication application) => dbContext.LoanApplications.Add(application);
}
