namespace Loans.Application;

public sealed record LoanDecision
{
    public bool IsApproved { get; }
    public IReadOnlyList<string> DenialReasons { get; }

    private LoanDecision(bool isApproved, IEnumerable<string> denialReasons) =>
        (IsApproved, DenialReasons) = (isApproved, denialReasons.ToList());

    public static LoanDecision Approved() => new(true, []);

    public static LoanDecision Denied(params IEnumerable<string> reasons)
    {
        var reasonList = reasons.ToList();
        if (reasonList.Count == 0)
            throw new ArgumentException("A denial needs at least one reason.", nameof(reasons));

        return new(false, reasonList);
    }
}
