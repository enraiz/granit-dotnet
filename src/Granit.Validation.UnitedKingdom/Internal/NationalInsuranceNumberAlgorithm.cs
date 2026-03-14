using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates United Kingdom National Insurance (NI) numbers.
/// </summary>
/// <remarks>
/// Format: 2 prefix letters + 6 digits + 1 suffix letter (A, B, C, or D).
/// Spaces and dashes are allowed as separators.
/// Invalid prefixes: BG, GB, NK, KN, TN, NT, ZZ, and any starting with D, F, I, Q, U, or V.
/// Second letter cannot be D, F, I, Q, U, or V (except for administrative prefixes).
/// </remarks>
internal static partial class NationalInsuranceNumberAlgorithm
{
    [GeneratedRegex(@"[\s\-]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    // NI: 2 letters + 6 digits + 1 letter (A-D).
    [GeneratedRegex(@"^[A-Z]{2}\d{6}[A-D]$", RegexOptions.None, 100)]
    private static partial Regex NiRegex();

    private static readonly FrozenSet<string> InvalidPrefixes = new[]
    {
        "BG", "GB", "NK", "KN", "TN", "NT", "ZZ",
    }.ToFrozenSet();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid UK NI number.
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

        if (!NiRegex().IsMatch(normalized))
        {
            return false;
        }

        string prefix = normalized[..2];

        // First letter cannot be D, F, I, Q, U, or V.
        if ("DFIQUV".Contains(normalized[0]))
        {
            return false;
        }

        // Second letter cannot be D, F, I, O, Q, U, or V.
        if ("DFIOQUV".Contains(normalized[1]))
        {
            return false;
        }

        return !InvalidPrefixes.Contains(prefix);
    }
}
