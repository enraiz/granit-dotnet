namespace Granit.Identity.Internal;

/// <summary>
/// No-op implementation of <see cref="IIdentityEventPublisher"/>.
/// Registered by default when no message bus integration is configured.
/// </summary>
internal sealed class NullIdentityEventPublisher : IIdentityEventPublisher
{
    /// <inheritdoc/>
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : notnull =>
        Task.CompletedTask;
}
