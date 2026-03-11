using FluentValidation;
using Granit.Validation.Internal;


namespace Granit.Validation.Extensions;

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

    /// <summary>
    /// Validates a French RIB (Relevé d'Identité Bancaire) bank account number.
    /// </summary>
    /// <remarks>
    /// A RIB is 23 characters: 5-digit bank code + 5-digit branch code + 11-character account number
    /// + 2-digit check key. The account number may contain letters A–Z.
    /// Key formula: <c>clé = 97 − (89 × banque + 15 × guichet + 3 × compte) mod 97</c> (or 97 when 0).
    /// Spaces and dashes are stripped before validation.
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> FrenchRib<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(FrenchRibAlgorithm.IsValid)
            .WithErrorCodeAndMessage("Granit:Validation:InvalidFrenchRib");

    /// <summary>
    /// Validates a Belgian bank account number in the legacy pre-IBAN format.
    /// </summary>
    /// <remarks>
    /// Format: <c>NNN-NNNNNNN-NN</c> (3-digit bank code + 7-digit account + 2-digit check pair).
    /// The check pair equals <c>(first 10 digits) mod 97</c>, or 97 when the remainder is zero.
    /// Dashes and spaces are stripped before validation.
    /// <para>
    /// This format pre-dates IBAN. Belgian banks display IBAN on all statements since 2014.
    /// Prefer <see cref="Iban"/> for new integrations.
    /// </para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> BelgianAccountNumber<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(BelgianAccountNumberAlgorithm.IsValid)
            .WithErrorCodeAndMessage("Granit:Validation:InvalidBelgianAccountNumber");
}
