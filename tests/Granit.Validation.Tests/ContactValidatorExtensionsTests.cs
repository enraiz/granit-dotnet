// =============================================================================
// Tests - ContactValidatorExtensions
// =============================================================================
// Email: RFC-compliant via FluentValidation .EmailAddress()
// =============================================================================

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class ContactValidatorExtensionsTests
{
    // =========================================================================
    // Email
    // =========================================================================

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+label@sub.domain.be")]
    [InlineData("admin@granit.io")]
    public void Email_ValidValues_PassValidation(string email)
    {
        InlineValidator<TestModel> validator = new();
        validator.RuleFor(x => x.Value).Email();

        ValidationResult result = validator.Validate(new TestModel(email));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("notanemail")]              // no @ sign
    [InlineData("missing@")]               // no domain
    [InlineData("@nodomain.com")]          // no local part
    [InlineData("spaces in@email.com")]    // space in local part
    public void Email_InvalidValues_FailValidation(string? email)
    {
        InlineValidator<TestModel> validator = new();
        validator.RuleFor(x => x.Value).Email();

        ValidationResult result = validator.Validate(new TestModel(email));

        result.IsValid.Should().BeFalse();
        result.Errors[0].ErrorMessage.Should().Be("Granit:Validation:InvalidEmail");
        result.Errors[0].ErrorCode.Should().Be("Granit:Validation:InvalidEmail");
    }

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    private sealed record TestModel(string? Value);
}
