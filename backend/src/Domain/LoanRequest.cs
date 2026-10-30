namespace Loans.Domain;

public sealed record LoanRequest(
    string FirstName,
    string LastName,
    Address Address,
    string CompanyName,
    Ssn Ssn,
    decimal RequestedAmount);
