using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Import.Pipeline;

namespace Granit.DataExchange.Wolverine;

/// <summary>
/// Wolverine handler that executes an import job via <see cref="IImportOrchestrator"/>.
/// </summary>
/// <remarks>
/// <para>
/// This handler is a thin adapter between Wolverine's message handling pipeline and the
/// transport-agnostic <see cref="IImportOrchestrator"/>. The orchestrator resolves scoped
/// services (DbContext, stores, etc.) internally.
/// </para>
/// <para>
/// Unlike migration batch handlers, import jobs do not cascade — each command is a
/// self-contained unit of work.
/// </para>
/// </remarks>
internal sealed class ExecuteImportCommandHandler(IImportOrchestrator orchestrator)
{
    /// <summary>
    /// Handles the import command by delegating to the orchestrator.
    /// </summary>
    public async Task HandleAsync(ExecuteImportCommand command, CancellationToken cancellationToken) =>
        await orchestrator.ExecuteAsync(command.ImportJobId, cancellationToken).ConfigureAwait(false);
}
