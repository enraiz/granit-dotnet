using FluentAssertions;
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

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_AtMinBound_DoesNotThrow()
    {
        NumericConstraint constraint = new(0, 100);

        Action act = () => constraint.Validate("App.MaxPatients", "0");

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_AtMaxBound_DoesNotThrow()
    {
        NumericConstraint constraint = new(0, 100);

        Action act = () => constraint.Validate("App.MaxPatients", "100");

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NonInteger_ThrowsFeatureValueValidationException()
    {
        NumericConstraint constraint = new(0, 1000);

        Action act = () => constraint.Validate("App.MaxPatients", "not-a-number");

        act.Should().Throw<FeatureValueValidationException>()
           .Which.FeatureName.Should().Be("App.MaxPatients");
    }

    [Fact]
    public void Validate_BelowMin_ThrowsFeatureValueValidationException()
    {
        NumericConstraint constraint = new(10, 1000);

        Action act = () => constraint.Validate("App.MaxPatients", "5");

        FeatureValueValidationException ex = act.Should()
            .Throw<FeatureValueValidationException>().Which;

        ex.FeatureName.Should().Be("App.MaxPatients");
        ex.InvalidValue.Should().Be("5");
        ex.Message.Should().Contain("10").And.Contain("1000");
    }

    [Fact]
    public void Validate_AboveMax_ThrowsFeatureValueValidationException()
    {
        NumericConstraint constraint = new(0, 100);

        Action act = () => constraint.Validate("App.MaxPatients", "999");

        FeatureValueValidationException ex = act.Should()
            .Throw<FeatureValueValidationException>().Which;

        ex.InvalidValue.Should().Be("999");
    }

    [Fact]
    public void Constraint_StoresMinAndMax()
    {
        NumericConstraint constraint = new(5, 500);

        constraint.Min.Should().Be(5);
        constraint.Max.Should().Be(500);
    }
}
