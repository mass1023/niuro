using Loans.Domain;

namespace Loans.UnitTests;

public class SsnTests
{
    [Theory]
    [InlineData("123-45-6789")]
    [InlineData("123456789")]
    [InlineData(" 123 45 6789 ")]
    public void Create_NormalizesToNineDigits(string raw)
    {
        var ssn = Ssn.Create(raw);

        Assert.Equal("123456789", ssn.Value);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567890")]
    [InlineData("abc-de-fghi")]
    [InlineData("12a-45-6789")]
    [InlineData("")]
    public void Create_RejectsInvalidInput(string raw)
    {
        Assert.ThrowsAny<ArgumentException>(() => Ssn.Create(raw));
    }

    [Fact]
    public void SameDigits_AreEqual()
    {
        Assert.Equal(Ssn.Create("123-45-6789"), Ssn.Create("123456789"));
    }

    [Fact]
    public void ToString_IsMasked()
    {
        var ssn = Ssn.Create("123-45-6789");

        Assert.Equal("***-**-6789", ssn.ToString());
    }

    [Fact]
    public void LoanRequest_ToString_DoesNotLeakSsn()
    {
        var request = LoanRequests.Create(ssn: "123-45-6789");

        Assert.DoesNotContain("123456789", request.ToString());
        Assert.DoesNotContain("123-45", request.ToString());
    }
}
