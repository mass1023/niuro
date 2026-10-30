namespace Loans.Application.Abstractions;

/// <summary>
/// Records an event to be published after the unit of work commits.
/// Nothing is sent here; the event is persisted with the rest of the changes.
/// </summary>
public interface IEventOutbox
{
    void Enqueue<TEvent>(TEvent @event) where TEvent : notnull;
}
