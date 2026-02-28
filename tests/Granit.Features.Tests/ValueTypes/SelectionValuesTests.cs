using Shouldly;
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

        Should.NotThrow(act);
    }

    [Fact]
    public void Validate_AllAllowedValues_DoNotThrow()
    {
        foreach (string value in Tiers.AllowedValues)
        {
            Action act = () => Tiers.Validate("App.Plan", value);
            Should.NotThrow(act);
        }
    }

    [Fact]
    public void Validate_UnknownValue_ThrowsFeatureValueValidationException()
    {
        Action act = () => Tiers.Validate("App.Plan", "ultimate");

        FeatureValueValidationException ex = Should.Throw<FeatureValueValidationException>(act);

        ex.FeatureName.ShouldBe("App.Plan");
        ex.InvalidValue.ShouldBe("ultimate");
        ex.Message.ShouldContain("starter");
        ex.Message.ShouldContain("enterprise");
    }

    [Fact]
    public void Validate_IsCaseSensitive()
    {
        // Ordinal comparison — "Starter" ≠ "starter"
        Action act = () => Tiers.Validate("App.Plan", "Starter");

        Should.Throw<FeatureValueValidationException>(act);
    }

    [Fact]
    public void AllowedValues_ArePreserved()
    {
        Tiers.AllowedValues.ShouldBe(new[] { "starter", "professional", "enterprise" });
    }
}
