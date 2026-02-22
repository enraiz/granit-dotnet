// =============================================================================
// AuditedEntityInterceptor - Automatic population of HDS audit fields
// =============================================================================
// Intercepts EF Core SaveChanges to automatically populate:
//   - CreatedAt/CreatedBy on every CreationAuditedEntity (add)
//   - ModifiedAt/ModifiedBy on every AuditedEntity (modify)
//   - TenantId on every IMultiTenant entity without a defined tenant (add)
//
// HDS compliance: every modification is tracked with the user and timestamp.
// Multi-tenant isolation is guaranteed at the persistence layer.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.Guids;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigitalDynamics.Foundation.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically populates audit fields
/// on entities inheriting from <see cref="CreationAuditedEntity"/>,
/// and the <see cref="IMultiTenant.TenantId"/> on multi-tenant entities.
/// </summary>
public sealed class AuditedEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;

    public AuditedEntityInterceptor(
        ICurrentUserService currentUserService,
        IClock clock,
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant)
    {
        _currentUserService = currentUserService;
        _clock = clock;
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
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

        DateTimeOffset now = _clock.Now;
        string userId = _currentUserService.UserId ?? "system";

        foreach (EntityEntry<CreationAuditedEntity> entry in context.ChangeTracker.Entries<CreationAuditedEntity>())
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

                    // Multi-tenant isolation: inject current TenantId if the entity supports it
                    if (entry.Entity is IMultiTenant multiTenant && multiTenant.TenantId is null)
                    {
                        multiTenant.TenantId = _currentTenant.Id;
                    }

                    break;

                case EntityState.Modified:
                    // Protect creation fields from modification
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;

                    // Populate modification fields only on AuditedEntity
                    if (entry.Entity is AuditedEntity audited)
                    {
                        audited.ModifiedAt = now;
                        audited.ModifiedBy = userId;
                    }

                    break;
            }
        }
    }
}
