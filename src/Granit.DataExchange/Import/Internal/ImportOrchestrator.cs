using Granit.DataExchange.Import.Pipeline;
using Granit.DataExchange.Import.Reporting;

namespace Granit.DataExchange.Import.Internal;

/// <summary>
/// Default implementation of <see cref="IImportOrchestrator"/>.
/// Coordinates the full pipeline: Parse → [Group] → Map → Validate → [Resolve] → Execute.
/// </summary>
/// <remarks>
/// This is a placeholder implementation. The full pipeline orchestration requires
/// <c>Granit.DataExchange.EntityFrameworkCore</c> for the import job store and executor.
/// Concrete implementations are provided in downstream packages.
/// </remarks>
internal sealed class ImportOrchestrator : IImportOrchestrator
{
    /// <inheritdoc/>
    public Task<ImportReport> ExecuteAsync(Guid importJobId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "Import orchestration requires Granit.DataExchange.EntityFrameworkCore. " +
            "Register a concrete IImportOrchestrator implementation.");

    /// <inheritdoc/>
    public Task<ImportReport> DryRunAsync(Guid importJobId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "Import orchestration requires Granit.DataExchange.EntityFrameworkCore. " +
            "Register a concrete IImportOrchestrator implementation.");
}
