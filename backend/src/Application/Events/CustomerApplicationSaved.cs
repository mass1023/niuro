using Loans.Domain;

namespace Loans.Application.Events;

/// <summary>
/// Snapshot of an approved customer and their loan application, sent as-is to the external customer service.
/// </summary>
public sealed record CustomerApplicationSaved(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string ZipCode,
    string CompanyName,
    string Ssn,
    Guid LoanApplicationId,
    decimal RequestedAmount)
{
    public static CustomerApplicationSaved From(Customer customer, LoanApplication application) =>
        new(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Address.Street,
            customer.Address.City,
            customer.Address.State,
            customer.Address.ZipCode,
            customer.CompanyName,
            customer.Ssn.Value,
            application.Id,
            application.RequestedAmount);

    // Ssn is a plain string here, so keep it out of logs.
    public override string ToString() =>
        $"{nameof(CustomerApplicationSaved)} {{ CustomerId = {CustomerId}, LoanApplicationId = {LoanApplicationId} }}";
}
