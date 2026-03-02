using System.Diagnostics;
using Granit.DataImport.Domain;
using Granit.DataImport.Execution;
using Granit.DataImport.Identity;
using Granit.DataImport.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Execution;

/// <summary>
/// EF Core implementation of <see cref="IImportExecutor{TEntity}"/>.
/// Persists validated entities in batches using the application DbContext.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TContext">The application DbContext type.</typeparam>
internal sealed class EfImportExecutor<TEntity, TContext>(
    IDbContextFactory<TContext> contextFactory) : IImportExecutor<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    /// <inheritdoc/>
    public async Task<ImportReport> ExecuteAsync(
        IAsyncEnumerable<ValidatedRow<TEntity>> entities,
        ImportExecutionOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        List<ImportRowError> errors = [];
        int totalRows = 0;
        int succeededRows = 0;
        int failedRows = 0;
        int insertedRows = 0;
        int updatedRows = 0;
        int batchCount = 0;

        await using TContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            await foreach (ValidatedRow<TEntity> row in entities.WithCancellation(ct))
            {
                totalRows++;

                try
                {
                    if (IsUpdateOperation(row))
                    {
                        context.Entry(row.Identity!.ExistingEntity!).CurrentValues.SetValues(row.Entity);
                        updatedRows++;
                    }
                    else
                    {
                        context.Set<TEntity>().Add(row.Entity);
                        insertedRows++;
                    }

                    succeededRows++;
                    batchCount++;

                    if (batchCount >= options.BatchSize)
                    {
                        await context.SaveChangesAsync(ct).ConfigureAwait(false);
                        batchCount = 0;

                        progress?.Report(new ImportProgress(
                            totalRows, 0, succeededRows, failedRows));
                    }
                }
                catch (DbUpdateException ex)
                {
                    failedRows++;
                    succeededRows--;
                    errors.Add(new ImportRowError(
                        row.RowNumber,
                        ImportRowErrorKind.Persistence,
                        ["Granit:DataImport:PersistenceError"],
                        ex.InnerException?.Message ?? ex.Message));

                    if (options.ErrorBehavior == ImportErrorBehavior.FailFast)
                    {
                        break;
                    }

                    // Detach the failed entity to continue processing
                    DetachFailedEntities(context);
                }
            }

            // Save remaining batch
            if (batchCount > 0)
            {
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            await CommitOrRollbackAsync(transaction, options.DryRun, ct).ConfigureAwait(false);
        }
        catch (Exception) when (options.ErrorBehavior != ImportErrorBehavior.FailFast)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
        }

        stopwatch.Stop();

        progress?.Report(new ImportProgress(totalRows, totalRows, succeededRows, failedRows));

        return new ImportReport
        {
            TotalRows = totalRows,
            SucceededRows = succeededRows,
            FailedRows = failedRows,
            SkippedRows = 0,
            InsertedRows = insertedRows,
            UpdatedRows = updatedRows,
            Duration = stopwatch.Elapsed,
            FinalStatus = DetermineFinalStatus(failedRows, succeededRows),
            RowErrors = errors.AsReadOnly(),
        };
    }

    private static bool IsUpdateOperation(ValidatedRow<TEntity> row) =>
        row.Identity?.Operation == RecordOperation.Update && row.Identity.ExistingEntity is not null;

    private static async Task CommitOrRollbackAsync(
        IDbContextTransaction transaction, bool dryRun, CancellationToken ct)
    {
        if (dryRun)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
        }
        else
        {
            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }
    }

    private static ImportJobStatus DetermineFinalStatus(int failedRows, int succeededRows)
    {
        if (failedRows == 0)
        {
            return ImportJobStatus.Completed;
        }

        return succeededRows > 0
            ? ImportJobStatus.PartiallyCompleted
            : ImportJobStatus.Failed;
    }

    private static void DetachFailedEntities(TContext context)
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in
            context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.State = EntityState.Detached;
        }
    }
}
