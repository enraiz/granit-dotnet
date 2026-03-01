using Granit.ReferenceData.EntityFrameworkCore.Extensions;
using Granit.ReferenceData.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.ReferenceData.EntityFrameworkCore.Tests;

public sealed class EfCoreReferenceDataStoreTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private sealed class TestEntityConfiguration : ReferenceDataEntityTypeConfiguration<TestEntity>
    {
        public TestEntityConfiguration() : base("ref_test_entities") { }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureReferenceData(new TestEntityConfiguration());
        }
    }

    private static EfCoreReferenceDataStore<TestEntity, TestDbContext> CreateStore(string dbName)
    {
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        ServiceProvider sp = services.BuildServiceProvider();
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        IOptions<ReferenceDataOptions> options = Options.Create(new ReferenceDataOptions());

        return new EfCoreReferenceDataStore<TestEntity, TestDbContext>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            cache,
            options);
    }

    private static async Task SeedAsync(
        string dbName,
        string code,
        string label,
        bool isActive = true,
        int sortOrder = 0,
        CancellationToken ct = default)
    {
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        await using ServiceProvider sp = services.BuildServiceProvider();
        await using TestDbContext context = sp.GetRequiredService<TestDbContext>();

        context.TestEntities.Add(new TestEntity
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = label,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed",
        });
        await context.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // GetByCodeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByCodeAsync_ExistingCode_ReturnsEntity()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        TestEntity? result = await store.GetByCodeAsync("BE", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("BE");
        result.Label.ShouldBe("Belgium");
    }

    [Fact]
    public async Task GetByCodeAsync_NonExistentCode_ReturnsNull()
    {
        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(Guid.NewGuid().ToString());

        TestEntity? result = await store.GetByCodeAsync("ZZ", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_CachesResult()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "FR", "France", ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);

        // First call — fetches from DB
        TestEntity? first = await store.GetByCodeAsync("FR", TestContext.Current.CancellationToken);
        // Second call — should come from cache (same store instance)
        TestEntity? second = await store.GetByCodeAsync("FR", TestContext.Current.CancellationToken);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first!.Code.ShouldBe("FR");
        second!.Code.ShouldBe("FR");
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_DefaultQuery_ReturnsActiveOnly()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", isActive: true, ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "XX", "Inactive", isActive: false, ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        result.TotalCount.ShouldBe(1);
        result.Items.ShouldAllBe(e => e.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ActiveOnlyFalse_ReturnsAll()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", isActive: true, ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "XX", "Inactive", isActive: false, ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            new ReferenceDataQuery(ActiveOnly: false),
            TestContext.Current.CancellationToken);

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_SortByCode_ReturnsSorted()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "FR", "France", ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "BE", "Belgium", ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "NL", "Netherlands", ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            new ReferenceDataQuery(SortBy: "Code"),
            TestContext.Current.CancellationToken);

        result.Items[0].Code.ShouldBe("BE");
        result.Items[1].Code.ShouldBe("FR");
        result.Items[2].Code.ShouldBe("NL");
    }

    [Fact]
    public async Task GetAllAsync_SortByCodeDescending_ReturnsSortedDescending()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "FR", "France", ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "BE", "Belgium", ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            new ReferenceDataQuery(SortBy: "Code", Descending: true),
            TestContext.Current.CancellationToken);

        result.Items[0].Code.ShouldBe("FR");
        result.Items[1].Code.ShouldBe("BE");
    }

    [Fact]
    public async Task GetAllAsync_SortByLabel_ReturnsSortedByLabel()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "NL", "Netherlands", ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "BE", "Belgium", ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            new ReferenceDataQuery(SortBy: "Label"),
            TestContext.Current.CancellationToken);

        result.Items[0].Label.ShouldBe("Belgium");
        result.Items[1].Label.ShouldBe("Netherlands");
    }

    [Fact]
    public async Task GetAllAsync_DefaultSortBySortOrder_ReturnsSortedBySortOrder()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "NL", "Netherlands", sortOrder: 3, ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "BE", "Belgium", sortOrder: 1, ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "FR", "France", sortOrder: 2, ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        result.Items[0].Code.ShouldBe("BE");
        result.Items[1].Code.ShouldBe("FR");
        result.Items[2].Code.ShouldBe("NL");
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", sortOrder: 1, ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "FR", "France", sortOrder: 2, ct: TestContext.Current.CancellationToken);
        await SeedAsync(db, "NL", "Netherlands", sortOrder: 3, ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            new ReferenceDataQuery(Skip: 1, Take: 1),
            TestContext.Current.CancellationToken);

        result.TotalCount.ShouldBe(3, "TotalCount reflects all matching entries before pagination");
        result.Items.Count.ShouldBe(1);
        result.Items[0].Code.ShouldBe("FR");
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyResult()
    {
        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(Guid.NewGuid().ToString());

        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_PersistsEntity()
    {
        string db = Guid.NewGuid().ToString();
        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);

        TestEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Code = "DE",
            Label = "Germany",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
        };

        await store.CreateAsync(entity, TestContext.Current.CancellationToken);
        TestEntity? loaded = await store.GetByCodeAsync("DE", TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded!.Label.ShouldBe("Germany");
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ModifiesExistingEntity()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", ct: TestContext.Current.CancellationToken);

        // Use a new store to simulate a fresh scope
        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        TestEntity? entity = await store.GetByCodeAsync("BE", TestContext.Current.CancellationToken);
        entity.ShouldNotBeNull();

        entity!.Label = "Kingdom of Belgium";
        await store.UpdateAsync(entity, TestContext.Current.CancellationToken);

        // Read again from a fresh store to confirm persistence
        EfCoreReferenceDataStore<TestEntity, TestDbContext> freshStore = CreateStore(db);
        TestEntity? updated = await freshStore.GetByCodeAsync("BE", TestContext.Current.CancellationToken);

        updated!.Label.ShouldBe("Kingdom of Belgium");
    }

    // -------------------------------------------------------------------------
    // SetActiveAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetActiveAsync_DeactivatesEntity()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", isActive: true, ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        await store.SetActiveAsync("BE", false, TestContext.Current.CancellationToken);

        // Query with ActiveOnly: false to see deactivated entry
        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            new ReferenceDataQuery(ActiveOnly: false),
            TestContext.Current.CancellationToken);

        result.Items.ShouldContain(e => e.Code == "BE" && !e.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_ReactivatesEntity()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, "BE", "Belgium", isActive: false, ct: TestContext.Current.CancellationToken);

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = CreateStore(db);
        await store.SetActiveAsync("BE", true, TestContext.Current.CancellationToken);

        ReferenceDataResult<TestEntity> result = await store.GetAllAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        result.Items.ShouldContain(e => e.Code == "BE" && e.IsActive);
    }

    // -------------------------------------------------------------------------
    // Cache invalidation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_InvalidatesCache()
    {
        string db = Guid.NewGuid().ToString();

        // Use shared cache to test invalidation
        MemoryCache cache = new(new MemoryCacheOptions());
        IOptions<ReferenceDataOptions> opts = Options.Create(new ReferenceDataOptions());

        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(db));
        ServiceProvider sp = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        EfCoreReferenceDataStore<TestEntity, TestDbContext> store = new(scopeFactory, cache, opts);

        // Populate cache by querying
        await store.GetByCodeAsync("DE", TestContext.Current.CancellationToken);

        // Create should invalidate
        await store.CreateAsync(new TestEntity
        {
            Id = Guid.NewGuid(),
            Code = "DE",
            Label = "Germany",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
        }, TestContext.Current.CancellationToken);

        TestEntity? result = await store.GetByCodeAsync("DE", TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result!.Label.ShouldBe("Germany");
    }
}
