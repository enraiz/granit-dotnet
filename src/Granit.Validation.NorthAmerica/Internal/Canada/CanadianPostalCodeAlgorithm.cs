using System.Text.RegularExpressions;

namespace Granit.Validation.NorthAmerica.Internal.Canada;

/// <summary>
/// Validates Canadian postal codes.
/// </summary>
/// <remarks>
/// Format: A1A 1A1 (letter-digit-letter space digit-letter-digit).
/// Letters D, F, I, O, Q, U are never used. W and Z are not used as the first letter.
/// The space between the two halves is optional.
/// </remarks>
internal static partial class CanadianPostalCodeAlgorithm
{
    // Canadian postal code: A1A 1A1 (space optional).
    // First letter: A-C, E, G-H, J-N, P, R-T, V-Y (excludes D, F, I, O, Q, U, W, Z).
    // Other letters: A-C, E, G-H, J-N, P, R-T, V-Z (excludes D, F, I, O, Q, U).
    [GeneratedRegex(
        @"^[ABCEGHJKLMNPRSTVXY]\d[ABCEGHJKLMNPRSTVWXYZ]\s?\d[ABCEGHJKLMNPRSTVWXYZ]\d$",
        RegexOptions.IgnoreCase, 100)]
    private static partial Regex PostalCodeRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Canadian postal code.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return PostalCodeRegex().IsMatch(value.Trim());
    }
}
