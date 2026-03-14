using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates United Kingdom VAT registration numbers.
/// </summary>
/// <remarks>
/// Format: "GB" prefix followed by either 9 or 12 digits.
/// <list type="bullet">
///   <item>9 digits: standard registration. First 7 digits form the number, last 2 are check digits.</item>
///   <item>12 digits: branch trader — same as 9-digit with a 3-digit branch code suffix (001–999).</item>
/// </list>
/// Check digit algorithm: multiply digits 1–7 by weights 8–2, sum, then the number is valid if
/// (sum + check digits) MOD 97 == 0, or (sum + 55 + check digits) MOD 97 == 0 (for numbers ≥ 100M).
/// GD (government) and HA (health authority) prefixed numbers are also accepted.
/// </remarks>
internal static partial class UkVatAlgorithm
{
    [GeneratedRegex(@"[\s\-\.]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    // Standard: GB + 9 or 12 digits.
    [GeneratedRegex(@"^GB(\d{9}|\d{12})$", RegexOptions.None, 100)]
    private static partial Regex StandardRegex();

    // Government departments: GD + 3 digits (000-499).
    [GeneratedRegex(@"^GD[0-4]\d{2}$", RegexOptions.None, 100)]
    private static partial Regex GovernmentRegex();

    // Health authorities: HA + 3 digits (500-999).
    [GeneratedRegex(@"^HA[5-9]\d{2}$", RegexOptions.None, 100)]
    private static partial Regex HealthRegex();

    private static readonly int[] Weights = [8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid UK VAT number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = SeparatorRegex()
            .Replace(value.Trim(), string.Empty)
            .ToUpperInvariant();

        // Government department or health authority — format-only check.
        if (GovernmentRegex().IsMatch(normalized) || HealthRegex().IsMatch(normalized))
        {
            return true;
        }

        if (!StandardRegex().IsMatch(normalized))
        {
            return false;
        }

        // Extract the 9-digit core (skip "GB" prefix).
        string digits = normalized[2..11];

        // Numbers starting with 0 are not issued.
        if (digits[0] == '0')
        {
            return false;
        }

        int sum = 0;

        for (int i = 0; i < 7; i++)
        {
            sum += (digits[i] - '0') * Weights[i];
        }

        int checkValue = int.Parse(digits[7..9], System.Globalization.CultureInfo.InvariantCulture);

        // Old scheme (numbers < 100,000,000): total = sum + check, valid if total MOD 97 == 0.
        // New scheme (numbers ≥ 100,000,000): total = sum + 55 + check, valid if total MOD 97 == 0.
        return (sum + checkValue) % 97 == 0 || (sum + 55 + checkValue) % 97 == 0;
    }
}
