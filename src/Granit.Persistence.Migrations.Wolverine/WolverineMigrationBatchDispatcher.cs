using Granit.Persistence.Migrations.Abstractions;
using Granit.Persistence.Migrations.Messages;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Granit.Persistence.Migrations.Wolverine;

/// <summary>
/// <see cref="IMigrationBatchDispatcher"/> implementation that dispatches batch commands
/// via Wolverine's <see cref="IMessageBus"/> for Outbox-backed durable execution.
/// </summary>
/// <remarks>
/// Registered as a Singleton. Resolves <see cref="IMessageBus"/> from a DI scope per dispatch
/// because <c>IMessageBus</c> is scoped.
/// </remarks>
internal sealed class WolverineMigrationBatchDispatcher(
    IServiceScopeFactory scopeFactory) : IMigrationBatchDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(RunMigrationBatchCommand command, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.SendAsync(command).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(IEnumerable<RunMigrationBatchCommand> commands, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        foreach (RunMigrationBatchCommand command in commands)
        {
            await bus.SendAsync(command).ConfigureAwait(false);
        }
    }
}
