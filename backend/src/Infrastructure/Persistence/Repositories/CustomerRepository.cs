using Loans.Application.Abstractions;
using Loans.Domain;
using Microsoft.EntityFrameworkCore;

namespace Loans.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(LoansDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken = default) =>
        dbContext.Customers.SingleOrDefaultAsync(c => c.Ssn == ssn, cancellationToken);

    public void Add(Customer customer) => dbContext.Customers.Add(customer);
}
