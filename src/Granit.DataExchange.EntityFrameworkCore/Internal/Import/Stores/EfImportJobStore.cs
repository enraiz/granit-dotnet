using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataExchange.EntityFrameworkCore.Internal.Import.Stores;

/// <summary>
/// EF Core implementation of <see cref="IImportJobStore"/>.
/// Performs CRUD operations on <see cref="ImportJob"/> via <see cref="DataExchangeDbContext"/>.
/// </summary>
internal sealed class EfImportJobStore(
    IDbContextFactory<DataExchangeDbContext> contextFactory) : IImportJobStore
{
    /// <inheritdoc/>
    public async Task<ImportJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(ImportJob job, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ImportJobs.Add(job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ImportJob job, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ImportJobs.Update(job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
