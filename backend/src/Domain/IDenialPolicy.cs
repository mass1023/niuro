namespace Loans.Domain;

public interface IDenialPolicy
{
    bool IsViolatedBy(LoanRequest request);
    string Reason { get; }
}
