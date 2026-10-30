using Loans.Domain;

namespace Loans.UnitTests;

internal static class LoanRequests
{
    public static LoanRequest Create(string state = "CA", string ssn = "222-33-4444") =>
        new(
            FirstName: "Jane",
            LastName: "Doe",
            Address: Address.Create("1 Main St", "Springfield", state, "90210"),
            CompanyName: "Acme",
            Ssn: Ssn.Create(ssn),
            RequestedAmount: 10_000m);
}
