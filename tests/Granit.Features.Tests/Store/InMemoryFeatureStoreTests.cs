using Granit.Features.Internal;
using Shouldly;
using Xunit;

namespace Granit.Features.Tests.Store;

public sealed class InMemoryFeatureStoreTests
{
    [Fact]
    public async Task GetOrNullAsync_NoEntry_Returns_Null()
    {
        InMemoryFeatureStore store = new();

        string? result = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGet_Returns_StoredValue()
    {
        InMemoryFeatureStore store = new();
        await store.SetAsync("App.Feature", tenantId: null, "true",
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);

        result.ShouldBe("true");
    }

    [Fact]
    public async Task TenantScope_IsIsolated_From_GlobalScope()
    {
        InMemoryFeatureStore store = new();
        string tenantId = Guid.NewGuid().ToString();
        await store.SetAsync("App.Feature", tenantId, "true", TestContext.Current.CancellationToken);

        string? globalResult = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);
        string? tenantResult = await store.GetOrNullAsync("App.Feature", tenantId,
            TestContext.Current.CancellationToken);

        globalResult.ShouldBeNull("global scope has no value");
        tenantResult.ShouldBe("true");
    }

    [Fact]
    public async Task DeleteAsync_Removes_Entry()
    {
        InMemoryFeatureStore store = new();
        await store.SetAsync("App.Feature", tenantId: null, "true",
            TestContext.Current.CancellationToken);

        await store.DeleteAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_Overwrites_ExistingValue()
    {
        InMemoryFeatureStore store = new();
        await store.SetAsync("App.Feature", tenantId: null, "false",
            TestContext.Current.CancellationToken);
        await store.SetAsync("App.Feature", tenantId: null, "true",
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);

        result.ShouldBe("true");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentEntry_DoesNotThrow()
    {
        InMemoryFeatureStore store = new();

        Func<Task> act = () => store.DeleteAsync("NonExistent.Feature", tenantId: null,
            TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task DeleteAsync_TenantScoped_DoesNotAffect_GlobalScope()
    {
        InMemoryFeatureStore store = new();
        string tenantId = Guid.NewGuid().ToString();
        await store.SetAsync("App.Feature", tenantId: null, "global-value",
            TestContext.Current.CancellationToken);
        await store.SetAsync("App.Feature", tenantId, "tenant-value",
            TestContext.Current.CancellationToken);

        await store.DeleteAsync("App.Feature", tenantId,
            TestContext.Current.CancellationToken);

        string? globalResult = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);
        string? tenantResult = await store.GetOrNullAsync("App.Feature", tenantId,
            TestContext.Current.CancellationToken);

        globalResult.ShouldBe("global-value", "global scope should not be affected");
        tenantResult.ShouldBeNull("tenant scope should be deleted");
    }

    [Fact]
    public async Task DifferentFeatureNames_AreIsolated()
    {
        InMemoryFeatureStore store = new();
        await store.SetAsync("App.FeatureA", tenantId: null, "valueA",
            TestContext.Current.CancellationToken);
        await store.SetAsync("App.FeatureB", tenantId: null, "valueB",
            TestContext.Current.CancellationToken);

        string? resultA = await store.GetOrNullAsync("App.FeatureA", tenantId: null,
            TestContext.Current.CancellationToken);
        string? resultB = await store.GetOrNullAsync("App.FeatureB", tenantId: null,
            TestContext.Current.CancellationToken);

        resultA.ShouldBe("valueA");
        resultB.ShouldBe("valueB");
    }

    [Fact]
    public async Task MultipleTenants_AreIsolated()
    {
        InMemoryFeatureStore store = new();
        string tenantA = Guid.NewGuid().ToString();
        string tenantB = Guid.NewGuid().ToString();

        await store.SetAsync("App.Feature", tenantA, "A-value",
            TestContext.Current.CancellationToken);
        await store.SetAsync("App.Feature", tenantB, "B-value",
            TestContext.Current.CancellationToken);

        string? resultA = await store.GetOrNullAsync("App.Feature", tenantA,
            TestContext.Current.CancellationToken);
        string? resultB = await store.GetOrNullAsync("App.Feature", tenantB,
            TestContext.Current.CancellationToken);

        resultA.ShouldBe("A-value");
        resultB.ShouldBe("B-value");
    }
}
