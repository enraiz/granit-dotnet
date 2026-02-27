using Granit.Persistence.Migrations.Messages;

namespace Granit.Persistence.Migrations;

/// <summary>
/// Abstraction for dispatching migration batch execution.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation uses a <see cref="System.Threading.Channels.Channel{T}"/>
/// consumed by an internal <see cref="Microsoft.Extensions.Hosting.BackgroundService"/>.
/// </para>
/// <para>
/// Install <c>Granit.Persistence.Migrations.Wolverine</c> for Outbox-backed dispatch
/// via <c>IMessageBus</c>.
/// </para>
/// </remarks>
public interface IMigrationBatchDispatcher
{
    /// <summary>
    /// Dispatches a single migration batch command for execution.
    /// </summary>
    Task DispatchAsync(RunMigrationBatchCommand command, CancellationToken ct = default);

    /// <summary>
    /// Dispatches multiple migration batch commands for execution.
    /// </summary>
    Task DispatchAsync(IEnumerable<RunMigrationBatchCommand> commands, CancellationToken ct = default);
}
