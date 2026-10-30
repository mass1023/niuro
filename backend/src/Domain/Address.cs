namespace Loans.Domain;

public sealed record Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    private Address(string street, string city, string state, string zipCode) =>
        (Street, City, State, ZipCode) = (street, city, state, zipCode);

    public static Address Create(string street, string city, string state, string zipCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipCode);

        street = street.Trim();
        city = city.Trim();
        state = state.Trim().ToUpperInvariant();
        zipCode = zipCode.Trim();

        if (state.Length != 2 || !state.All(char.IsAsciiLetter))
            throw new ArgumentException("State must be a two-letter abbreviation.", nameof(state));

        if (zipCode.Length != 5 || !zipCode.All(char.IsAsciiDigit))
            throw new ArgumentException("Zip code must contain exactly 5 digits.", nameof(zipCode));

        return new Address(street, city, state, zipCode);
    }
}
