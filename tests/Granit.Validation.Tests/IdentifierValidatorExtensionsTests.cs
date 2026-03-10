// =============================================================================
// Tests - HealthValidatorExtensions
// =============================================================================
// Verifies each health validator with real valid/invalid values.
// Error codes follow the convention Granit:Validation:* (WithMessage = WithErrorCode).
// =============================================================================

using FluentValidation;
using FluentValidation.Results;
using Granit.Validation.Extensions;
using Shouldly;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class IdentifierValidatorExtensionsTests
{
    // =========================================================================
    // BelgianNiss
    // =========================================================================

    [Theory]
    [InlineData("85073003328")]         // born 1985, male
    [InlineData("85.07.30-033.28")]     // formatted
    [InlineData("00010100105")]         // born 2000 (prefix-2 case)
    [InlineData("01012500182")]         // born 2001
    public void BelgianNiss_ValidValues_PassValidation(string niss)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).BelgianNiss();

        ValidationResult result = validator.Validate(new TestModel(niss));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567890")]          // 10 digits
    [InlineData("123456789012")]        // 12 digits
    [InlineData("85073003300")]         // wrong check digit
    [InlineData("abcdefghijk")]         // not digits
    public void BelgianNiss_InvalidValues_FailValidation(string? niss)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).BelgianNiss();

        ValidationResult result = validator.Validate(new TestModel(niss));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidBelgianNiss");
        result.Errors[0].ErrorCode.ShouldBe("Granit:Validation:InvalidBelgianNiss");
    }

    // =========================================================================
    // FrenchRpps
    // =========================================================================

    [Theory]
    [InlineData("10003456786")]         // valid Luhn 11-digit RPPS
    [InlineData("10000000009")]         // minimal valid
    public void FrenchRpps_ValidValues_PassValidation(string rpps)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchRpps();

        ValidationResult result = validator.Validate(new TestModel(rpps));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1000345678")]          // 10 digits
    [InlineData("100034567890")]        // 12 digits
    [InlineData("10003456788")]         // wrong Luhn check
    [InlineData("1000345678A")]         // non-digit character
    public void FrenchRpps_InvalidValues_FailValidation(string? rpps)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchRpps();

        ValidationResult result = validator.Validate(new TestModel(rpps));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidFrenchRpps");
    }

    // =========================================================================
    // E164Phone
    // =========================================================================

    [Theory]
    [InlineData("+32475123456")]        // Belgian mobile
    [InlineData("+33612345678")]        // French mobile
    [InlineData("+14155552671")]        // US
    [InlineData("+442071234567")]       // UK
    public void E164Phone_ValidValues_PassValidation(string phone)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).E164Phone();

        ValidationResult result = validator.Validate(new TestModel(phone));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0475123456")]          // missing +
    [InlineData("+32")]                 // too short
    [InlineData("+3247512345678901")]   // too long (>15 digits)
    [InlineData("+0475123456")]         // leading 0 after +
    [InlineData("+32 475 12 34 56")]    // spaces not allowed
    public void E164Phone_InvalidValues_FailValidation(string? phone)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).E164Phone();

        ValidationResult result = validator.Validate(new TestModel(phone));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidE164Phone");
    }

    // =========================================================================
    // FrenchAdeli
    // =========================================================================

    [Theory]
    [InlineData("123456789")]
    [InlineData("000000001")]
    public void FrenchAdeli_ValidValues_PassValidation(string adeli)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchAdeli();

        ValidationResult result = validator.Validate(new TestModel(adeli));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678")]            // 8 digits
    [InlineData("1234567890")]          // 10 digits
    [InlineData("12345678A")]           // non-digit
    public void FrenchAdeli_InvalidValues_FailValidation(string? adeli)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchAdeli();

        ValidationResult result = validator.Validate(new TestModel(adeli));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidFrenchAdeli");
    }

    // =========================================================================
    // Iban
    // =========================================================================

    [Theory]
    [InlineData("BE68539007547034")]             // Belgian IBAN
    [InlineData("BE68 5390 0754 7034")]          // with spaces
    [InlineData("FR7630006000011234567890189")]  // French IBAN
    [InlineData("DE89370400440532013000")]       // German IBAN
    [InlineData("GB29NWBK60161331926819")]       // UK IBAN
    public void Iban_ValidValues_PassValidation(string iban)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).Iban();

        ValidationResult result = validator.Validate(new TestModel(iban));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BE68539007547035")]         // wrong check digit
    [InlineData("XX00123456789012345")]      // invalid country code (passes format, fails MOD-97)
    [InlineData("BE685390075470")]           // too short
    [InlineData("123456789")]               // no country code
    public void Iban_InvalidValues_FailValidation(string? iban)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).Iban();

        ValidationResult result = validator.Validate(new TestModel(iban));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidIban");
    }

    // =========================================================================
    // FrenchFiness
    // =========================================================================

    [Theory]
    [InlineData("750100018")]           // valid Luhn 9-digit Finess
    public void FrenchFiness_ValidValues_PassValidation(string finess)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchFiness();

        ValidationResult result = validator.Validate(new TestModel(finess));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("75010001")]            // 8 digits
    [InlineData("7501000180")]          // 10 digits
    [InlineData("750100019")]           // wrong Luhn check
    [InlineData("75010001A")]           // non-digit
    public void FrenchFiness_InvalidValues_FailValidation(string? finess)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchFiness();

        ValidationResult result = validator.Validate(new TestModel(finess));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidFrenchFiness");
    }

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    private sealed record TestModel(string? Value);
}
