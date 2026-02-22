// =============================================================================
// SoftDeleteInterceptor - GDPR soft deletion
// =============================================================================
// Intercepts DELETE operations on ISoftDeletable entities and converts them
// to UPDATE SET IsDeleted = true.
//
// Soft-deleted entities are filtered by the global query filter
// configured in ConfigureModelExtensions.
//
// GDPR compliance: data is marked as deleted but retained
// for the HDS audit trail (3 years). Physical purge is handled separately.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigitalDynamics.Foundation.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that converts physical deletions
/// to soft deletions for <see cref="ISoftDeletable"/> entities.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;

    public SoftDeleteInterceptor(ICurrentUserService currentUserService, IClock clock)
    {
        _currentUserService = currentUserService;
        _clock = clock;
    }

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

        var now = _clock.Now;
        var userId = _currentUserService.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
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
