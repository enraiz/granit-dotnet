// =============================================================================
// ConfigurationSettingValueProviderTests - Unit tests for the Configuration provider
// =============================================================================
// Verifies that the provider reads from the "Settings:{name}" section
// and that Set/Clear are no-ops.
// =============================================================================

using FluentAssertions;
using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Values;
using Microsoft.Extensions.Configuration;
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
        CreateProvider([]).Name.Should().Be("C");

    [Fact]
    public void Order_Is_400() =>
        CreateProvider([]).Order.Should().Be(400);

    [Fact]
    public async Task GetOrNullAsync_KeyExists_Returns_SettingValue()
    {
        ConfigurationSettingValueProvider provider = CreateProvider(
            new Dictionary<string, string?> { ["Settings:App.Theme"] = "dark" });
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Value.Should().Be("dark");
        result.ProviderName.Should().Be("C");
        result.ProviderKey.Should().BeNull();
    }

    [Fact]
    public async Task GetOrNullAsync_KeyMissing_Returns_Null()
    {
        ConfigurationSettingValueProvider provider = CreateProvider([]);
        SettingDefinition def = new("App.Theme");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull();
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
        result!.Value.Should().Be("dark", "SetAsync is a no-op for the Configuration provider");
    }

    [Fact]
    public async Task ClearAsync_IsNoOp()
    {
        ConfigurationSettingValueProvider provider = CreateProvider([]);
        SettingDefinition def = new("App.Theme");

        Func<Task> act = () => provider.ClearAsync(def, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetOrNullAsync_UsesColonSeparatedKey()
    {
        // Keys like "App.Feature.Enabled" map to "Settings:App.Feature.Enabled"
        ConfigurationSettingValueProvider provider = CreateProvider(
            new Dictionary<string, string?> { ["Settings:App.Feature.Enabled"] = "true" });
        SettingDefinition def = new("App.Feature.Enabled");

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result!.Value.Should().Be("true");
    }
}
