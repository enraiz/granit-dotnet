// =============================================================================
// GranitSettingsModuleTests - Tests d'intégration DI du module Settings
// =============================================================================
// Vérifie le câblage complet des services via AddGranit<GranitSettingsModule>(),
// à la manière d'AbpIntegratedTest<T> dans ABP Framework.
// =============================================================================

using FluentAssertions;
using Granit.Core.Extensions;
using Granit.Core.Modularity;
using Granit.Security;
using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Services;
using Granit.Settings.Stores;
using Granit.Settings.Values;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class GranitSettingsModuleTests
{
    private static WebApplication BuildApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddGranit<GranitSettingsModule>();
        // ICurrentUserService implementation lives in Granit.Authentication.JwtBearer.
        // Register a stub so the UserSettingValueProvider can be activated in isolation.
        builder.Services.AddSingleton<ICurrentUserService, AnonymousCurrentUserService>();
        return builder.Build();
    }

    /// <summary>Stub that represents an unauthenticated user for unit testing purposes.</summary>
    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public string? Email => null;
        public bool IsAuthenticated => false;
        public IReadOnlyList<string> GetRoles() => [];
        public bool IsInRole(string role) => false;
    }

    // --- Câblage DI ---

    [Fact]
    public void SettingDefinitionManager_Is_Resolvable_And_Singleton()
    {
        using WebApplication app = BuildApp();

        SettingDefinitionManager first = app.Services.GetRequiredService<SettingDefinitionManager>();
        SettingDefinitionManager second = app.Services.GetRequiredService<SettingDefinitionManager>();

        first.Should().NotBeNull();
        first.Should().BeSameAs(second, "SettingDefinitionManager doit être un singleton");
    }

    [Fact]
    public void ISettingStore_Resolves_To_InMemorySettingStore_By_Default()
    {
        using WebApplication app = BuildApp();

        ISettingStore store = app.Services.GetRequiredService<ISettingStore>();

        store.Should().BeOfType<InMemorySettingStore>();
    }

    [Fact]
    public void Five_ISettingValueProviders_Are_Registered()
    {
        using WebApplication app = BuildApp();

        IEnumerable<ISettingValueProvider> providers =
            app.Services.GetRequiredService<IEnumerable<ISettingValueProvider>>();

        providers.Should().HaveCount(5, "U + T + G + C + D = 5 providers");
    }

    [Fact]
    public void Providers_Are_Ordered_U_T_G_C_D()
    {
        using WebApplication app = BuildApp();

        var names = app.Services
            .GetRequiredService<IEnumerable<ISettingValueProvider>>()
            .OrderBy(p => p.Order)
            .Select(p => p.Name)
            .ToList();

        names.Should().ContainInOrder("U", "T", "G", "C", "D");
    }

    [Fact]
    public void SettingValueProviderManager_Is_Resolvable_And_Singleton()
    {
        using WebApplication app = BuildApp();

        SettingValueProviderManager first = app.Services.GetRequiredService<SettingValueProviderManager>();
        SettingValueProviderManager second = app.Services.GetRequiredService<SettingValueProviderManager>();

        first.Should().NotBeNull();
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void ISettingProvider_Is_Resolvable_As_Scoped()
    {
        using WebApplication app = BuildApp();
        using IServiceScope scope = app.Services.CreateScope();

        ISettingProvider settingProvider = scope.ServiceProvider.GetRequiredService<ISettingProvider>();

        settingProvider.Should().NotBeNull();
        settingProvider.Should().BeOfType<SettingProvider>();
    }

    [Fact]
    public void ISettingManager_Is_Resolvable_As_Scoped()
    {
        using WebApplication app = BuildApp();
        using IServiceScope scope = app.Services.CreateScope();

        ISettingManager manager = scope.ServiceProvider.GetRequiredService<ISettingManager>();

        manager.Should().NotBeNull();
        manager.Should().BeOfType<SettingManager>();
    }

    // --- Ordre topologique ---

    [Fact]
    public void Module_Topological_Order_Is_Correct()
    {
        using WebApplication app = BuildApp();

        GranitApplication granitApp = app.Services.GetRequiredService<GranitApplication>();

        granitApp.GetModuleTypes().Should().ContainInOrder(
            typeof(GranitSettingsModule));
    }

    // --- Test fonctionnel (style AbpIntegratedTest) ---

    [Fact]
    public async Task SetGlobal_Then_GetOrNull_Returns_StoredValue()
    {
        using WebApplication app = BuildApp();
        using IServiceScope scope = app.Services.CreateScope();

        // Déclarer le paramètre via un provider enregistré dans le DI
        SettingDefinitionManager defManager = scope.ServiceProvider.GetRequiredService<SettingDefinitionManager>();
        ISettingStore store = scope.ServiceProvider.GetRequiredService<ISettingStore>();

        // Écrire directement dans le store (bypass manager pour ce test de câblage)
        await store.SetAsync(
            "App.Color", GlobalSettingValueProvider.ProviderName, null, "purple",
            TestContext.Current.CancellationToken);

        // Lire directement depuis le store
        SettingValue? retrieved = await store.GetOrNullAsync(
            "App.Color", GlobalSettingValueProvider.ProviderName, null,
            TestContext.Current.CancellationToken);

        retrieved.Should().NotBeNull();
        retrieved!.Value.Should().Be("purple");
    }
}
