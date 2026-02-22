// =============================================================================
// DefaultValueSettingValueProviderTests - Unit tests for the Default provider
// =============================================================================
// Verifies that the provider returns SettingDefinition.DefaultValue
// and returns null when no default is set.
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Values;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Settings.Tests;

public sealed class DefaultValueSettingValueProviderTests
{
    private static DefaultValueSettingValueProvider CreateProvider() => new();

    [Fact]
    public void Name_Is_D() =>
        CreateProvider().Name.Should().Be("D");

    [Fact]
    public void Order_Is_500() =>
        CreateProvider().Order.Should().Be(500);

    [Fact]
    public async Task GetOrNullAsync_WithDefaultValue_Returns_SettingValue()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Name.Should().Be("App.Theme");
        result.ProviderName.Should().Be("D");
        result.ProviderKey.Should().BeNull();
        result.Value.Should().Be("dark");
    }

    [Fact]
    public async Task GetOrNullAsync_WithoutDefaultValue_Returns_Null()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme"); // DefaultValue = null

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_IsNoOp()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };

        Func<Task> act = () => provider.SetAsync(def, "light", TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        // The DefaultValue is immutable — verify nothing changed
        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);
        result!.Value.Should().Be("dark", "SetAsync is a no-op for the Default provider");
    }

    [Fact]
    public async Task ClearAsync_IsNoOp()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };

        Func<Task> act = () => provider.ClearAsync(def, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
