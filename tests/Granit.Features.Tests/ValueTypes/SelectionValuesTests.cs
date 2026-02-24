using FluentAssertions;
using Granit.Features.Exceptions;
using Granit.Features.ValueTypes;
using Xunit;

namespace Granit.Features.Tests.ValueTypes;

public sealed class SelectionValuesTests
{
    private static readonly SelectionValues Tiers =
        new(["starter", "professional", "enterprise"]);

    [Fact]
    public void Validate_AllowedValue_DoesNotThrow()
    {
        Action act = () => Tiers.Validate("App.Plan", "professional");

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_AllAllowedValues_DoNotThrow()
    {
        foreach (string value in Tiers.AllowedValues)
        {
            Action act = () => Tiers.Validate("App.Plan", value);
            act.Should().NotThrow(because: $"'{value}' is in the allowed list");
        }
    }

    [Fact]
    public void Validate_UnknownValue_ThrowsFeatureValueValidationException()
    {
        Action act = () => Tiers.Validate("App.Plan", "ultimate");

        FeatureValueValidationException ex = act.Should()
            .Throw<FeatureValueValidationException>().Which;

        ex.FeatureName.Should().Be("App.Plan");
        ex.InvalidValue.Should().Be("ultimate");
        ex.Message.Should().Contain("starter").And.Contain("enterprise");
    }

    [Fact]
    public void Validate_IsCaseSensitive()
    {
        // Ordinal comparison — "Starter" ≠ "starter"
        Action act = () => Tiers.Validate("App.Plan", "Starter");

        act.Should().Throw<FeatureValueValidationException>();
    }

    [Fact]
    public void AllowedValues_ArePreserved()
    {
        Tiers.AllowedValues.Should().BeEquivalentTo(
            ["starter", "professional", "enterprise"],
            opts => opts.WithStrictOrdering());
    }
}
