// =============================================================================
// Tests - PaymentValidatorExtensions
// =============================================================================
// BicSwift:              8 or 11 alphanumeric chars, ISO 9362 format
// SepaCreditorIdentifier: CC + 2 check + 3 CBA + national ID, ISO 7064 MOD 97-10
// =============================================================================

using FluentValidation;
using FluentValidation.Results;
using Shouldly;
using Xunit;

namespace Granit.Validation.Tests;

public sealed class PaymentValidatorExtensionsTests
{
    // =========================================================================
    // BicSwift
    // =========================================================================

    [Theory]
    [InlineData("GEBABEBB")]               // 8-char BIC — BNP Paribas Fortis
    [InlineData("BNPAFRPP")]               // 8-char BIC — BNP Paribas France
    [InlineData("GEBABEBB36A")]            // 11-char BIC with branch code
    [InlineData("gebabebb")]               // lowercase — normalised to uppercase
    public void BicSwift_ValidValues_PassValidation(string bic)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).BicSwift();

        ValidationResult result = validator.Validate(new TestModel(bic));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("GEBA")]                   // 4 chars — too short
    [InlineData("GEBABE")]                 // 6 chars — invalid length
    [InlineData("GEBA1BBB")]              // digit in country code position (pos 5)
    [InlineData("12345678")]              // starts with digits — invalid bank code
    [InlineData("GEBABEBB36AB")]          // 12 chars — too long
    public void BicSwift_InvalidValues_FailValidation(string? bic)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).BicSwift();

        ValidationResult result = validator.Validate(new TestModel(bic));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidBicSwift");
        result.Errors[0].ErrorCode.ShouldBe("Granit:Validation:InvalidBicSwift");
    }

    // =========================================================================
    // SepaCreditorIdentifier
    // =========================================================================
    // Check validated via ISO 7064 MOD 97-10 (same as IBAN).
    // Valid test values computed from algorithm:
    //   "BE46ZZZ000000000": rearranged=ZZZ000000000BE46 → 353535000000000111446 mod 97=1 ✓
    //   "FR20ZZZ123456":    rearranged=ZZZ123456FR20    → 353535123456152720   mod 97=1 ✓

    [Theory]
    [InlineData("BE46ZZZ000000000")]       // Belgian SCI
    [InlineData("FR20ZZZ123456")]          // French SCI
    public void SepaCreditorIdentifier_ValidValues_PassValidation(string sci)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).SepaCreditorIdentifier();

        ValidationResult result = validator.Validate(new TestModel(sci));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BE47ZZZ000000000")]       // wrong check (46 → 47), mod97 ≠ 1
    [InlineData("BE92")]                   // too short (4 chars < 8 minimum)
    [InlineData("1234ZZZ000000000")]       // country code not alpha
    [InlineData("BEAAZZZ000000000")]       // check not digits
    public void SepaCreditorIdentifier_InvalidValues_FailValidation(string? sci)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).SepaCreditorIdentifier();

        ValidationResult result = validator.Validate(new TestModel(sci));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidSepaCreditorIdentifier");
        result.Errors[0].ErrorCode.ShouldBe("Granit:Validation:InvalidSepaCreditorIdentifier");
    }

    // =========================================================================
    // FrenchRib
    // =========================================================================
    // Key formula: clé = 97 − (89 × banque + 15 × guichet + 3 × compte) mod 97
    //   "30004000010000000404529": bank=30004, branch=00001, account=00000004045
    //     → (89×30004 + 15×1 + 3×4045) mod 97 = 2682506 mod 97 = 68, key = 29 ✓
    //   "00000000000000000000097": all-zero account → mod 97 = 0, key = 97 ✓

    [Theory]
    [InlineData("30004000010000000404529")]            // numeric RIB
    [InlineData("30004 00001 00000004045 29")]         // formatted with spaces
    [InlineData("00000000000000000000097")]            // all-zero account, key = 97
    public void FrenchRib_ValidValues_PassValidation(string rib)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchRib();

        ValidationResult result = validator.Validate(new TestModel(rib));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3000400001000000040452")]             // 22 chars — too short
    [InlineData("300040000100000004045290")]           // 24 chars — too long
    [InlineData("30004000010000000404530")]            // wrong key (29 → 30)
    public void FrenchRib_InvalidValues_FailValidation(string? rib)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).FrenchRib();

        ValidationResult result = validator.Validate(new TestModel(rib));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidFrenchRib");
        result.Errors[0].ErrorCode.ShouldBe("Granit:Validation:InvalidFrenchRib");
    }

    // =========================================================================
    // BelgianAccountNumber
    // =========================================================================
    // Key formula: key = (first 10 digits) mod 97, or 97 when remainder is 0.
    //   "123456789002": base=1234567890, mod 97 = 2, key = 02 ✓
    //   "000000000097": base=0, mod 97 = 0, key = 97 ✓

    [Theory]
    [InlineData("123456789002")]            // mod 97 = 2, key = 02
    [InlineData("000000000097")]            // mod 97 = 0, key = 97
    [InlineData("123-4567890-02")]          // formatted with dashes
    public void BelgianAccountNumber_ValidValues_PassValidation(string account)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).BelgianAccountNumber();

        ValidationResult result = validator.Validate(new TestModel(account));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678900")]             // 11 digits — too short
    [InlineData("1234567890020")]           // 13 digits — too long
    [InlineData("123456789003")]            // wrong key (02 → 03)
    public void BelgianAccountNumber_InvalidValues_FailValidation(string? account)
    {
        InlineValidator<TestModel> validator = [];
        validator.RuleFor(x => x.Value).BelgianAccountNumber();

        ValidationResult result = validator.Validate(new TestModel(account));

        result.IsValid.ShouldBeFalse();
        result.Errors[0].ErrorMessage.ShouldBe("Granit:Validation:InvalidBelgianAccountNumber");
        result.Errors[0].ErrorCode.ShouldBe("Granit:Validation:InvalidBelgianAccountNumber");
    }

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    private sealed record TestModel(string? Value);
}
