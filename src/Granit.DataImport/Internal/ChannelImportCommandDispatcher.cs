using System.Threading.Channels;
using Granit.DataImport.Messages;
using Granit.DataImport.Pipeline;

namespace Granit.DataImport.Internal;

/// <summary>
/// Default <see cref="IImportCommandDispatcher"/> implementation that writes commands
/// to an unbounded <see cref="Channel{T}"/> consumed by <see cref="ImportCommandWorker"/>.
/// </summary>
/// <remarks>
/// Registered as a Singleton. The channel is shared with <see cref="ImportCommandWorker"/>
/// via constructor injection.
/// </remarks>
internal sealed class ChannelImportCommandDispatcher(
    Channel<ExecuteImportCommand> channel) : IImportCommandDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(ExecuteImportCommand command, CancellationToken ct = default) =>
        await channel.Writer.WriteAsync(command, ct).ConfigureAwait(false);
}
