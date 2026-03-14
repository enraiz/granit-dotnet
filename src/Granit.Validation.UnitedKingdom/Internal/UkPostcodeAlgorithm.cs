using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates United Kingdom postcodes.
/// </summary>
/// <remarks>
/// UK postcodes consist of an outward code and an inward code separated by a space.
/// Valid formats: A9 9AA, A99 9AA, A9A 9AA, AA9 9AA, AA99 9AA, AA9A 9AA.
/// The inward code is always 9AA (digit + 2 letters). Letters C, I, K, M, O, V are not
/// used in the inward code final two positions.
/// Space between outward and inward codes is optional.
/// </remarks>
internal static partial class UkPostcodeAlgorithm
{
    // UK postcode pattern (space optional between outward and inward).
    // Outward: A9, A99, A9A, AA9, AA99, AA9A
    // Inward: 9AA (with restricted letters)
    [GeneratedRegex(
        @"^([A-Z]{1,2}\d[A-Z\d]?)\s?(\d[ABD-HJLNP-UW-Z]{2})$",
        RegexOptions.IgnoreCase, 100)]
    private static partial Regex PostcodeRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid UK postcode.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return PostcodeRegex().IsMatch(value.Trim());
    }
}
