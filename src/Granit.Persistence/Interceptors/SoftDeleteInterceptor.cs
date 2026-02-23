using Granit.Core.Domain;
using Granit.Security;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Granit.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that converts physical deletions
/// to soft deletions for <see cref="ISoftDeletable"/> entities.
/// </summary>
public sealed class SoftDeleteInterceptor(ICurrentUserService currentUserService, IClock clock) : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IClock _clock = clock;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset now = _clock.Now;
        string userId = _currentUserService.UserId ?? "system";

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ISoftDeletable> entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            // Convert DELETE -> UPDATE (soft delete)
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedBy = userId;
        }
    }
}
