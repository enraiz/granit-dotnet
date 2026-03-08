using Granit.Core.Events;

namespace Granit.Core.Domain;

/// <summary>
/// Base class for aggregate roots — entities that form the root of a consistency boundary.
/// Carries a collection of domain events that are dispatched after <c>SaveChanges</c>.
/// </summary>
/// <remarks>
/// <para>
/// Aggregate roots encapsulate invariants: external code should use methods
/// (e.g., <c>Approve()</c>, <c>Cancel()</c>) rather than setting properties directly.
/// </para>
/// <para>
/// Domain events are collected via <see cref="AddDomainEvent"/> and automatically
/// dispatched by the <c>DomainEventDispatcherInterceptor</c> after the transaction commits.
/// </para>
/// </remarks>
public abstract class AggregateRoot : Entity, IDomainEventSource
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
