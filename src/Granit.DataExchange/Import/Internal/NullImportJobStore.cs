using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Pipeline;

namespace Granit.DataExchange.Import.Internal;

/// <summary>
/// Default implementation of <see cref="IImportJobStore"/>.
/// Throws <see cref="NotImplementedException"/> — requires <c>Granit.DataExchange.EntityFrameworkCore</c>.
/// </summary>
internal sealed class NullImportJobStore : IImportJobStore
{
    private const string Message =
        "Import job persistence requires Granit.DataExchange.EntityFrameworkCore. " +
        "Call builder.AddGranitDataExchangeEntityFrameworkCore() to register a concrete IImportJobStore.";

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
