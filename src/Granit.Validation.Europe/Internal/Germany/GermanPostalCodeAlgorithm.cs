using System.Text.RegularExpressions;

namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates German postal codes (Postleitzahl / PLZ).
/// </summary>
/// <remarks>
/// Format: exactly 5 digits in the range 01001–99998.
/// The value <c>00000</c> is not a valid postal code.
/// </remarks>
internal static partial class GermanPostalCodeAlgorithm
{
    [GeneratedRegex(@"^\d{5}$", RegexOptions.None, 100)]
    private static partial Regex PlzRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid German PLZ.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        return PlzRegex().IsMatch(trimmed) &&
               !trimmed.Equals("00000", StringComparison.Ordinal);
    }
}
