using System.Text.RegularExpressions;

namespace Granit.Validation.NorthAmerica.Internal.UnitedStates;

/// <summary>
/// Validates United States Social Security Numbers (SSN).
/// </summary>
/// <remarks>
/// Format: AAA-GG-SSSS (9 digits, with or without dashes).
/// Invalid area numbers: 000, 666, 900–999.
/// Invalid group or serial: all zeros.
/// Reference: SSA publication No. 05-10633.
/// </remarks>
internal static partial class SsnAlgorithm
{
    // 9 digits with optional dashes: AAA-GG-SSSS or AAAGGSSSS.
    [GeneratedRegex(@"^(\d{3})-?(\d{2})-?(\d{4})$", RegexOptions.None, 100)]
    private static partial Regex SsnRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid SSN.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        Match match = SsnRegex().Match(normalized);

        if (!match.Success)
        {
            return false;
        }

        string area = match.Groups[1].Value;
        string group = match.Groups[2].Value;
        string serial = match.Groups[3].Value;

        // Area number cannot be 000, 666, or 900-999.
        if (area is "000" or "666" || area[0] == '9')
        {
            return false;
        }

        // Group number cannot be 00.
        if (group is "00")
        {
            return false;
        }

        // Serial number cannot be 0000.
        if (serial is "0000")
        {
            return false;
        }

        return true;
    }
}
