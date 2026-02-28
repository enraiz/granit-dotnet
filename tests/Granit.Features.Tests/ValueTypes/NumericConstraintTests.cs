using Shouldly;
using Granit.Features.Exceptions;
using Granit.Features.ValueTypes;
using Xunit;

namespace Granit.Features.Tests.ValueTypes;

public sealed class NumericConstraintTests
{
    [Fact]
    public void Validate_ValidIntegerWithinBounds_DoesNotThrow()
    {
        NumericConstraint constraint = new(0, 1000);

        Action act = () => constraint.Validate("App.MaxPatients", "500");

        Should.NotThrow(act);
    }

    [Fact]
    public void Validate_AtMinBound_DoesNotThrow()
    {
        NumericConstraint constraint = new(0, 100);

        Action act = () => constraint.Validate("App.MaxPatients", "0");

        Should.NotThrow(act);
    }

    [Fact]
    public void Validate_AtMaxBound_DoesNotThrow()
    {
        NumericConstraint constraint = new(0, 100);

        Action act = () => constraint.Validate("App.MaxPatients", "100");

        Should.NotThrow(act);
    }

    [Fact]
    public void Validate_NonInteger_ThrowsFeatureValueValidationException()
    {
        NumericConstraint constraint = new(0, 1000);

        Action act = () => constraint.Validate("App.MaxPatients", "not-a-number");

        Should.Throw<FeatureValueValidationException>(act)
           .FeatureName.ShouldBe("App.MaxPatients");
    }

    [Fact]
    public void Validate_BelowMin_ThrowsFeatureValueValidationException()
    {
        NumericConstraint constraint = new(10, 1000);

        Action act = () => constraint.Validate("App.MaxPatients", "5");

        FeatureValueValidationException ex = Should.Throw<FeatureValueValidationException>(act);

        ex.FeatureName.ShouldBe("App.MaxPatients");
        ex.InvalidValue.ShouldBe("5");
        ex.Message.ShouldContain("10");
        ex.Message.ShouldContain("1000");
    }

    [Fact]
    public void Validate_AboveMax_ThrowsFeatureValueValidationException()
    {
        NumericConstraint constraint = new(0, 100);

        Action act = () => constraint.Validate("App.MaxPatients", "999");

        FeatureValueValidationException ex = Should.Throw<FeatureValueValidationException>(act);

        ex.InvalidValue.ShouldBe("999");
    }

    [Fact]
    public void Constraint_StoresMinAndMax()
    {
        NumericConstraint constraint = new(5, 500);

        constraint.Min.ShouldBe(5);
        constraint.Max.ShouldBe(500);
    }
}
