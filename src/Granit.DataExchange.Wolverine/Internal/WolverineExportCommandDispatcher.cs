using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Messages;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Granit.DataExchange.Wolverine.Internal;

/// <summary>
/// <see cref="IExportCommandDispatcher"/> implementation that dispatches export commands
/// via Wolverine's <see cref="IMessageBus"/> for Outbox-backed durable execution.
/// </summary>
/// <remarks>
/// Registered as a Singleton. Resolves <see cref="IMessageBus"/> from a DI scope per dispatch
/// because <c>IMessageBus</c> is scoped.
/// </remarks>
internal sealed class WolverineExportCommandDispatcher(
    IServiceScopeFactory scopeFactory) : IExportCommandDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(ExecuteExportCommand command, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.SendAsync(command).ConfigureAwait(false);
    }
}
