using System.Text.RegularExpressions;

namespace Granit.Validation.NorthAmerica.Internal.UnitedStates;

/// <summary>
/// Validates United States ZIP codes.
/// </summary>
/// <remarks>
/// Accepts 5-digit ZIP (e.g. 10001) and ZIP+4 (e.g. 10001-1234).
/// </remarks>
internal static partial class ZipCodeAlgorithm
{
    // 5-digit ZIP or ZIP+4 format.
    [GeneratedRegex(@"^\d{5}(-\d{4})?$", RegexOptions.None, 100)]
    private static partial Regex ZipRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid US ZIP code.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return ZipRegex().IsMatch(value.Trim());
    }
}
