using Granit.DataImport.Domain;
using Granit.DataImport.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Stores;

/// <summary>
/// EF Core implementation of <see cref="IImportJobStore"/>.
/// Performs CRUD operations on <see cref="ImportJob"/> via <see cref="DataImportDbContext"/>.
/// </summary>
internal sealed class EfImportJobStore(
    IDbContextFactory<DataImportDbContext> contextFactory) : IImportJobStore
{
    /// <inheritdoc/>
    public async Task<ImportJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(ImportJob job, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct);
        context.ImportJobs.Add(job);
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ImportJob job, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct);
        context.ImportJobs.Update(job);
        await context.SaveChangesAsync(ct);
    }
}
