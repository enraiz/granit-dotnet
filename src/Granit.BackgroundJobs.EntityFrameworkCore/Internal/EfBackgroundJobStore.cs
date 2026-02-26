using Granit.BackgroundJobs.Internal;
using Microsoft.EntityFrameworkCore;

namespace Granit.BackgroundJobs.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IBackgroundJobStore"/>.
/// </summary>
/// <remarks>
/// Registered as a <b>Singleton</b> when <see cref="JobStoreMode.Durable"/> is configured.
/// Each operation creates and disposes its own <see cref="BackgroundJobsDbContext"/> via
/// <see cref="IDbContextFactory{TContext}"/>, making it safe for background services and
/// <see cref="Microsoft.Extensions.Hosting.IHostedService"/> consumers.
/// </remarks>
internal sealed class EfBackgroundJobStore(
    IDbContextFactory<BackgroundJobsDbContext> contextFactory) : IBackgroundJobStore
{
    /// <inheritdoc/>
    public async Task<BackgroundJobDefinition?> FindAsync(string jobName, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Jobs.FirstOrDefaultAsync(j => j.JobName == jobName, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BackgroundJobDefinition>> GetEnabledJobsAsync(CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Jobs.Where(j => j.IsEnabled).ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BackgroundJobDefinition>> GetAllJobsAsync(CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Jobs.ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task SeedJobsAsync(IEnumerable<RecurringJobRegistration> registrations, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);

        foreach (RecurringJobRegistration reg in registrations)
        {
            BackgroundJobDefinition? existing =
                await context.Jobs.FirstOrDefaultAsync(j => j.JobName == reg.JobName, ct);

            if (existing is null)
            {
                context.Jobs.Add(new BackgroundJobDefinition
                {
                    Id = Guid.NewGuid(),
                    JobName = reg.JobName,
                    CronExpression = reg.CronExpression,
                    MessageType = reg.MessageType,
                    IsEnabled = true,
                });
            }
            else
            {
                // Preserve administrative state — only sync scheduling metadata.
                existing.CronExpression = reg.CronExpression;
                existing.MessageType = reg.MessageType;
            }
        }

        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task RecordExecutionStartAsync(string jobName, DateTimeOffset startedAt, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        BackgroundJobDefinition? job = await context.Jobs.FirstOrDefaultAsync(j => j.JobName == jobName, ct);
        if (job is null)
        {
            return;
        }

        job.LastExecutedAt = startedAt;
        job.LastErrorMessage = null;
        job.ConsecutiveFailureCount = 0;
        job.TriggeredBy = null;
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task RecordNextExecutionAsync(string jobName, DateTimeOffset nextExecution, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        BackgroundJobDefinition? job = await context.Jobs.FirstOrDefaultAsync(j => j.JobName == jobName, ct);
        if (job is null)
        {
            return;
        }

        job.NextExecutionAt = nextExecution;
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task RecordExecutionFailureAsync(string jobName, string errorMessage, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        BackgroundJobDefinition? job = await context.Jobs.FirstOrDefaultAsync(j => j.JobName == jobName, ct);
        if (job is null)
        {
            return;
        }

        job.ConsecutiveFailureCount++;
        job.LastErrorMessage = errorMessage;
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task SetEnabledAsync(string jobName, bool enabled, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        BackgroundJobDefinition? job = await context.Jobs.FirstOrDefaultAsync(j => j.JobName == jobName, ct);
        if (job is null)
        {
            return;
        }

        job.IsEnabled = enabled;
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task SetTriggeredByAsync(string jobName, string? triggeredBy, CancellationToken ct = default)
    {
        await using BackgroundJobsDbContext context = await contextFactory.CreateDbContextAsync(ct);
        BackgroundJobDefinition? job = await context.Jobs.FirstOrDefaultAsync(j => j.JobName == jobName, ct);
        if (job is null)
        {
            return;
        }

        job.TriggeredBy = triggeredBy;
        await context.SaveChangesAsync(ct);
    }
}
