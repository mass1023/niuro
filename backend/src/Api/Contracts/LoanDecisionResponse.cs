using Loans.Application;

namespace Loans.Api.Contracts;

public sealed record LoanDecisionResponse(bool Approved, IReadOnlyList<string> DenialReasons)
{
    public static LoanDecisionResponse From(LoanDecision decision) => new(decision.IsApproved, decision.DenialReasons);
}
