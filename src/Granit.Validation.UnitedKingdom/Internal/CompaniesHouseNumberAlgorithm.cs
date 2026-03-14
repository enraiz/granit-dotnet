using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates Companies House registration numbers.
/// </summary>
/// <remarks>
/// A Companies House number is 8 characters:
/// <list type="bullet">
///   <item>All digits (e.g. 01234567)</item>
///   <item>2-letter prefix + 6 digits: valid prefixes are OC, SO, SC, NI, R0, NC, NF, IP, SP, IC, SI, NP, NO, RS, SR.</item>
/// </list>
/// </remarks>
internal static partial class CompaniesHouseNumberAlgorithm
{
    // 8 digits.
    [GeneratedRegex(@"^\d{8}$", RegexOptions.None, 100)]
    private static partial Regex AllDigitsRegex();

    // 2 letter prefix + 6 digits.
    [GeneratedRegex(@"^(OC|SO|SC|NI|R0|NC|NF|IP|SP|IC|SI|NP|NO|RS|SR)\d{6}$", RegexOptions.None, 100)]
    private static partial Regex PrefixedRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Companies House number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToUpperInvariant();

        return AllDigitsRegex().IsMatch(normalized) || PrefixedRegex().IsMatch(normalized);
    }
}
