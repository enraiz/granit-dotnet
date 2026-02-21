// =============================================================================
// Tests - ModelBuilderExtensions
// =============================================================================
// Vérifie que ApplyFoundationConventions applique les query filters globaux
// pour la suppression logique (RGPD) sur les entités ISoftDeletable.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.Persistence.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalDynamics.Foundation.Persistence.Tests;

public sealed class ModelBuilderExtensionsTests
{
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

    private static TestDbContext CreateContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
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

internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<TestProduct> Products => Set<TestProduct>();
    public DbSet<TestCategory> Categories => Set<TestCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyFoundationConventions();
}

#endregion
