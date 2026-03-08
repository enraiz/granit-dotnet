using System.Threading.Channels;
using Granit.DataExchange.Export.Messages;

namespace Granit.DataExchange.Export.Internal;

/// <summary>
/// Default <see cref="IExportCommandDispatcher"/> implementation that writes commands
/// to an unbounded <see cref="Channel{T}"/> consumed by <see cref="ExportCommandWorker"/>.
/// </summary>
/// <remarks>
/// Registered as a Singleton. The channel is shared with <see cref="ExportCommandWorker"/>
/// via constructor injection.
/// </remarks>
internal sealed class ChannelExportCommandDispatcher(
    Channel<ExecuteExportCommand> channel) : IExportCommandDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(ExecuteExportCommand command, CancellationToken cancellationToken = default) =>
        await channel.Writer.WriteAsync(command, cancellationToken).ConfigureAwait(false);
}
