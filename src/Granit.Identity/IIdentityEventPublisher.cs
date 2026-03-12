namespace Granit.Identity;

/// <summary>
/// Publishes identity domain events after successful write operations on the identity provider.
/// </summary>
/// <remarks>
/// <para>
/// A <c>NullIdentityEventPublisher</c> (no-op) is registered by default.
/// To enable event-driven cache synchronization and audit trail, register an implementation
/// backed by a message bus (e.g. Wolverine <c>IMessageBus</c>).
/// </para>
/// <para>
/// Events are defined as records in <see cref="Granit.Identity.Events"/> and are published
/// by identity providers after each successful write operation.
/// </para>
/// </remarks>
public interface IIdentityEventPublisher
{
    /// <summary>
    /// Publishes an identity domain event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : notnull;
}
