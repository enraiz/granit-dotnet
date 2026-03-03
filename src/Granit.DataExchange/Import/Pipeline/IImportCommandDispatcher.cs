using Granit.DataExchange.Import.Messages;

namespace Granit.DataExchange.Import.Pipeline;

/// <summary>
/// Dispatches <see cref="ExecuteImportCommand"/> for background execution.
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
public interface IImportCommandDispatcher
{
    /// <summary>
    /// Dispatches an import execution command for background processing.
    /// </summary>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DispatchAsync(ExecuteImportCommand command, CancellationToken ct = default);
}
