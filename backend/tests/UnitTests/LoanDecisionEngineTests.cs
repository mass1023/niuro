using Loans.Application;
using Loans.Application.Policies;
using Loans.Domain;
using Microsoft.Extensions.Options;

namespace Loans.UnitTests;

public class LoanDecisionEngineTests
{
    private static readonly NewYorkStatePolicy NewYork = new();

    private static readonly BlacklistedSsnPolicy Blacklist = new(Options.Create(new SsnBlacklistOptions
    {
        Ssns = ["123-45-6789"]
    }));

    private readonly LoanDecisionEngine _engine = new([NewYork, Blacklist]);

    [Fact]
    public void Evaluate_NoPolicyViolated_IsApproved()
    {
        var decision = _engine.Evaluate(LoanRequests.Create());

        Assert.True(decision.IsApproved);
        Assert.Empty(decision.DenialReasons);
    }

    [Fact]
    public void Evaluate_NewYork_IsDeniedWithNewYorkReason()
    {
        var decision = _engine.Evaluate(LoanRequests.Create(state: "NY"));

        Assert.False(decision.IsApproved);
        Assert.Equal([NewYork.Reason], decision.DenialReasons);
    }

    [Fact]
    public void Evaluate_BlacklistedSsn_IsDeniedWithBlacklistReason()
    {
        var decision = _engine.Evaluate(LoanRequests.Create(ssn: "123456789"));

        Assert.False(decision.IsApproved);
        Assert.Equal([Blacklist.Reason], decision.DenialReasons);
    }

    [Fact]
    public void Evaluate_SeveralPoliciesViolated_CollectsAllReasons()
    {
        var decision = _engine.Evaluate(LoanRequests.Create(state: "NY", ssn: "123-45-6789"));

        Assert.False(decision.IsApproved);
        Assert.Equal([NewYork.Reason, Blacklist.Reason], decision.DenialReasons);
    }

    [Fact]
    public void Evaluate_NewPolicy_IsAppliedWithoutChangingEngineOrExistingPolicies()
    {
        var engine = new LoanDecisionEngine([NewYork, Blacklist, new AlwaysDenyPolicy()]);

        var decision = engine.Evaluate(LoanRequests.Create());

        Assert.False(decision.IsApproved);
        Assert.Equal([AlwaysDenyPolicy.DenialReason], decision.DenialReasons);
    }

    [Fact]
    public void Evaluate_NoPoliciesRegistered_IsApproved()
    {
        var decision = new LoanDecisionEngine([]).Evaluate(LoanRequests.Create(state: "NY"));

        Assert.True(decision.IsApproved);
    }

    private sealed class AlwaysDenyPolicy : IDenialPolicy
    {
        public const string DenialReason = "Always denied.";

        public string Reason => DenialReason;

        public bool IsViolatedBy(LoanRequest request) => true;
    }
}
