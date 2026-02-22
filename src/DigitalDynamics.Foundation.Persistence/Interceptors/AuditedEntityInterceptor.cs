// =============================================================================
// AuditedEntityInterceptor - Remplissage automatique des champs d'audit HDS
// =============================================================================
// Intercepte les SaveChanges d'EF Core pour remplir automatiquement :
//   - CreatedAt/CreatedBy sur toute CreationAuditedEntity (ajout)
//   - ModifiedAt/ModifiedBy sur toute AuditedEntity (modification)
//   - TenantId sur toute entité IMultiTenant sans tenant défini (ajout)
//
// Conformité HDS : chaque modification est tracée avec l'utilisateur
// et l'horodatage. L'isolation multi-tenant est garantie à la couche persistance.
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
/// Intercepteur EF Core qui remplit automatiquement les champs d'audit
/// sur les entités héritant de <see cref="CreationAuditedEntity"/>,
/// et le <see cref="IMultiTenant.TenantId"/> sur les entités multi-tenant.
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

                    // Isolation multi-tenant : injecter le TenantId courant si l'entité le supporte
                    if (entry.Entity is IMultiTenant multiTenant && multiTenant.TenantId is null)
                    {
                        multiTenant.TenantId = _currentTenant.Id;
                    }

                    break;

                case EntityState.Modified:
                    // Protéger les champs de création
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;

                    // Remplir les champs de modification uniquement sur AuditedEntity
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
