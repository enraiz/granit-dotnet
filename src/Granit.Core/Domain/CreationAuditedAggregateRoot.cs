using Granit.Core.Events;

namespace Granit.Core.Domain;

/// <summary>
/// Aggregate root with creation-only audit trail (CreatedAt, CreatedBy).
/// </summary>
public abstract class CreationAuditedAggregateRoot : CreationAuditedEntity, IDomainEventSource
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be dispatched after the current transaction commits.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
