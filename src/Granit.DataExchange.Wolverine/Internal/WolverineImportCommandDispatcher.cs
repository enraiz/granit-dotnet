using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Import.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Granit.DataExchange.Wolverine.Internal;

/// <summary>
/// <see cref="IImportCommandDispatcher"/> implementation that dispatches import commands
/// via Wolverine's <see cref="IMessageBus"/> for Outbox-backed durable execution.
/// </summary>
/// <remarks>
/// Registered as a Singleton. Resolves <see cref="IMessageBus"/> from a DI scope per dispatch
/// because <c>IMessageBus</c> is scoped.
/// </remarks>
internal sealed class WolverineImportCommandDispatcher(
    IServiceScopeFactory scopeFactory) : IImportCommandDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(ExecuteImportCommand command, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.SendAsync(command).ConfigureAwait(false);
    }
}
