using System.Text.RegularExpressions;
using Granit.Validation.Internal;

namespace Granit.Validation.NorthAmerica.Internal.Canada;

/// <summary>
/// Validates Canadian Social Insurance Numbers (SIN).
/// </summary>
/// <remarks>
/// Format: 9 digits (with or without dashes/spaces: XXX-XXX-XXX or XXX XXX XXX).
/// Validated using the Luhn algorithm.
/// First digit indicates the type: 1–7 = regular, 9 = temporary resident.
/// 0 and 8 are not assigned as first digits.
/// </remarks>
internal static partial class SinAlgorithm
{
    [GeneratedRegex(@"[\s\-]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Canadian SIN.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = SeparatorRegex().Replace(value.Trim(), string.Empty);

        if (normalized.Length != 9 || !normalized.All(char.IsDigit))
        {
            return false;
        }

        // First digit cannot be 0 or 8.
        char first = normalized[0];
        if (first is '0' or '8')
        {
            return false;
        }

        return LuhnAlgorithm.IsValid(normalized);
    }
}
