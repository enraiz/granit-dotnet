using Granit.Core.MultiTenancy;
using Granit.DataExchange.EntityFrameworkCore.Internal;
using Granit.DataExchange.EntityFrameworkCore.Internal.Export.Stores;
using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Entities;
using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Stores;
using Granit.DataExchange.EntityFrameworkCore.Tests.Infrastructure;
using Granit.DataExchange.Import.Mapping;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.EntityFrameworkCore.Tests.Stores;

public sealed class EfMappingStoreTests
{
    private static string NewDb() => Guid.NewGuid().ToString();

    private static IClock CreateClock(DateTimeOffset? now = null)
    {
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(now ?? new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        return clock;
    }

    private static ICurrentTenant CreateTenant(Guid? id = null)
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(id.HasValue);
        tenant.Id.Returns(id);
        return tenant;
    }

    private static EfMappingStore CreateStore(string dbName, IClock? clock = null, ICurrentTenant? tenant = null) =>
        new(
            new InMemoryDataExchangeContextFactory(dbName),
            clock ?? CreateClock(),
            tenant ?? CreateTenant());

    // ---- LoadAsync --------------------------------------------------------

    [Fact]
    public async Task LoadAsync_returns_empty_when_no_mappings_saved()
    {
        // Arrange
        EfMappingStore store = CreateStore(NewDb());

        // Act
        IReadOnlyList<ColumnMapping> result = await store.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_returns_saved_mappings()
    {
        // Arrange
        string dbName = NewDb();
        EfMappingStore store = CreateStore(dbName);
        List<ColumnMapping> mappings =
        [
            new("Nom", "Name", MappingConfidence.Exact),
            new("Courriel", "Email", MappingConfidence.Fuzzy),
        ];
        await store.SaveAsync("Test.Import", mappings, TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<ColumnMapping> result = await store.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[0].SourceColumn.ShouldBe("Nom");
        result[0].TargetProperty.ShouldBe("Name");
        result[1].SourceColumn.ShouldBe("Courriel");
    }

    [Fact]
    public async Task LoadAsync_filters_by_definition_name()
    {
        // Arrange
        string dbName = NewDb();
        EfMappingStore store = CreateStore(dbName);
        List<ColumnMapping> mappings = [new("Col1", "Prop1", MappingConfidence.Exact)];
        await store.SaveAsync("Def.A", mappings, TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<ColumnMapping> result = await store.LoadAsync(
            "Def.B", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    // ---- SaveAsync --------------------------------------------------------

    [Fact]
    public async Task SaveAsync_creates_new_entry()
    {
        // Arrange
        string dbName = NewDb();
        EfMappingStore store = CreateStore(dbName);
        List<ColumnMapping> mappings = [new("Col1", "Prop1", MappingConfidence.Saved)];

        // Act
        await store.SaveAsync("Test.Import", mappings, TestContext.Current.CancellationToken);

        // Assert — verify directly in the database
        await using DataExchangeDbContext context = new InMemoryDataExchangeContextFactory(dbName).CreateDbContext();
        int count = await context.SavedMappings.CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveAsync_upserts_existing_entry()
    {
        // Arrange
        string dbName = NewDb();
        EfMappingStore store = CreateStore(dbName);
        List<ColumnMapping> first = [new("Col1", "Prop1", MappingConfidence.Exact)];
        List<ColumnMapping> second = [new("Col2", "Prop2", MappingConfidence.Fuzzy)];

        // Act
        await store.SaveAsync("Test.Import", first, TestContext.Current.CancellationToken);
        await store.SaveAsync("Test.Import", second, TestContext.Current.CancellationToken);

        // Assert
        IReadOnlyList<ColumnMapping> result = await store.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);
        result.Count.ShouldBe(1);
        result[0].SourceColumn.ShouldBe("Col2");
    }

    // ---- Multi-tenancy ---------------------------------------------------

    [Fact]
    public async Task SaveAsync_and_LoadAsync_respect_tenant_isolation()
    {
        // Arrange
        string dbName = NewDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        EfMappingStore storeA = CreateStore(dbName, tenant: CreateTenant(tenantA));
        EfMappingStore storeB = CreateStore(dbName, tenant: CreateTenant(tenantB));

        List<ColumnMapping> mappingsA = [new("ColA", "PropA", MappingConfidence.Saved)];
        List<ColumnMapping> mappingsB = [new("ColB", "PropB", MappingConfidence.Saved)];
        await storeA.SaveAsync("Test.Import", mappingsA, TestContext.Current.CancellationToken);
        await storeB.SaveAsync("Test.Import", mappingsB, TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<ColumnMapping> resultA = await storeA.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);
        IReadOnlyList<ColumnMapping> resultB = await storeB.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);

        // Assert
        resultA.Count.ShouldBe(1);
        resultA[0].SourceColumn.ShouldBe("ColA");
        resultB.Count.ShouldBe(1);
        resultB[0].SourceColumn.ShouldBe("ColB");
    }

    [Fact]
    public async Task LoadAsync_without_tenant_returns_null_tenant_mappings()
    {
        // Arrange
        string dbName = NewDb();
        EfMappingStore storeWithoutTenant = CreateStore(dbName, tenant: CreateTenant());
        EfMappingStore storeWithTenant = CreateStore(dbName, tenant: CreateTenant(Guid.NewGuid()));

        List<ColumnMapping> mappings = [new("Col1", "Prop1", MappingConfidence.Exact)];
        await storeWithoutTenant.SaveAsync("Test.Import", mappings, TestContext.Current.CancellationToken);

        // Act
        IReadOnlyList<ColumnMapping> resultNoTenant = await storeWithoutTenant.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);
        IReadOnlyList<ColumnMapping> resultWithTenant = await storeWithTenant.LoadAsync(
            "Test.Import", TestContext.Current.CancellationToken);

        // Assert
        resultNoTenant.Count.ShouldBe(1);
        resultWithTenant.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_sets_timestamp_from_clock()
    {
        // Arrange
        string dbName = NewDb();
        DateTimeOffset fixedNow = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        EfMappingStore store = CreateStore(dbName, clock: CreateClock(fixedNow));
        List<ColumnMapping> mappings = [new("Col1", "Prop1", MappingConfidence.Exact)];

        // Act
        await store.SaveAsync("Test.Import", mappings, TestContext.Current.CancellationToken);

        // Assert
        await using DataExchangeDbContext context = new InMemoryDataExchangeContextFactory(dbName).CreateDbContext();
        Internal.Import.Entities.SavedMappingEntity? entity = await context.SavedMappings
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        entity.ShouldNotBeNull();
        entity.SavedAt.ShouldBe(fixedNow);
    }
}
