namespace Loans.Domain;

public sealed record Ssn
{
    public string Value { get; }

    private Ssn(string value) => Value = value;

    public static Ssn Create(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);

        var digits = raw.Trim().Replace("-", "").Replace(" ", "");

        if (digits.Length != 9 || !digits.All(char.IsAsciiDigit))
            throw new ArgumentException("SSN must contain exactly 9 digits.", nameof(raw));

        return new Ssn(digits);
    }

    public override string ToString() => $"***-**-{Value[^4..]}";
}
