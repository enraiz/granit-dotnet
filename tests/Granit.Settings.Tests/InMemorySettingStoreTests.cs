// =============================================================================
// InMemorySettingStoreTests - Unit tests for the in-memory setting store
// =============================================================================
// Verifies CRUD operations and key isolation by (providerName, providerKey).
// =============================================================================

using Granit.Settings.Stores;
using Granit.Settings.Values;
using Shouldly;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class InMemorySettingStoreTests
{
    private static InMemorySettingStore CreateStore() => new();

    [Fact]
    public async Task GetOrNullAsync_MissingKey_Returns_Null()
    {
        InMemorySettingStore store = CreateStore();

        SettingValue? result = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_Then_GetOrNullAsync_Returns_StoredValue()
    {
        InMemorySettingStore store = CreateStore();

        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);
        SettingValue? result = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("dark");
        result.ProviderName.ShouldBe("G");
        result.ProviderKey.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_Overwrites_ExistingEntry()
    {
        InMemorySettingStore store = CreateStore();

        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);
        await store.SetAsync("App.Theme", "G", null, "light", TestContext.Current.CancellationToken);

        SettingValue? result = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);

        result!.Value.ShouldBe("light");
    }

    [Fact]
    public async Task DeleteAsync_Removes_Entry()
    {
        InMemorySettingStore store = CreateStore();

        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);
        await store.DeleteAsync("App.Theme", "G", null, TestContext.Current.CancellationToken);

        SettingValue? result = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentKey_DoesNotThrow()
    {
        InMemorySettingStore store = CreateStore();

        Func<Task> act = () => store.DeleteAsync(
            "Unknown", "G", null, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task GetOrNullAsync_IsolatedByProviderName()
    {
        InMemorySettingStore store = CreateStore();

        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);

        // Same setting name, different provider — must be null
        SettingValue? result = await store.GetOrNullAsync(
            "App.Theme", "U", null, TestContext.Current.CancellationToken);

        result.ShouldBeNull("provider name is part of the key");
    }

    [Fact]
    public async Task GetOrNullAsync_IsolatedByProviderKey()
    {
        InMemorySettingStore store = CreateStore();

        await store.SetAsync("App.Theme", "T", "tenant-1", "blue", TestContext.Current.CancellationToken);

        // Same setting + provider, different tenant key
        SettingValue? result = await store.GetOrNullAsync(
            "App.Theme", "T", "tenant-2", TestContext.Current.CancellationToken);

        result.ShouldBeNull("provider key is part of the key");
    }

    [Fact]
    public async Task GetListAsync_Returns_AllEntriesForProvider()
    {
        InMemorySettingStore store = CreateStore();

        await store.SetAsync("App.Theme", "G", null, "dark", TestContext.Current.CancellationToken);
        await store.SetAsync("App.Language", "G", null, "fr", TestContext.Current.CancellationToken);
        // Different provider — must not be returned
        await store.SetAsync("App.Theme", "U", "user-1", "light", TestContext.Current.CancellationToken);

        IReadOnlyList<SettingValue> results = await store.GetListAsync(
            "G", null, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(2);
        results.Select(v => v.Name).ShouldContain("App.Theme");
        results.Select(v => v.Name).ShouldContain("App.Language");
    }

    [Fact]
    public async Task GetListAsync_EmptyStore_Returns_EmptyList()
    {
        InMemorySettingStore store = CreateStore();

        IReadOnlyList<SettingValue> results = await store.GetListAsync(
            "G", null, TestContext.Current.CancellationToken);

        results.ShouldBeEmpty();
    }
}
