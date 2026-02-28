// =============================================================================
// SettingProviderTests - Tests de la résolution en cascade des paramètres
// =============================================================================
// Vérifie la cascade U → T → G → C → D ainsi que les règles IsInherited
// et la liste blanche Providers.
// =============================================================================

using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Services;
using Granit.Settings.Values;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class SettingProviderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SettingDefinitionManager ManagerWith(params SettingDefinition[] defs) =>
        new([new FakeDefinitionProvider(defs)]);

    private static ISettingValueProvider MockProvider(string name, int order, SettingValue? returnValue)
    {
        ISettingValueProvider provider = Substitute.For<ISettingValueProvider>();
        provider.Name.Returns(name);
        provider.Order.Returns(order);
        provider.GetOrNullAsync(Arg.Any<SettingDefinition>(), Arg.Any<CancellationToken>())
            .Returns(returnValue);
        return provider;
    }

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

    // -------------------------------------------------------------------------
    // Résolution de base
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UnknownSetting_Returns_Null()
    {
        SettingDefinitionManager defManager = ManagerWith();
        SettingProvider provider = new(new SettingValueProviderManager([]), defManager);

        string? result = await provider.GetOrNullAsync("Unknown.Setting", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FirstNonNull_Provider_Wins_InCascade()
    {
        SettingDefinition def = new("App.Theme");
        SettingDefinitionManager defManager = ManagerWith(def);

        ISettingValueProvider userProvider = MockProvider("U", 100, null);
        ISettingValueProvider tenantProvider = MockProvider("T", 200,
            new SettingValue(def.Name, "T", "tenant-1", "blue"));
        ISettingValueProvider globalProvider = MockProvider("G", 300,
            new SettingValue(def.Name, "G", null, "green"));

        SettingValueProviderManager providerManager = new([userProvider, tenantProvider, globalProvider]);
        SettingProvider settingProvider = new(providerManager, defManager);

        string? result = await settingProvider.GetOrNullAsync(def.Name, TestContext.Current.CancellationToken);

        result.ShouldBe("blue", "le provider Tenant (T) doit l'emporter sur Global");
    }

    [Fact]
    public async Task AllProviders_ReturnNull_Returns_Null()
    {
        SettingDefinition def = new("App.Theme");
        SettingDefinitionManager defManager = ManagerWith(def);

        ISettingValueProvider p1 = MockProvider("G", 300, null);
        ISettingValueProvider p2 = MockProvider("D", 500, null);

        SettingValueProviderManager providerManager = new([p1, p2]);
        SettingProvider settingProvider = new(providerManager, defManager);

        string? result = await settingProvider.GetOrNullAsync(def.Name, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DefaultProvider_IsReturned_WhenOthersNull()
    {
        SettingDefinition def = new("App.Theme");
        SettingDefinitionManager defManager = ManagerWith(def);

        ISettingValueProvider userProvider = MockProvider("U", 100, null);
        ISettingValueProvider defaultProvider = MockProvider("D", 500,
            new SettingValue(def.Name, "D", null, "dark"));

        SettingValueProviderManager providerManager = new([userProvider, defaultProvider]);
        SettingProvider settingProvider = new(providerManager, defManager);

        string? result = await settingProvider.GetOrNullAsync(def.Name, TestContext.Current.CancellationToken);

        result.ShouldBe("dark");
    }

    // -------------------------------------------------------------------------
    // IsInherited = false
    // -------------------------------------------------------------------------

    [Fact]
    public async Task IsInherited_False_StopsCascade_WhenProviderReturnsNull()
    {
        SettingDefinition def = new("App.Feature") { IsInherited = false };
        SettingDefinitionManager defManager = ManagerWith(def);

        ISettingValueProvider userProvider = MockProvider("U", 100, null);
        ISettingValueProvider globalProvider = MockProvider("G", 300,
            new SettingValue(def.Name, "G", null, "enabled"));

        SettingValueProviderManager providerManager = new([userProvider, globalProvider]);
        SettingProvider settingProvider = new(providerManager, defManager);

        string? result = await settingProvider.GetOrNullAsync(def.Name, TestContext.Current.CancellationToken);

        result.ShouldBeNull("la cascade doit s'arrêter après U quand IsInherited=false");
    }

    // -------------------------------------------------------------------------
    // Liste blanche Providers
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AllowList_Restricts_ProvidersConsulted()
    {
        SettingDefinition def = new("App.GlobalOnly");
        def.Providers.Add("G");

        SettingDefinitionManager defManager = ManagerWith(def);

        ISettingValueProvider userProvider = MockProvider("U", 100,
            new SettingValue(def.Name, "U", "user-1", "user-value"));
        ISettingValueProvider globalProvider = MockProvider("G", 300,
            new SettingValue(def.Name, "G", null, "global-value"));

        SettingValueProviderManager providerManager = new([userProvider, globalProvider]);
        SettingProvider settingProvider = new(providerManager, defManager);

        string? result = await settingProvider.GetOrNullAsync(def.Name, TestContext.Current.CancellationToken);

        result.ShouldBe("global-value", "seul le provider G est autorisé");
        await userProvider.DidNotReceive().GetOrNullAsync(Arg.Any<SettingDefinition>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_Returns_OneEntry_PerName()
    {
        SettingDefinition def1 = new("App.Theme");
        SettingDefinition def2 = new("App.Language");
        SettingDefinitionManager defManager = ManagerWith(def1, def2);

        ISettingValueProvider globalProvider = Substitute.For<ISettingValueProvider>();
        globalProvider.Name.Returns("G");
        globalProvider.Order.Returns(300);
        globalProvider.GetOrNullAsync(def1, Arg.Any<CancellationToken>())
            .Returns(new SettingValue(def1.Name, "G", null, "dark"));
        globalProvider.GetOrNullAsync(def2, Arg.Any<CancellationToken>())
            .Returns(new SettingValue(def2.Name, "G", null, "fr"));

        SettingValueProviderManager providerManager = new([globalProvider]);
        SettingProvider settingProvider = new(providerManager, defManager);

        IReadOnlyList<SettingValue> results = await settingProvider.GetAllAsync(
            [def1.Name, def2.Name], TestContext.Current.CancellationToken);

        results.Count.ShouldBe(2);
        results.First(v => v.Name == def1.Name).Value.ShouldBe("dark");
        results.First(v => v.Name == def2.Name).Value.ShouldBe("fr");
    }
}
