namespace Loans.Domain;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public string CompanyName { get; private set; } = null!;
    public Ssn Ssn { get; private set; } = null!;

    private Customer() { }

    public static Customer Create(string firstName, string lastName, Address address, string companyName, Ssn ssn)
    {
        ArgumentNullException.ThrowIfNull(ssn);

        var customer = new Customer { Id = Guid.CreateVersion7(), Ssn = ssn };
        customer.UpdateDetails(firstName, lastName, address, companyName);
        return customer;
    }

    public void UpdateDetails(string firstName, string lastName, Address address, string companyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Address = address;
        CompanyName = companyName.Trim();
    }
}
