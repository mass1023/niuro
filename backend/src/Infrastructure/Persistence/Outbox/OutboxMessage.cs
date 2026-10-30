namespace Loans.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string payload) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Payload = payload,
            OccurredAtUtc = DateTime.UtcNow
        };

    public void MarkProcessed()
    {
        Attempts++;
        ProcessedAtUtc = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        Attempts++;
        LastError = error;
    }
}
