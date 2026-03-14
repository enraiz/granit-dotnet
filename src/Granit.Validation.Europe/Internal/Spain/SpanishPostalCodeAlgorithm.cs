using System.Text.RegularExpressions;

namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Spanish postal codes (código postal).
/// </summary>
/// <remarks>
/// Format: exactly 5 digits representing provinces 01–52.
/// The first two digits identify the province (01–52), followed by 3 digits for the delivery zone.
/// </remarks>
internal static partial class SpanishPostalCodeAlgorithm
{
    [GeneratedRegex(@"^(0[1-9]|[1-4]\d|5[0-2])\d{3}$", RegexOptions.None, 100)]
    private static partial Regex CodigoPostalRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Spanish postal code.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return CodigoPostalRegex().IsMatch(value.Trim());
    }
}
