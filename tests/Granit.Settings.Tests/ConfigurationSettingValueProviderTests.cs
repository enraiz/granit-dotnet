// =============================================================================
// ConfigurationSettingValueProviderTests - Unit tests for the Configuration provider
// =============================================================================
// Verifies that the provider reads from the "Settings:{name}" section
// and that Set/Clear are no-ops.
// =============================================================================

using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Values;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class ConfigurationSettingValueProviderTests
{
    private static ConfigurationSettingValueProvider CreateProvider(
        Dictionary<string, string?> inMemoryData)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryData)
            .Build();
        return new ConfigurationSettingValueProvider(config);
    }

    [Fact]
    public void Name_Is_C() =>
        CreateProvider([]).Name.ShouldBe("C");

    [Fact]
    public void Order_Is_400() =>
        CreateProvider([]).Order.ShouldBe(400);

    [Fact]
    public async Task GetOrNullAsync_KeyExists_Returns_SettingValue()
    {
        ConfigurationSettingValueProvider provider = CreateProvider(
            new Dictionary<string, string?> { ["Settings:App.Theme"] = "dark" });
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe("dark");
        result.ProviderName.ShouldBe("C");
        result.ProviderKey.ShouldBeNull();
    }

    [Fact]
    public async Task GetOrNullAsync_KeyMissing_Returns_Null()
    {
        ConfigurationSettingValueProvider provider = CreateProvider([]);
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_IsNoOp_ConfigurationNotMutated()
    {
        ConfigurationSettingValueProvider provider = CreateProvider(
            new Dictionary<string, string?> { ["Settings:App.Theme"] = "dark" });
        SettingDefinition def = new("App.Theme");

        await provider.SetAsync(def, "light", TestContext.Current.CancellationToken);

        // IConfiguration is read-only — value must remain unchanged
        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);
        result!.Value.ShouldBe("dark", "SetAsync is a no-op for the Configuration provider");
    }

    [Fact]
    public async Task ClearAsync_IsNoOp()
    {
        ConfigurationSettingValueProvider provider = CreateProvider([]);
        SettingDefinition def = new("App.Theme");

        Func<Task> act = () => provider.ClearAsync(def, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task GetOrNullAsync_UsesColonSeparatedKey()
    {
        // Keys like "App.Feature.Enabled" map to "Settings:App.Feature.Enabled"
        ConfigurationSettingValueProvider provider = CreateProvider(
            new Dictionary<string, string?> { ["Settings:App.Feature.Enabled"] = "true" });
        SettingDefinition def = new("App.Feature.Enabled");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result!.Value.ShouldBe("true");
    }
}
