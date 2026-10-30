using Loans.Application.Policies;

namespace Loans.UnitTests;

public class NewYorkStatePolicyTests
{
    private readonly NewYorkStatePolicy _policy = new();

    [Theory]
    [InlineData("NY")]
    [InlineData("ny")]
    [InlineData(" Ny ")]
    public void IsViolatedBy_NewYork_ReturnsTrue(string state)
    {
        Assert.True(_policy.IsViolatedBy(LoanRequests.Create(state: state)));
    }

    [Theory]
    [InlineData("CA")]
    [InlineData("NJ")]
    public void IsViolatedBy_OtherState_ReturnsFalse(string state)
    {
        Assert.False(_policy.IsViolatedBy(LoanRequests.Create(state: state)));
    }
}
