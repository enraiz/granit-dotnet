namespace Granit.DataExchange.Export.Internal;

/// <summary>
/// Null-object implementation of <see cref="IExportJobStore"/>.
/// Stores jobs in memory (non-durable). Replaced by EF Core implementation when
/// <c>Granit.DataExchange.EntityFrameworkCore</c> is installed.
/// </summary>
internal sealed class NullExportJobStore : IExportJobStore
{
    public Task<ExportJob?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<ExportJob?>(null);

    public Task CreateAsync(ExportJob job, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(ExportJob job, CancellationToken ct = default) =>
        Task.CompletedTask;
}
