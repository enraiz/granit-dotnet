using Granit.DataImport.Export;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Stores;

/// <summary>
/// EF Core implementation of <see cref="IExportJobStore"/>.
/// Performs CRUD operations on <see cref="ExportJob"/> via <see cref="DataImportDbContext"/>.
/// </summary>
internal sealed class EfExportJobStore(
    IDbContextFactory<DataImportDbContext> contextFactory) : IExportJobStore
{
    /// <inheritdoc/>
    public async Task<ExportJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ExportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(ExportJob job, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ExportJobs.Add(job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ExportJob job, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.ExportJobs.Update(job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
