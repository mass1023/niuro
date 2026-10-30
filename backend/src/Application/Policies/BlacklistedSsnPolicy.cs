using Loans.Domain;
using Microsoft.Extensions.Options;

namespace Loans.Application.Policies;

public sealed class BlacklistedSsnPolicy(IOptions<SsnBlacklistOptions> options) : IDenialPolicy
{
    private readonly HashSet<string> _blacklistedSsns =
        options.Value.Ssns.Select(ssn => Ssn.Create(ssn).Value).ToHashSet();

    public string Reason => "The applicant is not eligible.";

    public bool IsViolatedBy(LoanRequest request) => _blacklistedSsns.Contains(request.Ssn.Value);
}
