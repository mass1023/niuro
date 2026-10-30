using Loans.Domain;

namespace Loans.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> FindBySsnAsync(Ssn ssn, CancellationToken cancellationToken = default);
    void Add(Customer customer);
}
