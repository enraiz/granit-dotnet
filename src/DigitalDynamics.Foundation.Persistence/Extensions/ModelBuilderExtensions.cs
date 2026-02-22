using System.Linq.Expressions;
using System.Reflection;
using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DigitalDynamics.Foundation.Persistence.Extensions;

/// <summary>
/// Extensions for configuring Foundation conventions on the EF Core ModelBuilder.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies Foundation conventions:
    /// - Global query filter for <see cref="ISoftDeletable"/> (WHERE IsDeleted = false)
    /// - Global query filter for <see cref="IMultiTenant"/> (WHERE TenantId = currentTenant.Id),
    ///   only if <paramref name="currentTenant"/> is provided.
    /// </summary>
    /// <param name="modelBuilder">The EF Core ModelBuilder.</param>
    /// <param name="currentTenant">
    /// Current tenant service. If <c>null</c>, the multi-tenant filter is not applied.
    /// Pass the instance injected in the DbContext constructor for correct lazy evaluation
    /// (re-evaluated on each query via AsyncLocal).
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
                .GetMethod(nameof(SetMultiTenantFilter), BindingFlags.Static | BindingFlags.NonPublic)! // NOSONAR S3011 - intentional: generic EF Core filter pattern requires reflection
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [modelBuilder, currentTenant]);
        }
    }

    // Generic typed method: EF Core evaluates `currentTenant.Id` as a closure
    // re-evaluated on each query (not a snapshot captured at filter registration).
    private static void SetMultiTenantFilter<TEntity>(ModelBuilder modelBuilder, ICurrentTenant currentTenant)
        where TEntity : class, IMultiTenant =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == currentTenant.Id);
}
