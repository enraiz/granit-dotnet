using FluentAssertions;
using Granit.Features.Store;
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

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGet_Returns_StoredValue()
    {
        InMemoryFeatureStore store = new();
        await store.SetAsync("App.Feature", tenantId: null, "true",
            TestContext.Current.CancellationToken);

        string? result = await store.GetOrNullAsync("App.Feature", tenantId: null,
            TestContext.Current.CancellationToken);

        result.Should().Be("true");
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

        globalResult.Should().BeNull("global scope has no value");
        tenantResult.Should().Be("true");
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
        result.Should().BeNull();
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

        result.Should().Be("true");
    }
}
