using FluentValidation;

namespace Granit.Validation.Extensions;

/// <summary>
/// Extension methods on <see cref="IRuleBuilderOptions{T,TProperty}"/> for Granit conventions.
/// </summary>
public static class RuleBuilderExtensions
{
    /// <summary>
    /// Sets <see cref="IRuleBuilderOptions{T,TProperty}.WithErrorCode"/> and
    /// <see cref="IRuleBuilderOptions{T,TProperty}.WithMessage"/> to the same value.
    /// </summary>
    /// <remarks>
    /// In Granit validators the error code is also the message key so that the Wolverine HTTP
    /// middleware serializes it in <c>ValidationProblemDetails.errors</c>.
    /// Using this method prevents the two values from silently diverging.
    /// </remarks>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder options to configure.</param>
    /// <param name="code">The <c>Granit:Validation:*</c> error code (also used as message key).</param>
    /// <returns>The same rule builder options for fluent chaining.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithErrorCodeAndMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, string code) =>
        rule.WithErrorCode(code).WithMessage(code);
}
