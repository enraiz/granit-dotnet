// =============================================================================
// Tests - GranitValidator<T>
// =============================================================================
// Verifies:
//   - ClassLevelCascadeMode is set to CascadeMode.Continue
//   - All errors are returned in a single validation pass (no fail-fast)
//   - Inherits from AbstractValidator<T>
// =============================================================================

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Granit.Validation;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class GranitValidatorTests
{
    // -------------------------------------------------------------------------
    // CascadeMode
    // -------------------------------------------------------------------------

    [Fact]
    public void ClassLevelCascadeMode_IsContinue()
    {
        TestCommandValidator validator = new();

        validator.ClassLevelCascadeMode.Should().Be(CascadeMode.Continue);
    }

    [Fact]
    public void Validate_MultipleRulesFail_ReturnsAllErrors()
    {
        TestCommandValidator validator = new();
        TestCommand command = new(Name: "", Age: -1);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TestCommand.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TestCommand.Age));
    }

    [Fact]
    public void Validate_AllRulesPass_ReturnsValid()
    {
        TestCommandValidator validator = new();
        TestCommand command = new(Name: "Alice", Age: 30);

        ValidationResult result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Inheritance
    // -------------------------------------------------------------------------

    [Fact]
    public void IsAssignableFrom_AbstractValidator()
    {
        TestCommandValidator validator = new();

        validator.Should().BeAssignableTo<AbstractValidator<TestCommand>>();
    }

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    private sealed record TestCommand(string Name, int Age);

    private sealed class TestCommandValidator : GranitValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }
}
