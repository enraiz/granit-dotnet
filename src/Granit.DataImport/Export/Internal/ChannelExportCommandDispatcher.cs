using System.Threading.Channels;
using Granit.DataImport.Export.Messages;

namespace Granit.DataImport.Export.Internal;

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
    public async Task DispatchAsync(ExecuteExportCommand command, CancellationToken ct = default) =>
        await channel.Writer.WriteAsync(command, ct).ConfigureAwait(false);
}
