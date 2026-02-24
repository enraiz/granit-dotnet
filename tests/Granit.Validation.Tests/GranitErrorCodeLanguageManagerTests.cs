// =============================================================================
// Tests - GranitErrorCodeLanguageManager
// =============================================================================
// Verifies:
//   - Returns "Granit:Validation:{key}" for any key
//   - Culture parameter has no effect on the returned code
//   - Enabled is true by default
//   - Culture is InvariantCulture by default
// =============================================================================

using System.Globalization;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Granit.Validation.Extensions;
using Granit.Validation.Internal;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class GranitErrorCodeLanguageManagerTests
{
    // -------------------------------------------------------------------------
    // GetString
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("NotEmptyValidator", "Granit:Validation:NotEmptyValidator")]
    [InlineData("MaximumLengthValidator", "Granit:Validation:MaximumLengthValidator")]
    [InlineData("EmailValidator", "Granit:Validation:EmailValidator")]
    [InlineData("CustomKey", "Granit:Validation:CustomKey")]
    public void GetString_ReturnsGranitValidationCode(string key, string expected)
    {
        GranitErrorCodeLanguageManager manager = new();

        string result = manager.GetString(key);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetString_CultureParameterHasNoEffect()
    {
        GranitErrorCodeLanguageManager manager = new();

        string french = manager.GetString("NotEmptyValidator", new CultureInfo("fr"));
        string dutch = manager.GetString("NotEmptyValidator", new CultureInfo("nl"));
        string english = manager.GetString("NotEmptyValidator", new CultureInfo("en"));

        french.Should().Be(dutch).And.Be(english);
    }

    // -------------------------------------------------------------------------
    // Default state
    // -------------------------------------------------------------------------

    [Fact]
    public void Enabled_IsTrueByDefault()
    {
        GranitErrorCodeLanguageManager manager = new();

        manager.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Culture_IsInvariantCultureByDefault()
    {
        GranitErrorCodeLanguageManager manager = new();

        manager.Culture.Should().Be(CultureInfo.InvariantCulture);
    }

    // -------------------------------------------------------------------------
    // Integration — AddGranitValidation sets the global language manager
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitValidation_SetsGranitErrorCodeLanguageManager()
    {
        Microsoft.Extensions.DependencyInjection.ServiceCollection services = new();

        services.AddGranitValidation();

        ValidatorOptions.Global.LanguageManager.Should().BeOfType<GranitErrorCodeLanguageManager>();
    }

    [Fact]
    public void AddGranitValidation_BuiltInRule_EmitsErrorCode()
    {
        Microsoft.Extensions.DependencyInjection.ServiceCollection services = new();
        services.AddGranitValidation();

        InlineValidator<string> validator = new();
        validator.RuleFor(x => x).NotEmpty();

        ValidationResult result = validator.Validate(string.Empty);

        result.Errors[0].ErrorMessage.Should().Be("Granit:Validation:NotEmptyValidator");
    }
}
