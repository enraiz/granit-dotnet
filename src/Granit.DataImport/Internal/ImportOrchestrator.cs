using Granit.DataImport.Pipeline;
using Granit.DataImport.Reporting;

namespace Granit.DataImport.Internal;

/// <summary>
/// Default implementation of <see cref="IImportOrchestrator"/>.
/// Coordinates the full pipeline: Parse → [Group] → Map → Validate → [Resolve] → Execute.
/// </summary>
/// <remarks>
/// This is a placeholder implementation. The full pipeline orchestration requires
/// <c>Granit.DataImport.EntityFrameworkCore</c> for the import job store and executor.
/// Concrete implementations are provided in downstream packages.
/// </remarks>
internal sealed class ImportOrchestrator : IImportOrchestrator
{
    /// <inheritdoc/>
    public Task<ImportReport> ExecuteAsync(Guid importJobId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "Import orchestration requires Granit.DataImport.EntityFrameworkCore. " +
            "Register a concrete IImportOrchestrator implementation.");

    /// <inheritdoc/>
    public Task<ImportReport> DryRunAsync(Guid importJobId, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "Import orchestration requires Granit.DataImport.EntityFrameworkCore. " +
            "Register a concrete IImportOrchestrator implementation.");
}
