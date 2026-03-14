using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Granit.Validation.NorthAmerica.Internal.UnitedStates;

/// <summary>
/// Validates United States Employer Identification Numbers (EIN).
/// </summary>
/// <remarks>
/// Format: XX-XXXXXXX (9 digits, with or without dash).
/// The first two digits (campus prefix) must be a valid IRS campus code.
/// Reference: IRS Publication 1635.
/// </remarks>
internal static partial class EinAlgorithm
{
    // 9 digits with optional dash: XX-XXXXXXX or XXXXXXXXX.
    [GeneratedRegex(@"^(\d{2})-?(\d{7})$", RegexOptions.None, 100)]
    private static partial Regex EinRegex();

    // Valid IRS campus prefixes (2-digit codes assigned to processing centers).
    private static readonly FrozenSet<string> ValidPrefixes = new[]
    {
        // Brookhaven
        "01", "02", "03", "04", "05", "06", "11", "13", "14", "16",
        // Andover
        "10", "12",
        // Atlanta
        "60", "67",
        // Austin
        "50", "53",
        // Cincinnati
        "30", "32", "35", "36", "37", "38", "61",
        // Fresno
        "15", "24",
        // Kansas City
        "40", "44",
        // Memphis
        "94", "95",
        // Ogden
        "80", "90",
        // Philadelphia
        "33", "39", "41", "42", "43", "46", "48", "62", "63", "64", "66", "68",
        "71", "72", "73", "74", "75", "76", "77",
        // Internet / IVES
        "20", "26", "27", "45", "46", "47",
        // Small Business/Self-Employed
        "81", "82", "83", "84", "85", "86", "87", "88", "91", "92", "93",
        "98", "99",
    }.ToFrozenSet();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid EIN.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        Match match = EinRegex().Match(normalized);

        if (!match.Success)
        {
            return false;
        }

        string prefix = match.Groups[1].Value;

        return ValidPrefixes.Contains(prefix);
    }
}
