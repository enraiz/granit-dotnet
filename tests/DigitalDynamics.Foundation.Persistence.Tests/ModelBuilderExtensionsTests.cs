// =============================================================================
// Tests - ModelBuilderExtensions
// =============================================================================
// Vérifie que ApplyFoundationConventions applique :
//   - Le query filter global ISoftDeletable (WHERE IsDeleted = false)
//   - Le query filter global IMultiTenant (WHERE TenantId = currentTenant.Id)
//
// Note sur les tests multi-tenant :
// EF Core InMemory évalue les closures de filtre via son partial evaluator,
// qui peut différer du mode relationnel (SQL paramétré). Pour les tests de
// comportement du filtre, on compile l'expression directement via
// LambdaExpression.Compile() et on l'invoque sans passer par le pipeline EF Core.
// Cela teste ce qui compte : la fermeture est-elle dynamique (re-évaluée par appel) ?
// =============================================================================

using System.Linq.Expressions;
using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Persistence.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace DigitalDynamics.Foundation.Persistence.Tests;

public sealed class ModelBuilderExtensionsTests
{
    // Singleton partagé entre tous les tests — EF Core cache le modèle par type de
    // DbContext, donc la closure du filter reference toujours cette même instance.
    private static readonly MutableCurrentTenant SharedTenant = new();

    // -------------------------------------------------------------------------
    // Soft delete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApplyFoundationConventions_FiltersSoftDeletedEntities()
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
    public async Task ApplyFoundationConventions_IgnoreQueryFilters_ReturnsAll()
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
    public async Task ApplyFoundationConventions_NonSoftDeletableEntity_NotFiltered()
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
    // Multi-tenant query filter — vérification du modèle et de l'expression
    // -------------------------------------------------------------------------

    [Fact]
    public void ApplyFoundationConventions_MultiTenant_QueryFilterIsRegisteredOnModel()
    {
        // Arrange
        using TestMultiTenantDbContext context = CreateMultiTenantContext();

        // Act
        IEntityType? entityType = context.Model.FindEntityType(typeof(TestTenantEntity));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().FirstOrDefault().Should().NotBeNull("un filtre IMultiTenant doit être enregistré");
    }

    [Fact]
    public void ApplyFoundationConventions_WithoutCurrentTenant_NoMultiTenantFilter()
    {
        // Arrange
        using TestMultiTenantDbContextWithoutFilter context = CreateContextWithoutFilter();

        // Act
        IEntityType? entityType = context.Model.FindEntityType(typeof(TestTenantEntity));

        // Assert — aucun filtre multi-tenant enregistré quand currentTenant n'est pas fourni
        entityType!.GetDeclaredQueryFilters().FirstOrDefault().Should().BeNull(
            "aucun filtre multi-tenant ne doit être enregistré sans ICurrentTenant");
    }

    [Fact]
    public void ApplyFoundationConventions_MultiTenant_FilterExpression_MatchesCurrentTenant()
    {
        // Arrange — filtre compilé et invoqué directement, sans pipeline EF Core InMemory
        // (le partial evaluator d'EF Core peut capturer la valeur au moment de la compilation
        // de requête ; la compilation directe via Compile() garantit la dynamicité de la closure)
        Guid tenantA = Guid.NewGuid();
        SharedTenant.Id = tenantA;

        using TestMultiTenantDbContext context = CreateMultiTenantContext();
        LambdaExpression? filter = context.Model.FindEntityType(typeof(TestTenantEntity))?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        filter.Should().NotBeNull();

        Func<TestTenantEntity, bool> compiled = (Func<TestTenantEntity, bool>)filter!.Compile();

        // Assert — entité du tenant actuel : passe le filtre
        compiled(new TestTenantEntity { TenantId = tenantA }).Should().BeTrue("entité du tenant courant");
        compiled(new TestTenantEntity { TenantId = Guid.NewGuid() }).Should().BeFalse("entité d'un autre tenant");
        compiled(new TestTenantEntity { TenantId = null }).Should().BeFalse("entité sans tenant");
    }

    [Fact]
    public void ApplyFoundationConventions_MultiTenant_FilterExpression_EvaluatesDynamically()
    {
        // Arrange — vérifie que la closure réévalue dynamiquement currentTenant.Id
        // (comportement identique à la production avec ICurrentTenant AsyncLocal)
        Guid tenantA = Guid.NewGuid();
        SharedTenant.Id = tenantA;

        using TestMultiTenantDbContext context = CreateMultiTenantContext();
        LambdaExpression? filter = context.Model.FindEntityType(typeof(TestTenantEntity))?.GetDeclaredQueryFilters().FirstOrDefault()?.Expression;
        Func<TestTenantEntity, bool> compiled = (Func<TestTenantEntity, bool>)filter!.Compile();

        // Premier tenant actif
        compiled(new TestTenantEntity { TenantId = tenantA }).Should().BeTrue();

        // Changer le tenant (simule un changement de contexte AsyncLocal)
        Guid tenantB = Guid.NewGuid();
        SharedTenant.Id = tenantB;

        // Assert — le filtre s'adapte dynamiquement
        compiled(new TestTenantEntity { TenantId = tenantB }).Should().BeTrue("filtre réévalué après changement de tenant");
        compiled(new TestTenantEntity { TenantId = tenantA }).Should().BeFalse("ancien tenant filtré");

        // Reset
        SharedTenant.Id = null;
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

    // ICurrentTenant mutable partagée : simule le singleton AsyncLocal de production.
    private sealed class MutableCurrentTenant : ICurrentTenant
    {
        public bool IsAvailable => Id.HasValue;
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public IDisposable Change(Guid? id, string? name = null) => throw new NotSupportedException();
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

internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<TestProduct> Products => Set<TestProduct>();
    public DbSet<TestCategory> Categories => Set<TestCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyFoundationConventions();
}

internal sealed class TestMultiTenantDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public TestMultiTenantDbContext(DbContextOptions<TestMultiTenantDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<TestTenantEntity> TenantEntities => Set<TestTenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyFoundationConventions(_currentTenant);
}

// DbContext sans filtre multi-tenant (currentTenant non fourni)
internal sealed class TestMultiTenantDbContextWithoutFilter : DbContext
{
    public TestMultiTenantDbContextWithoutFilter(DbContextOptions<TestMultiTenantDbContextWithoutFilter> options)
        : base(options) { }

    public DbSet<TestTenantEntity> TenantEntities => Set<TestTenantEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyFoundationConventions();
}

#endregion
