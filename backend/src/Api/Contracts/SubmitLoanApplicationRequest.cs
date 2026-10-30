using System.ComponentModel.DataAnnotations;
using Loans.Application.SubmitLoanApplication;

namespace Loans.Api.Contracts;

/// <summary>
/// HTTP contract for the loan form. Input-shape validation lives here (field-level 400s for the UI);
/// the domain still enforces its own invariants.
/// </summary>
public sealed record SubmitLoanApplicationRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, MaxLength(200)] string Street,
    [Required, MaxLength(100)] string City,
    [Required, RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "State must be a two-letter code.")] string State,
    [Required, RegularExpression(@"^\d{5}$", ErrorMessage = "Zip code must be 5 digits.")] string ZipCode,
    [Required, MaxLength(200)] string CompanyName,
    [Required, RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "SSN must be 9 digits (123-45-6789).")] string Ssn,
    [Range(0.01, 1_000_000_000, ErrorMessage = "Requested amount must be greater than zero.")] decimal RequestedAmount)
{
    public SubmitLoanApplicationCommand ToCommand() =>
        new(FirstName, LastName, Street, City, State, ZipCode, CompanyName, Ssn, RequestedAmount);
}
