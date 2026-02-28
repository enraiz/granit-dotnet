using Granit.Features.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Granit.Features.EntityFrameworkCore.Tests;

public sealed class EfCoreFeatureStoreTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private sealed class InMemoryContextFactory(string dbName) : IDbContextFactory<GranitFeaturesDbContext>
    {
        public GranitFeaturesDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<GranitFeaturesDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);

        public Task<GranitFeaturesDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static EfCoreFeatureStore CreateStore(string dbName) =>
        new(new InMemoryContextFactory(dbName));

    private static async Task SeedAsync(
        string dbName,
        Guid tenantId,
        string featureName,
        string value,
        CancellationToken ct = default)
    {
        InMemoryContextFactory factory = new(dbName);
        await using GranitFeaturesDbContext ctx = factory.CreateDbContext();
        ctx.FeatureOverrides.Add(new TenantFeatureOverride
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FeatureName = featureName,
            Value = value,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed",
        });
        await ctx.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // GetOrNullAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_ExistingOverride_ReturnsValue()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantId = Guid.NewGuid();
        await SeedAsync(db, tenantId, "Guava.MaxPatientsCount", "500",
            TestContext.Current.CancellationToken);

        EfCoreFeatureStore store = CreateStore(db);
        string? result = await store.GetOrNullAsync(
            "Guava.MaxPatientsCount", tenantId.ToString(),
            TestContext.Current.CancellationToken);

        result.ShouldBe("500");
    }

    [Fact]
    public async Task GetOrNullAsync_NoOverride_ReturnsNull()
    {
        EfCoreFeatureStore store = CreateStore(Guid.NewGuid().ToString());

        string? result = await store.GetOrNullAsync(
            "Guava.VideoConsultation", Guid.NewGuid().ToString(),
            TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOrNullAsync_DifferentTenant_ReturnsNull()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        await SeedAsync(db, tenantA, "Guava.MaxPatientsCount", "500",
            TestContext.Current.CancellationToken);

        EfCoreFeatureStore store = CreateStore(db);
        string? result = await store.GetOrNullAsync(
            "Guava.MaxPatientsCount", tenantB.ToString(),
            TestContext.Current.CancellationToken);

        result.ShouldBeNull("override belongs to tenant A, not tenant B");
    }

    [Fact]
    public async Task GetOrNullAsync_NullTenantId_GlobalScope_ReturnsValue()
    {
        string db = Guid.NewGuid().ToString();
        EfCoreFeatureStore store = CreateStore(db);

        // Store a global (null tenant) override via the store itself
        await store.SetAsync(
            "Guava.GlobalFeature", null, "global-value",
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync(
            "Guava.GlobalFeature", null,
            TestContext.Current.CancellationToken);

        result.ShouldBe("global-value");
    }

    // -------------------------------------------------------------------------
    // SetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetAsync_NewOverride_PersistsValue()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantId = Guid.NewGuid();
        EfCoreFeatureStore store = CreateStore(db);

        await store.SetAsync(
            "Guava.MaxPatientsCount", tenantId.ToString(), "1000",
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync(
            "Guava.MaxPatientsCount", tenantId.ToString(),
            TestContext.Current.CancellationToken);

        result.ShouldBe("1000");
    }

    [Fact]
    public async Task SetAsync_ExistingOverride_UpdatesValue()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantId = Guid.NewGuid();
        await SeedAsync(db, tenantId, "Guava.MaxPatientsCount", "200",
            TestContext.Current.CancellationToken);

        EfCoreFeatureStore store = CreateStore(db);
        await store.SetAsync(
            "Guava.MaxPatientsCount", tenantId.ToString(), "5000",
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync(
            "Guava.MaxPatientsCount", tenantId.ToString(),
            TestContext.Current.CancellationToken);

        result.ShouldBe("5000");
    }

    [Fact]
    public async Task SetAsync_IsIdempotent_WhenCalledTwiceWithSameValue()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantId = Guid.NewGuid();
        EfCoreFeatureStore store = CreateStore(db);

        await store.SetAsync(
            "Guava.Feature", tenantId.ToString(), "true",
            TestContext.Current.CancellationToken);
        await store.SetAsync(
            "Guava.Feature", tenantId.ToString(), "true",
            TestContext.Current.CancellationToken);

        InMemoryContextFactory factory = new(db);
        await using GranitFeaturesDbContext ctx = factory.CreateDbContext();
        int count = await ctx.FeatureOverrides.CountAsync(
            o => o.FeatureName == "Guava.Feature" && o.TenantId == tenantId,
            TestContext.Current.CancellationToken);

        count.ShouldBe(1, "upsert must not create duplicates");
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ExistingOverride_RemovesRow()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantId = Guid.NewGuid();
        await SeedAsync(db, tenantId, "Guava.VideoConsultation", "true",
            TestContext.Current.CancellationToken);

        EfCoreFeatureStore store = CreateStore(db);
        await store.DeleteAsync(
            "Guava.VideoConsultation", tenantId.ToString(),
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync(
            "Guava.VideoConsultation", tenantId.ToString(),
            TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnknownOverride_DoesNotThrow()
    {
        EfCoreFeatureStore store = CreateStore(Guid.NewGuid().ToString());

        Func<Task> act = () => store.DeleteAsync(
            "Guava.Ghost", Guid.NewGuid().ToString(),
            TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task DeleteAsync_IsolatesTenants_DoesNotRemoveOtherTenantOverride()
    {
        string db = Guid.NewGuid().ToString();
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        await SeedAsync(db, tenantA, "Guava.Feature", "true",
            TestContext.Current.CancellationToken);
        await SeedAsync(db, tenantB, "Guava.Feature", "true",
            TestContext.Current.CancellationToken);

        EfCoreFeatureStore store = CreateStore(db);
        await store.DeleteAsync(
            "Guava.Feature", tenantA.ToString(),
            TestContext.Current.CancellationToken);

        string? resultB = await store.GetOrNullAsync(
            "Guava.Feature", tenantB.ToString(),
            TestContext.Current.CancellationToken);

        resultB.ShouldBe("true", "tenant B override must not be affected");
    }
}
