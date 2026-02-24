using FluentValidation;
using Granit.Validation.Internal;

namespace Granit.Validation;

/// <summary>
/// FluentValidation extension methods for payment and financial identifiers.
/// </summary>
public static class PaymentValidatorExtensions
{
    /// <summary>
    /// Validates an International Bank Account Number (IBAN) per ISO 13616.
    /// </summary>
    /// <remarks>
    /// Accepts IBANs with or without spaces (e.g. <c>BE68 5390 0754 7034</c>).
    /// Validates the MOD-97 check digits. Supports all country formats (15–34 characters).
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> Iban<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(IbanAlgorithm.IsValid)
            .WithErrorCodeAndMessage("Granit:Validation:InvalidIban");

    /// <summary>
    /// Validates a BIC/SWIFT code per ISO 9362.
    /// </summary>
    /// <remarks>
    /// Accepts 8-character (primary office) and 11-character (branch) codes.
    /// Format: 4 alpha bank code + 2 alpha country + 2 alphanumeric location + 3 alphanumeric branch (optional).
    /// Case-insensitive.
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> BicSwift<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(BicSwiftAlgorithm.IsValid)
            .WithErrorCodeAndMessage("Granit:Validation:InvalidBicSwift");

    /// <summary>
    /// Validates a SEPA Creditor Identifier (SCI) per EPC262-08.
    /// </summary>
    /// <remarks>
    /// Format: 2-alpha country + 2-digit check + 3 alphanumeric creditor business code + national ID.
    /// Validated using ISO 7064 MOD 97-10 (same algorithm as IBAN).
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> SepaCreditorIdentifier<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(SepaCreditorIdentifierAlgorithm.IsValid)
            .WithErrorCodeAndMessage("Granit:Validation:InvalidSepaCreditorIdentifier");
}
