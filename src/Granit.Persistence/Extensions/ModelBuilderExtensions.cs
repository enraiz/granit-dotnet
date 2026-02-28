using System.Linq.Expressions;
using System.Reflection;
using Granit.Core.DataFiltering;
using Granit.Core.Domain;
using Granit.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Granit.Persistence.Extensions;

/// <summary>
/// Extensions for configuring Granit conventions on the EF Core ModelBuilder.
/// </summary>
public static class ModelBuilderExtensions
{
    private static readonly MethodInfo SetEntityFilterMethod =
        typeof(ModelBuilderExtensions)
            .GetMethod(nameof(SetEntityFilter), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Applies Granit conventions to all entity types in the model:
    /// <list type="bullet">
    ///   <item><b>Query filters</b>:
    ///     <see cref="ISoftDeletable"/>, <see cref="IActive"/>,
    ///     <see cref="IProcessingRestrictable"/>, <see cref="IMultiTenant"/>
    ///   </item>
    ///   <item><b>Translation conventions</b>:
    ///     <see cref="ITranslation{TParent}"/> → FK, cascade delete, unique index (ParentId, Culture)
    ///   </item>
    /// </list>
    /// Entities implementing multiple filter interfaces receive a single combined
    /// <c>HasQueryFilter</c> (conditions joined with <c>AndAlso</c>), which fixes
    /// the silent filter-overwrite bug in EF Core when multiple calls are made.
    /// </summary>
    /// <param name="modelBuilder">The EF Core ModelBuilder.</param>
    /// <param name="currentTenant">
    /// Current tenant service. If <c>null</c>, the multi-tenant filter is not applied.
    /// Pass the instance injected in the DbContext constructor for correct lazy evaluation
    /// (re-evaluated on each query via AsyncLocal).
    /// </param>
    /// <param name="dataFilter">
    /// Data filter service. If <c>null</c>, all filters are always applied
    /// (backward-compatible behavior identical to before this parameter was added).
    /// When provided, each filter can be individually bypassed at runtime via
    /// <c>IDataFilter.Disable&lt;TFilter&gt;()</c>.
    /// </param>
    public static ModelBuilder ApplyGranitConventions(
        this ModelBuilder modelBuilder,
        ICurrentTenant? currentTenant = null,
        IDataFilter? dataFilter = null)
    {
        // FilterProxy wraps IDataFilter? and exposes simple boolean properties.
        // EF Core extracts property access on a ConstantExpression as a query parameter
        // re-evaluated on each query — the same mechanism used by currentTenant.Id in
        // the multi-tenant filter. This avoids the risk of EF Core attempting SQL
        // translation of a generic method call (IsEnabled<T>()).
        FilterProxy proxy = new(dataFilter);

        foreach (Type clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType))
        {
            bool hasSoftDelete = typeof(ISoftDeletable).IsAssignableFrom(clrType);
            bool hasActive = typeof(IActive).IsAssignableFrom(clrType);
            bool hasProcessingRestriction = typeof(IProcessingRestrictable).IsAssignableFrom(clrType);
            bool hasMultiTenant = typeof(IMultiTenant).IsAssignableFrom(clrType)
                && currentTenant is not null;

            if (!hasSoftDelete && !hasActive && !hasProcessingRestriction && !hasMultiTenant)
            {
                continue;
            }

            SetEntityFilterMethod // NOSONAR S3011 - intentional: generic EF Core filter pattern requires reflection
                .MakeGenericMethod(clrType)
                .Invoke(null, [modelBuilder, currentTenant, proxy]);
        }

        // --- Translation conventions ---
        // Detects ITranslation<TParent> implementations and configures:
        //   - FK from Translation.ParentId → Parent.Id with cascade delete
        //   - Unique index on (ParentId, Culture)
        //   - Culture max length (20, BCP 47)
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            Type? translationInterface = entityType.ClrType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(ITranslation<>));

            if (translationInterface is null)
            {
                continue;
            }

            Type parentType = translationInterface.GetGenericArguments()[0];

            typeof(ModelBuilderExtensions)
                .GetMethod(nameof(ConfigureTranslation), BindingFlags.Static | BindingFlags.NonPublic)! // NOSONAR S3011 - intentional: generic EF Core convention pattern requires reflection
                .MakeGenericMethod(entityType.ClrType, parentType)
                .Invoke(null, [modelBuilder]);
        }

        return modelBuilder;
    }

    // Configures a translation entity type: FK, cascade delete, unique index, Culture max length.
    private static void ConfigureTranslation<TTranslation, TParent>(ModelBuilder modelBuilder)
        where TTranslation : class, ITranslation<TParent>
        where TParent : Entity
    {
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TTranslation> builder =
            modelBuilder.Entity<TTranslation>();

        // Culture column: max 20 chars (BCP 47 — same as LocalizationOverride)
        builder.Property(t => t.Culture)
            .HasMaxLength(20)
            .IsRequired();

        // FK: Translation.ParentId → Parent.Id, cascade delete (HDS/RGPD compliance)
        builder.HasOne(t => t.Parent)
            .WithMany()
            .HasForeignKey(t => t.ParentId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

        // Unique index: one translation per (parent, culture)
        builder.HasIndex(t => new { t.ParentId, t.Culture })
            .IsUnique();
    }

    // Builds and registers a single combined HasQueryFilter for TEntity.
    // Each applicable filter interface contributes one condition: bypass || realCondition.
    // All conditions are combined with AndAlso — one HasQueryFilter call per entity type.
    private static void SetEntityFilter<TEntity>(
        ModelBuilder modelBuilder,
        ICurrentTenant? currentTenant,
        FilterProxy proxy)
        where TEntity : class
    {
        ParameterExpression param = Expression.Parameter(typeof(TEntity), "e");
        List<Expression> conditions = [];

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            // bypass = !proxy.SoftDeleteEnabled (re-evaluated by EF Core as a query parameter)
            // real   = !e.IsDeleted
            Expression bypass = Expression.Not(
                Expression.Property(Expression.Constant(proxy), nameof(FilterProxy.SoftDeleteEnabled)));
            Expression notDeleted = Expression.Not(
                Expression.Property(param, nameof(ISoftDeletable.IsDeleted)));
            conditions.Add(Expression.OrElse(bypass, notDeleted));
        }

        if (typeof(IActive).IsAssignableFrom(typeof(TEntity)))
        {
            // bypass = !proxy.ActiveEnabled
            // real   = e.IsActive
            Expression bypass = Expression.Not(
                Expression.Property(Expression.Constant(proxy), nameof(FilterProxy.ActiveEnabled)));
            Expression isActive = Expression.Property(param, nameof(IActive.IsActive));
            conditions.Add(Expression.OrElse(bypass, isActive));
        }

        if (typeof(IProcessingRestrictable).IsAssignableFrom(typeof(TEntity)))
        {
            // bypass = !proxy.ProcessingRestrictableEnabled
            // real   = !e.IsProcessingRestricted
            Expression bypass = Expression.Not(
                Expression.Property(Expression.Constant(proxy), nameof(FilterProxy.ProcessingRestrictableEnabled)));
            Expression notRestricted = Expression.Not(
                Expression.Property(param, nameof(IProcessingRestrictable.IsProcessingRestricted)));
            conditions.Add(Expression.OrElse(bypass, notRestricted));
        }

        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)) && currentTenant is not null)
        {
            // bypass = !proxy.MultiTenantEnabled
            // real   = e.TenantId == currentTenant.Id (closure re-evaluated via AsyncLocal)
            Expression bypass = Expression.Not(
                Expression.Property(Expression.Constant(proxy), nameof(FilterProxy.MultiTenantEnabled)));
            Expression tenantMatch = Expression.Equal(
                Expression.Property(param, nameof(IMultiTenant.TenantId)),
                Expression.Property(Expression.Constant(currentTenant), nameof(ICurrentTenant.Id)));
            conditions.Add(Expression.OrElse(bypass, tenantMatch));
        }

        if (conditions.Count == 0)
        {
            return;
        }

        Expression combined = conditions.Aggregate(Expression.AndAlso);
        modelBuilder.Entity<TEntity>().HasQueryFilter(Expression.Lambda<Func<TEntity, bool>>(combined, param));
    }

    // Internal wrapper: EF Core evaluates simple property access on a ConstantExpression
    // as a query parameter re-evaluated on each query execution.
    // Registered as Singleton + static AsyncLocal in DataFilter → the captured instance
    // reads the correct per-flow state on every query, regardless of model caching.
    private sealed class FilterProxy(IDataFilter? dataFilter)
    {
        private readonly IDataFilter? _dataFilter = dataFilter;

        public bool SoftDeleteEnabled => _dataFilter?.IsEnabled<ISoftDeletable>() ?? true;
        public bool ActiveEnabled => _dataFilter?.IsEnabled<IActive>() ?? true;
        public bool ProcessingRestrictableEnabled => _dataFilter?.IsEnabled<IProcessingRestrictable>() ?? true;
        public bool MultiTenantEnabled => _dataFilter?.IsEnabled<IMultiTenant>() ?? true;
    }
}
