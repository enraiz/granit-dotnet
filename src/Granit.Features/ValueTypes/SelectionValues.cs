using Granit.Features.Exceptions;

namespace Granit.Features.ValueTypes;

/// <summary>
/// Defines the allowed string values for a <see cref="FeatureValueType.Selection"/> feature.
/// </summary>
/// <param name="AllowedValues">The exhaustive list of accepted values.</param>
public sealed record SelectionValues(IReadOnlyList<string> AllowedValues)
{
    /// <summary>
    /// Validates that <paramref name="rawValue"/> is one of the allowed values.
    /// </summary>
    /// <param name="featureName">Feature name for error messages.</param>
    /// <param name="rawValue">The string value to validate.</param>
    /// <exception cref="FeatureValueValidationException">
    /// Thrown when the value is not in <see cref="AllowedValues"/>.
    /// </exception>
    public void Validate(string featureName, string rawValue)
    {
        if (!AllowedValues.Contains(rawValue, StringComparer.Ordinal))
        {
            throw new FeatureValueValidationException(
                featureName,
                rawValue,
                $"value must be one of [{string.Join(", ", AllowedValues)}].");
        }
    }
}
