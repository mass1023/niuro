using Loans.Application;
using Loans.Application.Policies;
using Microsoft.Extensions.Options;

namespace Loans.UnitTests;

public class BlacklistedSsnPolicyTests
{
    private readonly BlacklistedSsnPolicy _policy = new(Options.Create(new SsnBlacklistOptions
    {
        Ssns = ["123-45-6789", "111223333"]
    }));

    [Theory]
    [InlineData("123-45-6789")]
    [InlineData("123456789")]
    [InlineData("111-22-3333")]
    public void IsViolatedBy_BlacklistedSsn_ReturnsTrue(string ssn)
    {
        Assert.True(_policy.IsViolatedBy(LoanRequests.Create(ssn: ssn)));
    }

    [Fact]
    public void IsViolatedBy_CleanSsn_ReturnsFalse()
    {
        Assert.False(_policy.IsViolatedBy(LoanRequests.Create(ssn: "222-33-4444")));
    }

    [Fact]
    public void IsViolatedBy_EmptyBlacklist_ReturnsFalse()
    {
        var policy = new BlacklistedSsnPolicy(Options.Create(new SsnBlacklistOptions()));

        Assert.False(policy.IsViolatedBy(LoanRequests.Create(ssn: "123-45-6789")));
    }

    [Fact]
    public void Reason_DoesNotDiscloseBlacklist()
    {
        Assert.DoesNotContain("blacklist", _policy.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
