using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Messages;

namespace Granit.DataExchange.Wolverine;

/// <summary>
/// Wolverine handler that executes an export job via <see cref="IExportOrchestrator"/>.
/// </summary>
/// <remarks>
/// <para>
/// This handler is a thin adapter between Wolverine's message handling pipeline and the
/// transport-agnostic <see cref="IExportOrchestrator"/>. The orchestrator resolves scoped
/// services (DbContext, stores, etc.) internally.
/// </para>
/// <para>
/// Unlike migration batch handlers, export jobs do not cascade — each command is a
/// self-contained unit of work.
/// </para>
/// </remarks>
internal sealed class ExecuteExportCommandHandler(IExportOrchestrator orchestrator)
{
    /// <summary>
    /// Handles the export command by delegating to the orchestrator.
    /// </summary>
    public async Task HandleAsync(ExecuteExportCommand command, CancellationToken ct) =>
        await orchestrator.ExecuteAsync(command.ExportJobId, ct).ConfigureAwait(false);
}
