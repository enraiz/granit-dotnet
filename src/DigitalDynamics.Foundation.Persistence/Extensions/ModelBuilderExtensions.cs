
using System.Linq.Expressions;
using DigitalDynamics.Foundation.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace DigitalDynamics.Foundation.Persistence.Extensions;

/// <summary>
/// Extensions pour configurer les conventions Foundation sur le ModelBuilder EF Core.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applique les conventions Foundation :
    /// - Query filter global ISoftDeletable (WHERE IsDeleted = false)
    /// - Configuration de la table AuditLogEntry
    /// </summary>
    public static ModelBuilder ApplyFoundationConventions(this ModelBuilder modelBuilder)
    {
        ApplySoftDeleteQueryFilters(modelBuilder);
        return modelBuilder;
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
