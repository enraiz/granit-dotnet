using Granit.Persistence.Migrations.Messages;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Granit.Persistence.Migrations.Internal;

/// <summary>
/// Wolverine handler that executes one batch of the <see cref="MigrationPhase.Migrate"/> phase.
/// Cascades a new <see cref="RunMigrationBatchCommand"/> while rows remain to be processed,
/// or returns an empty array when migration is complete.
/// </summary>
/// <remarks>
/// <para>
/// Cascade pattern: returning <c>object[]</c> from a Wolverine handler emits zero or more
/// follow-up messages into the durable Outbox. An empty array stops the cascade.
/// </para>
/// <para>
/// <see cref="MigrationProgressDbContext"/> is committed independently from the tenant
/// <see cref="DbContext"/>: progress tracking is best-effort and must not block the migration.
/// </para>
/// </remarks>
internal sealed class RunMigrationBatchHandler(
    IMigrationCycleRegistry registry,
    IServiceProvider serviceProvider,
    MigrationProgressDbContext progressContext,
    ITenantDbIsolator isolator,
    IClock clock,
    ILogger<RunMigrationBatchHandler> logger)
{
    /// <summary>
    /// Processes one batch and cascades the next command, or returns empty when done.
    /// </summary>
    public async Task<object[]> HandleAsync(RunMigrationBatchCommand command, CancellationToken ct)
    {
        MigrationCycleRegistration? registration = registry.Find(command.CycleId);
        if (registration is null)
        {
            logger.LogWarning(
                "Migration cycle '{CycleId}' not found in registry. Message discarded.",
                command.CycleId);
            return [];
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
            return [];
        }

        MigrationBatchContext batchContext = new(command.Cursor, command.BatchSize, command.TenantId);
        MigrationBatchResult result;

        try
        {
            result = await registration.Migration(tenantContext, batchContext, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Migration batch failed for cycle '{CycleId}', tenant {TenantId}, cursor '{Cursor}'.",
                command.CycleId, tenantId, command.Cursor);

            progress.Status = MigrationStatus.Failed;
            progress.Error = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
            await SaveProgressAsync(progress, command.CycleId, tenantId, ct);

            throw; // Re-throw so Wolverine applies its retry / DLQ policy.
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

        await SaveProgressAsync(progress, command.CycleId, tenantId, ct);

        return result.NextCursor is null
            ? []
            : [new RunMigrationBatchCommand(command.CycleId, command.TenantId, result.NextCursor, command.BatchSize)];
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
        MigrationProgress progress,
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
