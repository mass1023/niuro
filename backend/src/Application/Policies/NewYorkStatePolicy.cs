using Loans.Domain;

namespace Loans.Application.Policies;

public sealed class NewYorkStatePolicy : IDenialPolicy
{
    public string Reason => "Applications from New York are not accepted.";

    public bool IsViolatedBy(LoanRequest request)
    {
        return request.Address.State == "NY";
    }


}