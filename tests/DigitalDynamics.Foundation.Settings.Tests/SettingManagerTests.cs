// =============================================================================
// SettingManagerTests - Tests d'écriture et d'invalidation de cache
// =============================================================================
// Vérifie que SettingManager écrit dans le store et invalide le cache pour
// les portées Global, Tenant et User.
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Services;
using DigitalDynamics.Foundation.Settings.Stores;
using DigitalDynamics.Foundation.Settings.Values;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Settings.Tests;

public sealed class SettingManagerTests
{
    private static SettingDefinitionManager ManagerWith(params SettingDefinition[] defs) =>
        new([new FakeDefinitionProvider(defs)]);

    private sealed class FakeDefinitionProvider(SettingDefinition[] definitions) : ISettingDefinitionProvider
    {
        public void Define(ISettingDefinitionContext context)
        {
            foreach (SettingDefinition def in definitions)
            {
                context.Add(def);
            }
        }
    }

    private static (SettingManager manager, InMemorySettingStore store, ICacheService<SettingValue> cache)
        CreateManager(params SettingDefinition[] defs)
    {
        InMemorySettingStore store = new();
        ICacheService<SettingValue> cache = Substitute.For<ICacheService<SettingValue>>();
        SettingDefinitionManager defManager = ManagerWith(defs);
        SettingManager manager = new(store, cache, defManager);
        return (manager, store, cache);
    }

    // -------------------------------------------------------------------------
    // SetGlobalAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetGlobalAsync_Writes_ToStore_WithProviderName_G()
    {
        SettingDefinition def = new("App.Theme");
        (SettingManager manager, InMemorySettingStore store, _) = CreateManager(def);

        await manager.SetGlobalAsync("App.Theme", "light", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.Value.Should().Be("light");
    }

    [Fact]
    public async Task SetGlobalAsync_Invalidates_Cache()
    {
        SettingDefinition def = new("App.Theme");
        (SettingManager manager, _, ICacheService<SettingValue> cache) = CreateManager(def);

        await manager.SetGlobalAsync("App.Theme", "light", TestContext.Current.CancellationToken);

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('G') && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetGlobalAsync_UnknownSetting_Throws()
    {
        (SettingManager manager, _, _) = CreateManager();

        Func<Task> act = () => manager.SetGlobalAsync("Unknown.Setting", "value");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unknown.Setting*");
    }

    // -------------------------------------------------------------------------
    // SetForTenantAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetForTenantAsync_Writes_ToStore_WithProviderName_T_And_TenantKey()
    {
        SettingDefinition def = new("App.Theme");
        (SettingManager manager, InMemorySettingStore store, _) = CreateManager(def);
        Guid tenantId = Guid.NewGuid();

        await manager.SetForTenantAsync(tenantId, "App.Theme", "blue", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "T", tenantId.ToString(), TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.Value.Should().Be("blue");
    }

    [Fact]
    public async Task SetForTenantAsync_Invalidates_Cache_WithTenantKey()
    {
        SettingDefinition def = new("App.Theme");
        (SettingManager manager, _, ICacheService<SettingValue> cache) = CreateManager(def);
        Guid tenantId = Guid.NewGuid();

        await manager.SetForTenantAsync(tenantId, "App.Theme", "blue", TestContext.Current.CancellationToken);

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('T') && k.Contains(tenantId.ToString()) && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // SetForUserAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetForUserAsync_Writes_ToStore_WithProviderName_U_And_UserId()
    {
        SettingDefinition def = new("App.Theme");
        (SettingManager manager, InMemorySettingStore store, _) = CreateManager(def);

        await manager.SetForUserAsync("user-42", "App.Theme", "red", TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "U", "user-42", TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.Value.Should().Be("red");
    }

    [Fact]
    public async Task SetForUserAsync_EmptyUserId_Throws()
    {
        (SettingManager manager, _, _) = CreateManager(new SettingDefinition("App.Theme"));

        Func<Task> act = () => manager.SetForUserAsync("", "App.Theme", "value");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_Removes_FromStore_And_Invalidates_Cache()
    {
        SettingDefinition def = new("App.Theme");
        (SettingManager manager, InMemorySettingStore store, ICacheService<SettingValue> cache) = CreateManager(def);

        // Préalable : écrire une valeur
        await store.SetAsync("App.Theme", "G", null, "light", TestContext.Current.CancellationToken);

        await manager.DeleteAsync("App.Theme", "G", null, TestContext.Current.CancellationToken);

        SettingValue? stored = await store.GetOrNullAsync(
            "App.Theme", "G", null, TestContext.Current.CancellationToken);
        stored.Should().BeNull("la suppression doit retirer l'entrée du store");

        await cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains('G') && k.Contains("App.Theme")),
            Arg.Any<CancellationToken>());
    }
}
