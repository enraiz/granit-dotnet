using Granit.DataExchange.Export;
using Granit.Querying;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataExchange.EntityFrameworkCore.Internal.Export.Stores;

/// <summary>
/// EF Core implementation of <see cref="IExportJobStore"/>.
/// Performs CRUD operations on <see cref="ExportJob"/> via <see cref="DataExchangeDbContext"/>.
/// </summary>
internal sealed class EfExportJobStore(
    IDbContextFactory<DataExchangeDbContext> contextFactory) : IExportJobStore
{
    /// <inheritdoc/>
    public async Task<ExportJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ExportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ExportJob>> ListAsync(
        ExportJobStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<ExportJob> query = context.ExportJobs.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        int totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        List<ExportJob> items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<ExportJob>(items, totalCount);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(ExportJob job, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ExportJobs.Add(job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ExportJob job, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ExportJobs.Update(job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
