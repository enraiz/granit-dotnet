using FluentAssertions;
using Granit.Features.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Granit.Features.EntityFrameworkCore.Tests;

public sealed class GranitFeaturesDbContextTests
{
    private static GranitFeaturesDbContext CreateInMemory() =>
        new(new DbContextOptionsBuilder<GranitFeaturesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // -------------------------------------------------------------------------
    // Schema creation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EnsureCreatedAsync_WithInMemoryProvider_DoesNotThrow()
    {
        await using GranitFeaturesDbContext ctx = CreateInMemory();

        Func<Task> act = () => ctx.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------------
    // Entity configuration — model metadata
    // -------------------------------------------------------------------------

    [Fact]
    public void Model_TableName_IsFeatureOverrides()
    {
        using GranitFeaturesDbContext ctx = CreateInMemory();

        string? tableName = ctx.Model
            .FindEntityType(typeof(TenantFeatureOverride))!
            .GetTableName();

        tableName.Should().Be("feature_overrides");
    }

    [Fact]
    public void Model_UniqueIndex_OnTenantIdAndFeatureName()
    {
        using GranitFeaturesDbContext ctx = CreateInMemory();

        IEntityType entityType = ctx.Model.FindEntityType(typeof(TenantFeatureOverride))!;

        IIndex? uniqueIndex = entityType.GetIndexes().FirstOrDefault(i =>
            i.IsUnique &&
            i.Properties.Any(p => p.Name == nameof(TenantFeatureOverride.TenantId)) &&
            i.Properties.Any(p => p.Name == nameof(TenantFeatureOverride.FeatureName)));

        uniqueIndex.Should().NotBeNull(
            "a unique composite index on (TenantId, FeatureName) must be configured");
    }

    [Fact]
    public void Model_FeatureName_HasMaxLength200()
    {
        using GranitFeaturesDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(TenantFeatureOverride))!
            .FindProperty(nameof(TenantFeatureOverride.FeatureName));

        property!.GetMaxLength().Should().Be(200);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Model_Value_HasMaxLength2000()
    {
        using GranitFeaturesDbContext ctx = CreateInMemory();

        IProperty? property = ctx.Model
            .FindEntityType(typeof(TenantFeatureOverride))!
            .FindProperty(nameof(TenantFeatureOverride.Value));

        property!.GetMaxLength().Should().Be(2000);
        property.IsNullable.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // CRUD round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveAndReload_AllFields_MatchOriginal()
    {
        await using GranitFeaturesDbContext ctx = CreateInMemory();
        Guid tenantId = Guid.NewGuid();

        TenantFeatureOverride entity = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FeatureName = "Guava.MaxPatientsCount",
            Value = "5000",
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedBy = "admin@digitaldynamics.be",
        };

        ctx.FeatureOverrides.Add(entity);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        ctx.ChangeTracker.Clear();

        TenantFeatureOverride? loaded = await ctx.FeatureOverrides
            .FindAsync([entity.Id], TestContext.Current.CancellationToken);

        loaded.Should().NotBeNull();
        loaded!.TenantId.Should().Be(tenantId);
        loaded.FeatureName.Should().Be("Guava.MaxPatientsCount");
        loaded.Value.Should().Be("5000");
        loaded.CreatedBy.Should().Be("admin@digitaldynamics.be");
        loaded.CreatedAt.Should().Be(entity.CreatedAt);
    }
}
