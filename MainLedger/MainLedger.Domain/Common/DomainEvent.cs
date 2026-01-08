namespace MainLedger.Domain.Common;

/// <summary>
/// Base class for all domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public abstract class DomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredAt { get; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }
}
