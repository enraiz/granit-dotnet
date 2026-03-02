using Granit.DataImport.Domain;
using Granit.DataImport.Pipeline;

namespace Granit.DataImport.Internal;

/// <summary>
/// Default implementation of <see cref="IImportJobStore"/>.
/// Throws <see cref="NotImplementedException"/> — requires <c>Granit.DataImport.EntityFrameworkCore</c>.
/// </summary>
internal sealed class NullImportJobStore : IImportJobStore
{
    private const string Message =
        "Import job persistence requires Granit.DataImport.EntityFrameworkCore. " +
        "Call builder.AddGranitDataImportEntityFrameworkCore() to register a concrete IImportJobStore.";

    /// <inheritdoc/>
    public Task<ImportJob?> GetAsync(Guid id, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task CreateAsync(ImportJob job, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task UpdateAsync(ImportJob job, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);
}
