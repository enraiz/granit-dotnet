using Granit.Persistence.Migrations.Messages;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Granit.Persistence.Migrations.Internal;

/// <summary>
/// Executes a single migration batch and returns the next command (cascade), or <c>null</c> when complete.
/// </summary>
/// <remarks>
/// Extracted from the Wolverine handler to be transport-agnostic. This class contains no
/// dependency on Wolverine — it can be consumed by a <see cref="MigrationBatchWorker"/>
/// (Channel-based) or by a Wolverine handler in <c>Granit.Persistence.Migrations.Wolverine</c>.
/// </remarks>
internal sealed class MigrationBatchExecutor(
    IMigrationCycleRegistry registry,
    IServiceProvider serviceProvider,
    MigrationProgressDbContext progressContext,
    ITenantDbIsolator isolator,
    IClock clock,
    ILogger<MigrationBatchExecutor> logger)
{
    /// <summary>
    /// Processes one batch and returns the next command, or <c>null</c> when the cycle is complete.
    /// </summary>
    public async Task<RunMigrationBatchCommand?> ExecuteBatchAsync(
        RunMigrationBatchCommand command,
        CancellationToken ct)
    {
        MigrationCycleRegistration? registration = registry.Find(command.CycleId);
        if (registration is null)
        {
            logger.LogWarning(
                "Migration cycle '{CycleId}' not found in registry. Message discarded.",
                command.CycleId);
            return null;
        }

        DbContext tenantContext =
            (DbContext)serviceProvider.GetRequiredService(registration.DbContextType);

        Guid? tenantId = command.TenantId == Guid.Empty ? null : command.TenantId;

        if (tenantId.HasValue)
        {
            await isolator.IsolateAsync(tenantContext, tenantId.Value, ct);
        }

        MigrationProgress progress = await FindOrCreateProgressAsync(command, tenantId, ct);

        if (progress.Status == MigrationStatus.Completed)
        {
            logger.LogInformation(
                "Migration cycle '{CycleId}' already completed for tenant {TenantId}. Message discarded.",
                command.CycleId, tenantId);
            return null;
        }

        MigrationBatchContext batchContext = new(command.Cursor, command.BatchSize, command.TenantId);
        MigrationBatchResult result;

        try
        {
            result = await registration.Migration(tenantContext, batchContext, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            progress.Status = MigrationStatus.Failed;
            progress.Error = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
            await SaveProgressAsync(command.CycleId, tenantId, ct);

            throw;
        }

        progress.ProcessedRows += result.ProcessedCount;
        progress.LastCursor = result.NextCursor;

        if (result.NextCursor is null)
        {
            progress.Status = MigrationStatus.Completed;
            progress.CompletedAt = clock.Now;
            logger.LogInformation(
                "Migration cycle '{CycleId}' completed for tenant {TenantId}. Total rows migrated: {ProcessedRows}.",
                command.CycleId, tenantId, progress.ProcessedRows);
        }
        else
        {
            logger.LogInformation(
                "Migration batch processed for cycle '{CycleId}', tenant {TenantId}. "
                    + "Rows this batch: {Count}. Next cursor: '{Cursor}'.",
                command.CycleId, tenantId, result.ProcessedCount, result.NextCursor);
        }

        await SaveProgressAsync(command.CycleId, tenantId, ct);

        return result.NextCursor is null
            ? null
            : new RunMigrationBatchCommand(command.CycleId, command.TenantId, result.NextCursor, command.BatchSize);
    }

    private async Task<MigrationProgress> FindOrCreateProgressAsync(
        RunMigrationBatchCommand command,
        Guid? tenantId,
        CancellationToken ct)
    {
        MigrationProgress? existing = await progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == command.CycleId && p.TenantId == tenantId, ct);

        if (existing is not null)
        {
            return existing;
        }

        MigrationProgress created = new()
        {
            Id = Guid.NewGuid(),
            CycleId = command.CycleId,
            Phase = MigrationPhase.Migrate,
            Status = MigrationStatus.InProgress,
            TenantId = tenantId,
            StartedAt = clock.Now,
        };

        progressContext.MigrationProgresses.Add(created);
        return created;
    }

    private async Task SaveProgressAsync(
        string cycleId,
        Guid? tenantId,
        CancellationToken ct)
    {
        try
        {
            await progressContext.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Best-effort: progress tracking must not fail the migration batch.
            logger.LogWarning(
                ex,
                "Failed to persist migration progress for cycle '{CycleId}', tenant {TenantId}. "
                    + "Progress tracking may be stale.",
                cycleId, tenantId);
        }
    }
}
