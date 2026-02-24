using System.Text.RegularExpressions;
using FluentValidation;
using Granit.Validation.Internal;

namespace Granit.Validation;

/// <summary>
/// FluentValidation extension methods for structured and regulated identifiers.
/// </summary>
/// <remarks>
/// Each extension emits both <c>WithErrorCode</c> and <c>WithMessage</c> set to the
/// same <c>Granit:Validation:*</c> code, so the code is serialized in
/// <c>ValidationProblemDetails.errors</c> by the Wolverine HTTP middleware.
/// </remarks>
public static class IdentifierValidatorExtensions
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{6,14}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Validates a Belgian National Identification Number (NISS / INSZ / SSIN).
    /// </summary>
    /// <remarks>
    /// Accepts both formatted (<c>85.07.30-033.28</c>) and unformatted (<c>85073003328</c>) forms.
    /// Validates the 97-modulo check digit for persons born before and from 2000.
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> BelgianNiss<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(NissAlgorithm.IsValid)
            .WithErrorCode("Granit:Validation:InvalidBelgianNiss")
            .WithMessage("Granit:Validation:InvalidBelgianNiss");

    /// <summary>
    /// Validates a French RPPS number (Répertoire Partagé des Professionnels de Santé).
    /// </summary>
    /// <remarks>
    /// Expects exactly 11 digits with a valid Luhn check digit.
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> FrenchRpps<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(RppsAlgorithm.IsValid)
            .WithErrorCode("Granit:Validation:InvalidFrenchRpps")
            .WithMessage("Granit:Validation:InvalidFrenchRpps");

    /// <summary>
    /// Validates a phone number in E.164 international format.
    /// </summary>
    /// <remarks>
    /// Expected format: <c>+</c> followed by 7 to 15 digits (e.g. <c>+32475123456</c>).
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> E164Phone<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => value != null && E164Regex.IsMatch(value))
            .WithErrorCode("Granit:Validation:InvalidE164Phone")
            .WithMessage("Granit:Validation:InvalidE164Phone");

    /// <summary>
    /// Validates a French ADELI number (Automatisation DEs LIstes).
    /// </summary>
    /// <remarks>
    /// Expects exactly 9 digits.
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> FrenchAdeli<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(AdeliAlgorithm.IsValid)
            .WithErrorCode("Granit:Validation:InvalidFrenchAdeli")
            .WithMessage("Granit:Validation:InvalidFrenchAdeli");

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
            .WithErrorCode("Granit:Validation:InvalidIban")
            .WithMessage("Granit:Validation:InvalidIban");

    /// <summary>
    /// Validates a French Finess number (Fichier National des Établissements Sanitaires et Sociaux).
    /// </summary>
    /// <remarks>
    /// Expects 9 digits with a valid Luhn check digit.
    /// Identifies French health establishments (hospitals, clinics, pharmacies).
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> FrenchFiness<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(FinesAlgorithm.IsValid)
            .WithErrorCode("Granit:Validation:InvalidFrenchFiness")
            .WithMessage("Granit:Validation:InvalidFrenchFiness");
}
