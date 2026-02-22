// =============================================================================
// AuditableEntityInterceptor - Automatic population of HDS audit fields
// =============================================================================
// Intercepts EF Core SaveChanges to automatically populate
// CreatedAt/CreatedBy and ModifiedAt/ModifiedBy on every AuditableEntity.
//
// HDS compliance: every modification is tracked with the user and timestamp.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.Guids;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigitalDynamics.Foundation.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically populates audit fields
/// on <see cref="AuditableEntity"/> entities.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;
    private readonly IGuidGenerator _guidGenerator;

    public AuditableEntityInterceptor(
        ICurrentUserService currentUserService,
        IClock clock,
        IGuidGenerator guidGenerator)
    {
        _currentUserService = currentUserService;
        _clock = clock;
        _guidGenerator = guidGenerator;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _clock.Now;
        var userId = _currentUserService.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = _guidGenerator.Create();
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = userId;
                    // Prevent modification of creation fields
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    break;
            }
        }
    }
}
