using FluentAssertions;
using Granit.Features.Definitions;
using Granit.Features.ValueProviders;
using Granit.Features.ValueTypes;
using Xunit;

namespace Granit.Features.Tests.ValueProviders;

public sealed class DefaultValueFeatureValueProviderTests
{
    private static FeatureDefinition MakeDefinition(string defaultValue = "false") =>
        new("App.Feature", defaultValue, FeatureValueType.Toggle);

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    [Fact]
    public void Name_Is_Default() =>
        new DefaultValueFeatureValueProvider().Name.Should().Be("Default");

    [Fact]
    public void Order_Is_300() =>
        new DefaultValueFeatureValueProvider().Order.Should().Be(300);

    // -------------------------------------------------------------------------
    // GetOrNullAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrNullAsync_ReturnsDefaultValue()
    {
        DefaultValueFeatureValueProvider provider = new();

        string? result = await provider.GetOrNullAsync(
            MakeDefinition("true"), TestContext.Current.CancellationToken);

        result.Should().Be("true");
    }

    [Fact]
    public async Task GetOrNullAsync_NeverReturnsNull()
    {
        DefaultValueFeatureValueProvider provider = new();

        string? result = await provider.GetOrNullAsync(
            MakeDefinition("some-value"), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Should().Be("some-value");
    }

    [Fact]
    public async Task GetOrNullAsync_NumericDefault_ReturnsStringValue()
    {
        FeatureDefinition definition = new("App.MaxPatients", "100", FeatureValueType.Numeric);
        DefaultValueFeatureValueProvider provider = new();

        string? result = await provider.GetOrNullAsync(
            definition, TestContext.Current.CancellationToken);

        result.Should().Be("100");
    }
}
