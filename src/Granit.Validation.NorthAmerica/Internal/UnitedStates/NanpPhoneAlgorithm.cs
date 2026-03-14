using System.Text.RegularExpressions;

namespace Granit.Validation.NorthAmerica.Internal.UnitedStates;

/// <summary>
/// Validates North American Numbering Plan (NANP) phone numbers.
/// </summary>
/// <remarks>
/// Format: NXX-NXX-XXXX where N is 2–9 and X is 0–9.
/// Accepts with or without dashes, parentheses, spaces, dots, and optional +1 country code.
/// Rejects area codes starting with 0 or 1, and exchange codes starting with 0 or 1.
/// Covers US, Canada, and Caribbean NANP countries.
/// </remarks>
internal static partial class NanpPhoneAlgorithm
{
    // Strips all formatting to get 10 or 11 (with leading 1) digits.
    [GeneratedRegex(@"[\s\-\.\(\)]+", RegexOptions.None, 100)]
    private static partial Regex FormattingRegex();

    // 10-digit NANP: NXX NXX XXXX (N = 2-9).
    [GeneratedRegex(@"^[2-9]\d{2}[2-9]\d{6}$", RegexOptions.None, 100)]
    private static partial Regex NanpRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid NANP phone number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Strip formatting characters.
        string normalized = FormattingRegex().Replace(value.Trim(), string.Empty);

        // Remove optional +1 or 1 country code prefix.
        if (normalized.StartsWith('+'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 11 && normalized[0] == '1')
        {
            normalized = normalized[1..];
        }

        if (normalized.Length != 10)
        {
            return false;
        }

        return NanpRegex().IsMatch(normalized);
    }
}
