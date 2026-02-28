// =============================================================================
// DefaultValueSettingValueProviderTests - Unit tests for the Default provider
// =============================================================================
// Verifies that the provider returns SettingDefinition.DefaultValue
// and returns null when no default is set.
// =============================================================================

using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Values;
using Shouldly;
using Xunit;

namespace Granit.Settings.Tests;

public sealed class DefaultValueSettingValueProviderTests
{
    private static DefaultValueSettingValueProvider CreateProvider() => new();

    [Fact]
    public void Name_Is_D() =>
        CreateProvider().Name.ShouldBe("D");

    [Fact]
    public void Order_Is_500() =>
        CreateProvider().Order.ShouldBe(500);

    [Fact]
    public async Task GetOrNullAsync_WithDefaultValue_Returns_SettingValue()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("App.Theme");
        result.ProviderName.ShouldBe("D");
        result.ProviderKey.ShouldBeNull();
        result.Value.ShouldBe("dark");
    }

    [Fact]
    public async Task GetOrNullAsync_WithoutDefaultValue_Returns_Null()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme"); // DefaultValue = null

        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_IsNoOp()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };

        Func<Task> act = () => provider.SetAsync(def, "light", TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
        // The DefaultValue is immutable — verify nothing changed
        SettingValue? result = await provider.GetOrNullAsync(def, TestContext.Current.CancellationToken);
        result!.Value.ShouldBe("dark", "SetAsync is a no-op for the Default provider");
    }

    [Fact]
    public async Task ClearAsync_IsNoOp()
    {
        DefaultValueSettingValueProvider provider = CreateProvider();
        SettingDefinition def = new("App.Theme") { DefaultValue = "dark" };

        Func<Task> act = () => provider.ClearAsync(def, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }
}
