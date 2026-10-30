namespace Loans.Domain;

public sealed class LoanApplication
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal RequestedAmount { get; private set; }

    private LoanApplication() { }

    public static LoanApplication Create(Guid customerId, decimal requestedAmount)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));

        var application = new LoanApplication { Id = Guid.CreateVersion7(), CustomerId = customerId };
        application.UpdateRequestedAmount(requestedAmount);
        return application;
    }

    public void UpdateRequestedAmount(decimal requestedAmount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedAmount);

        RequestedAmount = requestedAmount;
    }
}
