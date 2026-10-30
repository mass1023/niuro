using Loans.Domain;

namespace Loans.Application;

public sealed class LoanDecisionEngine(IEnumerable<IDenialPolicy> denialPolicies)
{
    public LoanDecision Evaluate(LoanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reasons = denialPolicies
            .Where(policy => policy.IsViolatedBy(request))
            .Select(policy => policy.Reason)
            .ToList();

        return reasons.Count == 0 ? LoanDecision.Approved() : LoanDecision.Denied(reasons);
    }
}
