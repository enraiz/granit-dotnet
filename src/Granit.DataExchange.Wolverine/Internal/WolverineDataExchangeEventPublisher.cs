using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Messages;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Granit.DataExchange.Wolverine.Internal;

/// <summary>
/// <see cref="IDataExchangeEventPublisher"/> implementation that publishes lifecycle events
/// via Wolverine's <see cref="IMessageBus"/> for Outbox-backed durable delivery.
/// </summary>
/// <remarks>
/// Registered as a Singleton. Resolves <see cref="IMessageBus"/> from a DI scope per publish
/// because <c>IMessageBus</c> is scoped.
/// </remarks>
internal sealed class WolverineDataExchangeEventPublisher(
    IServiceScopeFactory scopeFactory) : IDataExchangeEventPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync(ImportJobCompletedEvent notification, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(notification).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task PublishAsync(ExportJobCompletedEvent notification, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(notification).ConfigureAwait(false);
    }
}
