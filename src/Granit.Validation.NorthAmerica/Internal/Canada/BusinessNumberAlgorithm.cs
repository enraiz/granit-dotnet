using System.Text.RegularExpressions;

namespace Granit.Validation.NorthAmerica.Internal.Canada;

/// <summary>
/// Validates Canadian Business Numbers (BN / NE).
/// </summary>
/// <remarks>
/// A BN is a 9-digit number assigned by the CRA (Canada Revenue Agency).
/// The 9th digit is a check digit computed using Luhn mod-10.
/// The full BN account may include a 6-character program identifier suffix (e.g. 123456789 RT0001),
/// but this validator checks only the 9-digit root.
/// </remarks>
internal static partial class BusinessNumberAlgorithm
{
    [GeneratedRegex(@"[\s\-]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid 9-digit Canadian BN.
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

        return Granit.Validation.Internal.LuhnAlgorithm.IsValid(normalized);
    }
}
