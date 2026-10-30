namespace Loans.Application.SubmitLoanApplication;

public sealed record SubmitLoanApplicationCommand(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string ZipCode,
    string CompanyName,
    string Ssn,
    decimal RequestedAmount);
