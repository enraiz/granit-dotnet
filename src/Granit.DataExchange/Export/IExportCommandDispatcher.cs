using Granit.DataExchange.Export.Messages;

namespace Granit.DataExchange.Export;

/// <summary>
/// Dispatches <see cref="ExecuteExportCommand"/> for background execution.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation uses a <see cref="System.Threading.Channels.Channel{T}"/>
/// consumed by an internal <see cref="Microsoft.Extensions.Hosting.BackgroundService"/>.
/// </para>
/// <para>
/// Applications using Wolverine should register a Wolverine-backed implementation
/// that publishes via <c>IMessageBus.SendAsync()</c> for Outbox-backed durable dispatch.
/// </para>
/// </remarks>
public interface IExportCommandDispatcher
{
    /// <summary>
    /// Dispatches an export execution command for background processing.
    /// </summary>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(ExecuteExportCommand command, CancellationToken cancellationToken = default);
}
