// =============================================================================
// ModelBuilderExtensions - Configuration EF Core pour les entités Foundation
// =============================================================================
// Applique les query filters globaux (soft delete, multi-tenant) sur le
// ModelBuilder. À appeler dans OnModelCreating du DbContext de l'application.
//
// Usage dans un DbContext de module :
//   protected override void OnModelCreating(ModelBuilder modelBuilder)
//   {
//       modelBuilder.HasDefaultSchema("auth");
//       modelBuilder.ApplyFoundationConventions(_currentTenant);
//       modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
//   }
// =============================================================================

using System.Linq.Expressions;
using System.Reflection;
using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DigitalDynamics.Foundation.Persistence.Extensions;

/// <summary>
/// Extensions pour configurer les conventions Foundation sur le ModelBuilder EF Core.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applique les conventions Foundation :
    /// - Query filter global <see cref="ISoftDeletable"/> (WHERE IsDeleted = false)
    /// - Query filter global <see cref="IMultiTenant"/> (WHERE TenantId = currentTenant.Id),
    ///   uniquement si <paramref name="currentTenant"/> est fourni.
    /// </summary>
    /// <param name="modelBuilder">Le ModelBuilder EF Core.</param>
    /// <param name="currentTenant">
    /// Service du tenant courant. Si <c>null</c>, le filtre multi-tenant n'est pas appliqué.
    /// Passer l'instance injectée dans le constructeur du DbContext pour une évaluation
    /// lazy correcte (re-évaluée à chaque requête via AsyncLocal).
    /// </param>
    public static ModelBuilder ApplyFoundationConventions(
        this ModelBuilder modelBuilder,
        ICurrentTenant? currentTenant = null)
    {
        ApplySoftDeleteQueryFilters(modelBuilder);

        if (currentTenant is not null)
        {
            ApplyMultiTenantQueryFilters(modelBuilder, currentTenant);
        }

        return modelBuilder;
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)))
        {
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            BinaryExpression condition = Expression.Equal(property, Expression.Constant(false));
            LambdaExpression lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    private static void ApplyMultiTenantQueryFilters(ModelBuilder modelBuilder, ICurrentTenant currentTenant)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType)))
        {
            typeof(ModelBuilderExtensions)
                .GetMethod(nameof(SetMultiTenantFilter), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [modelBuilder, currentTenant]);
        }
    }

    // Méthode générique typée : EF Core traite `currentTenant.Id` comme une closure
    // réévaluée à chaque requête (pas un snapshot capturé à l'enregistrement du filtre).
    private static void SetMultiTenantFilter<TEntity>(ModelBuilder modelBuilder, ICurrentTenant currentTenant)
        where TEntity : class, IMultiTenant =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == currentTenant.Id);
}
