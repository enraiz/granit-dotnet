using System.Text.RegularExpressions;
using FluentValidation;

namespace Granit.Validation;

/// <summary>
/// FluentValidation extension methods for contact and communication identifiers.
/// </summary>
public static class ContactValidatorExtensions
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{6,14}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Validates a phone number in E.164 international format.
    /// </summary>
    /// <remarks>
    /// Expected format: <c>+</c> followed by 7 to 15 digits (e.g. <c>+32475123456</c>).
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> E164Phone<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(value => value != null && E164Regex.IsMatch(value))
            .WithErrorCodeAndMessage("Granit:Validation:InvalidE164Phone");
}
