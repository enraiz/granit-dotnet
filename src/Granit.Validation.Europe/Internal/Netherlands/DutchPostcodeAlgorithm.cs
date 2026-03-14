using System.Text.RegularExpressions;

namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Dutch postcodes (postcode).
/// </summary>
/// <remarks>
/// Format: 4 digits (1000–9999) followed by an optional space and 2 uppercase letters.
/// <list type="bullet">
///   <item>The first digit must be 1–9 (no leading zero).</item>
///   <item>The letter combinations SA, SD, and SS are excluded per Dutch postal convention.</item>
/// </list>
/// Example valid values: <c>1234 AB</c>, <c>1234AB</c>.
/// </remarks>
internal static partial class DutchPostcodeAlgorithm
{
    [GeneratedRegex(@"^[1-9]\d{3}\s?[A-Z]{2}$", RegexOptions.None, 100)]
    private static partial Regex PostcodeRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Dutch postcode.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim().ToUpperInvariant();

        if (!PostcodeRegex().IsMatch(trimmed))
        {
            return false;
        }

        // Extract the 2-letter suffix (last 2 characters).
        string letters = trimmed[^2..];

        return !letters.Equals("SA", StringComparison.Ordinal) &&
               !letters.Equals("SD", StringComparison.Ordinal) &&
               !letters.Equals("SS", StringComparison.Ordinal);
    }
}
