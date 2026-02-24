// =============================================================================
// Tests - ModelBuilderExtensions
// =============================================================================
// Verifies that ApplyGranitConventions applies:
//   - ISoftDeletable global query filter (WHERE IsDeleted = false)
//   - IMultiTenant global query filter (WHERE TenantId = currentTenant.Id)
//   - IActive global query filter (WHERE IsActive = true)
//   - A single HasQueryFilter per entity (fixes the combination bug)
//   - Selective bypass via IDataFilter
//   - Backward compatibility (dataFilter = null)
//
// Note on EF Core model caching:
// EF Core caches the model by DbContext type. The filter closures (FilterProxy,
// currentTenant) captured during the first OnModelCreating call are reused for
// all subsequent instances of the same type. Tests must therefore use STATIC
// shared instances (SharedTenant, SharedDataFilter) so that mutations in tests
// affect the same objects captured in the cached model expressions.
//
// Note on multi-tenant and DataFilter tests:
// EF Core InMemory evaluates filter closures via its partial evaluator,
// which may differ from relational mode (SQL parameters). For filter behavior
// tests, the expression is compiled directly via LambdaExpression.Compile()
// and invoked without going through the EF Core pipeline.
// This tests what matters: is the closure dynamic (re-evaluated on each call)?
// =============================================================================

using System.Linq.Expressions;
using FluentAssertions;
using Granit.Core.DataFiltering;
using Granit.Core.Domain;
using Granit.MultiTenancy;
using Granit.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Granit.Persistence.Tests;

public sealed class ModelBuilderExtensionsTests
{
    // Static shared instances across all tests in this class.
    // EF Core caches the model by DbContext type: the closures captured during the first
    // OnModelCreating call are reused. Using static instances ensures that test mutations
    // target the same objects held by the cached expression tree.
    private static readonly MutableCurrentTenant SharedTenant = new();
    private static readonly MutableDataFilter SharedDataFilter = new();

    // -------------------------------------------------------------------------
    // Soft delete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApplyGranitConventions_FiltersSoftDeletedEntities()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.Products.Add(new TestProduct { Name = "Active", IsDeleted = false });
        context.Products.Add(new TestProduct { Name = "Deleted", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow, DeletedBy = "test" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        List<TestProduct> results = await context.Products.ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task ApplyGranitConventions_IgnoreQueryFilters_ReturnsAll()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.Products.Add(new TestProduct { Name = "Active", IsDeleted = false });
        context.Products.Add(new TestProduct { Name = "Deleted", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow, DeletedBy = "test" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        List<TestProduct> results = await context.Products.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApplyGranitConventions_NonSoftDeletableEntity_NotFiltered()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.Categories.Add(new TestCategory { Name = "Cat1" });
        context.Categories.Add(new TestCategory { Name = "Cat2" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        List<TestCategory> results = await context.Categories.ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // Multi-tenant query filter — model and expression verification
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyGranitConventions_MultiTenant_QueryFilterIsRegisteredOnModel()
    {
        // Arrange
        using TestMultiTenantDbContext context = CreateMultiTenantContext();

        // Act
        IEntityType? entityType = context.Model.FindEntityType(typeof(TestTenantEntity));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().FirstOrDefault().Should().NotBeNull("an IMultiTenant filter must be registered");
    }

    [Fact]
    public void ApplyGranitConventions_WithoutCurrentTenant_NoMultiTenantFilter()
    {
        // Arrange
        using TestMultiTenantDbContextWithoutFilter context = CreateContextWithoutFilter();

        // Act
        IEntityType? entityType = context.Model.FindEntityType(typeof(TestTenantEntity));

        // Assert — no multi-tenant filter registered when currentTenant is not provided
        entityType!.GetDeclaredQueryFilters().FirstOrDefault().Should().BeNull(
            "no multi-tenant filter must be registered without ICurrentTenant");
    }

    [Fact]
    public void ApplyGranitConventions_MultiTenant_FilterExpression_MatchesCurrentTenant()
    {
        // Arrange — filter compiled and invoked directly, without the EF Core InMemory pipeline
        // (EF Core's partial evaluator may capture the value at query compilation time;
        // direct compilation via Compile() guarantees the closure is dynamic)
        Guid tenantA = Guid.NewGuid();
        SharedTenant.Id = tenantA;

        using TestMultiTenantDbContext context = CreateMultiTenantContext();
        LambdaExpression? filter = context.Model.FindEntityType(typeof(TestTenantEntity))?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        filter.Should().NotBeNull();

        Func<TestTenantEntity, bool> compiled = (Func<TestTenantEntity, bool>)filter!.Compile();

        // Assert — entity matching current tenant: passes the filter
        compiled(new TestTenantEntity { TenantId = tenantA }).Should().BeTrue("entity of current tenant must pass");
        compiled(new TestTenantEntity { TenantId = Guid.NewGuid() }).Should().BeFalse("entity of another tenant must be filtered");
        compiled(new TestTenantEntity { TenantId = null }).Should().BeFalse("entity without tenant must be filtered");

        SharedTenant.Id = null;
    }

    [Fact]
    public void ApplyGranitConventions_MultiTenant_FilterExpression_EvaluatesDynamically()
    {
        // Arrange — verifies that the closure dynamically re-evaluates currentTenant.Id
        // (same behavior as production with AsyncLocal ICurrentTenant)
        Guid tenantA = Guid.NewGuid();
        SharedTenant.Id = tenantA;

        using TestMultiTenantDbContext context = CreateMultiTenantContext();
        LambdaExpression? filter = context.Model.FindEntityType(typeof(TestTenantEntity))?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        Func<TestTenantEntity, bool> compiled = (Func<TestTenantEntity, bool>)filter!.Compile();

        // First active tenant
        compiled(new TestTenantEntity { TenantId = tenantA }).Should().BeTrue();

        // Change tenant (simulates an AsyncLocal context switch)
        Guid tenantB = Guid.NewGuid();
        SharedTenant.Id = tenantB;

        // Assert — filter adapts dynamically
        compiled(new TestTenantEntity { TenantId = tenantB }).Should().BeTrue("filter must re-evaluate after tenant change");
        compiled(new TestTenantEntity { TenantId = tenantA }).Should().BeFalse("previous tenant must be filtered");

        SharedTenant.Id = null;
    }

    // -------------------------------------------------------------------------
    // IActive filter
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApplyGranitConventions_FiltersInactiveEntities()
    {
        // Arrange
        await using TestDbContextWithActive context = CreateContextWithActive();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.ActiveEntities.Add(new TestActiveEntity { Name = "Active", IsActive = true });
        context.ActiveEntities.Add(new TestActiveEntity { Name = "Inactive", IsActive = false });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        List<TestActiveEntity> results = await context.ActiveEntities.ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task ApplyGranitConventions_ActiveFilter_IgnoreQueryFilters_ReturnsAll()
    {
        // Arrange
        await using TestDbContextWithActive context = CreateContextWithActive();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        context.ActiveEntities.Add(new TestActiveEntity { Name = "Active", IsActive = true });
        context.ActiveEntities.Add(new TestActiveEntity { Name = "Inactive", IsActive = false });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        List<TestActiveEntity> results = await context.ActiveEntities
            .IgnoreQueryFilters()
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // IDataFilter bypass — verified via direct expression compilation
    // (see header note on EF Core model caching and EF Core InMemory vs SQL)
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyGranitConventions_SoftDelete_DataFilter_Bypass_EvaluatesDynamically()
    {
        // Arrange — SharedDataFilter is the instance captured in the cached model expression.
        SharedDataFilter.SetEnabled<ISoftDeletable>(true);

        using TestDbContextWithDataFilter context = CreateContextWithDataFilter();
        LambdaExpression? filter = context.Model
            .FindEntityType(typeof(TestSoftDeleteWithFilter))
            ?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        filter.Should().NotBeNull();

        Func<TestSoftDeleteWithFilter, bool> compiled = (Func<TestSoftDeleteWithFilter, bool>)filter!.Compile();

        // Filter active — deleted entity excluded
        compiled(new TestSoftDeleteWithFilter { IsDeleted = true }).Should().BeFalse("deleted must be filtered");
        compiled(new TestSoftDeleteWithFilter { IsDeleted = false }).Should().BeTrue("non-deleted must pass");

        // Bypass — all entities pass (same compiled expression, dynamic re-evaluation)
        SharedDataFilter.SetEnabled<ISoftDeletable>(false);
        compiled(new TestSoftDeleteWithFilter { IsDeleted = true }).Should().BeTrue("deleted must pass when filter disabled");
        compiled(new TestSoftDeleteWithFilter { IsDeleted = false }).Should().BeTrue("non-deleted must pass when filter disabled");

        // Restore — filter active again
        SharedDataFilter.SetEnabled<ISoftDeletable>(true);
        compiled(new TestSoftDeleteWithFilter { IsDeleted = true }).Should().BeFalse("filter must be restored");
    }

    [Fact]
    public void ApplyGranitConventions_Active_DataFilter_Bypass_EvaluatesDynamically()
    {
        SharedDataFilter.SetEnabled<IActive>(true);

        using TestDbContextWithDataFilter context = CreateContextWithDataFilter();
        LambdaExpression? filter = context.Model
            .FindEntityType(typeof(TestActiveWithFilter))
            ?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        filter.Should().NotBeNull();

        Func<TestActiveWithFilter, bool> compiled = (Func<TestActiveWithFilter, bool>)filter!.Compile();

        compiled(new TestActiveWithFilter { IsActive = false }).Should().BeFalse("inactive must be filtered");
        compiled(new TestActiveWithFilter { IsActive = true }).Should().BeTrue("active must pass");

        SharedDataFilter.SetEnabled<IActive>(false);
        compiled(new TestActiveWithFilter { IsActive = false }).Should().BeTrue("inactive must pass when filter disabled");

        SharedDataFilter.SetEnabled<IActive>(true);
    }

    // -------------------------------------------------------------------------
    // Bug fix: entities combining multiple filter interfaces
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyGranitConventions_CombinedEntity_HasSingleQueryFilter()
    {
        // Arrange — SharedTenant is non-null so both ISoftDeletable and IMultiTenant conditions
        // are registered for TestCombinedEntity, combined into a single HasQueryFilter.
        using TestDbContextWithCombined context = CreateContextWithCombined();

        // Act
        IEntityType? entityType = context.Model.FindEntityType(typeof(TestCombinedEntity));

        // Assert — single HasQueryFilter (no duplication / silent overwrite)
        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().Should().HaveCount(1,
            "exactly one HasQueryFilter must be registered, even for multi-interface entities");
    }

    [Fact]
    public void ApplyGranitConventions_CombinedEntity_BothFiltersActive()
    {
        // Arrange — set state on shared instances captured in the cached model expression
        Guid tenantA = Guid.NewGuid();
        SharedTenant.Id = tenantA;
        SharedDataFilter.SetEnabled<ISoftDeletable>(true);
        SharedDataFilter.SetEnabled<IMultiTenant>(true);

        using TestDbContextWithCombined context = CreateContextWithCombined();
        LambdaExpression? filter = context.Model
            .FindEntityType(typeof(TestCombinedEntity))
            ?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        Func<TestCombinedEntity, bool> compiled = (Func<TestCombinedEntity, bool>)filter!.Compile();

        // Correct tenant, not deleted — passes
        compiled(new TestCombinedEntity { TenantId = tenantA, IsDeleted = false }).Should().BeTrue("correct tenant + not deleted");

        // Correct tenant but deleted — excluded by soft delete
        compiled(new TestCombinedEntity { TenantId = tenantA, IsDeleted = true }).Should().BeFalse("correct tenant but deleted");

        // Different tenant, not deleted — excluded by multi-tenant
        compiled(new TestCombinedEntity { TenantId = Guid.NewGuid(), IsDeleted = false }).Should().BeFalse("wrong tenant");

        SharedTenant.Id = null;
    }

    [Fact]
    public void ApplyGranitConventions_CombinedEntity_IndependentBypass()
    {
        Guid tenantA = Guid.NewGuid();
        SharedTenant.Id = tenantA;
        SharedDataFilter.SetEnabled<ISoftDeletable>(false); // soft delete bypassed
        SharedDataFilter.SetEnabled<IMultiTenant>(true);    // multi-tenant active

        using TestDbContextWithCombined context = CreateContextWithCombined();
        LambdaExpression? filter = context.Model
            .FindEntityType(typeof(TestCombinedEntity))
            ?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        Func<TestCombinedEntity, bool> compiled = (Func<TestCombinedEntity, bool>)filter!.Compile();

        // Soft delete bypassed — deleted entity from correct tenant passes
        compiled(new TestCombinedEntity { TenantId = tenantA, IsDeleted = true }).Should().BeTrue("soft delete bypassed");

        // Multi-tenant still active — wrong tenant still filtered
        compiled(new TestCombinedEntity { TenantId = Guid.NewGuid(), IsDeleted = true }).Should().BeFalse("multi-tenant still active");

        // Reset
        SharedTenant.Id = null;
        SharedDataFilter.SetEnabled<ISoftDeletable>(true);
    }

    // -------------------------------------------------------------------------
    // Backward compatibility — dataFilter = null
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyGranitConventions_NullDataFilter_FiltersAlwaysApply()
    {
        // Arrange — DbContext without IDataFilter (legacy behavior)
        using TestDbContext context = CreateContext();
        LambdaExpression? filter = context.Model
            .FindEntityType(typeof(TestProduct))
            ?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        filter.Should().NotBeNull("soft delete filter must be registered even without IDataFilter");

        Func<TestProduct, bool> compiled = (Func<TestProduct, bool>)filter!.Compile();

        // Assert — filter always active
        compiled(new TestProduct { IsDeleted = true }).Should().BeFalse("deleted must be filtered");
        compiled(new TestProduct { IsDeleted = false }).Should().BeTrue("non-deleted must pass");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TestDbContext CreateContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static TestMultiTenantDbContext CreateMultiTenantContext()
    {
        DbContextOptions<TestMultiTenantDbContext> options = new DbContextOptionsBuilder<TestMultiTenantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestMultiTenantDbContext(options, SharedTenant);
    }

    private static TestMultiTenantDbContextWithoutFilter CreateContextWithoutFilter()
    {
        DbContextOptions<TestMultiTenantDbContextWithoutFilter> options =
            new DbContextOptionsBuilder<TestMultiTenantDbContextWithoutFilter>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        return new TestMultiTenantDbContextWithoutFilter(options);
    }

    private static TestDbContextWithActive CreateContextWithActive()
    {
        DbContextOptions<TestDbContextWithActive> options =
            new DbContextOptionsBuilder<TestDbContextWithActive>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        return new TestDbContextWithActive(options);
    }

    // Always uses the static SharedDataFilter — required so that test mutations target
    // the same instance captured in the EF Core cached model expression.
    private static TestDbContextWithDataFilter CreateContextWithDataFilter()
    {
        DbContextOptions<TestDbContextWithDataFilter> options =
            new DbContextOptionsBuilder<TestDbContextWithDataFilter>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        return new TestDbContextWithDataFilter(options, SharedDataFilter);
    }

    // Always uses SharedTenant and SharedDataFilter — same reason as above.
    private static TestDbContextWithCombined CreateContextWithCombined()
    {
        DbContextOptions<TestDbContextWithCombined> options =
            new DbContextOptionsBuilder<TestDbContextWithCombined>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        return new TestDbContextWithCombined(options, SharedTenant, SharedDataFilter);
    }

    // Mutable ICurrentTenant: simulates the production AsyncLocal singleton.
    private sealed class MutableCurrentTenant : ICurrentTenant
    {
        public bool IsAvailable => Id.HasValue;
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public IDisposable Change(Guid? id, string? name = null) => throw new NotSupportedException();
    }

    // Mutable IDataFilter for tests: direct control without AsyncLocal.
    // Verifies that filter closures are dynamic (re-evaluated on each call).
    private sealed class MutableDataFilter : IDataFilter
    {
        private readonly Dictionary<Type, bool> _state = [];

        public void SetEnabled<TFilter>(bool enabled) => _state[typeof(TFilter)] = enabled;

        public IDisposable Disable<TFilter>() where TFilter : class
        {
            _state[typeof(TFilter)] = false;
            return NullScope.Instance;
        }

        public IDisposable Enable<TFilter>() where TFilter : class
        {
            _state[typeof(TFilter)] = true;
            return NullScope.Instance;
        }

        public bool IsEnabled<TFilter>() where TFilter : class =>
            !_state.TryGetValue(typeof(TFilter), out bool value) || value;

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

#region Test entities

internal sealed class TestProduct : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

internal sealed class TestCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class TestTenantEntity : IMultiTenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}

internal sealed class TestActiveEntity : IActive
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

internal sealed class TestSoftDeleteWithFilter : ISoftDeletable
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

internal sealed class TestActiveWithFilter : IActive
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

internal sealed class TestCombinedEntity : ISoftDeletable, IMultiTenant
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public Guid? TenantId { get; set; }
}

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestProduct> Products => Set<TestProduct>();
    public DbSet<TestCategory> Categories => Set<TestCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyGranitConventions();
}

internal sealed class TestMultiTenantDbContext(DbContextOptions<TestMultiTenantDbContext> options, ICurrentTenant currentTenant) : DbContext(options)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public DbSet<TestTenantEntity> TenantEntities => Set<TestTenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyGranitConventions(_currentTenant);
}

// DbContext without multi-tenant filter (currentTenant not provided)
internal sealed class TestMultiTenantDbContextWithoutFilter(DbContextOptions<TestMultiTenantDbContextWithoutFilter> options) : DbContext(options)
{
    public DbSet<TestTenantEntity> TenantEntities => Set<TestTenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyGranitConventions();
}

internal sealed class TestDbContextWithActive(DbContextOptions<TestDbContextWithActive> options) : DbContext(options)
{
    public DbSet<TestActiveEntity> ActiveEntities => Set<TestActiveEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyGranitConventions();
}

internal sealed class TestDbContextWithDataFilter(
    DbContextOptions<TestDbContextWithDataFilter> options,
    IDataFilter dataFilter) : DbContext(options)
{
    private readonly IDataFilter _dataFilter = dataFilter;

    public DbSet<TestSoftDeleteWithFilter> SoftDeleteEntities => Set<TestSoftDeleteWithFilter>();
    public DbSet<TestActiveWithFilter> ActiveEntities => Set<TestActiveWithFilter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyGranitConventions(dataFilter: _dataFilter);
}

internal sealed class TestDbContextWithCombined(
    DbContextOptions<TestDbContextWithCombined> options,
    ICurrentTenant? currentTenant,
    IDataFilter? dataFilter) : DbContext(options)
{
    private readonly ICurrentTenant? _currentTenant = currentTenant;
    private readonly IDataFilter? _dataFilter = dataFilter;

    public DbSet<TestCombinedEntity> CombinedEntities => Set<TestCombinedEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
}

#endregion
