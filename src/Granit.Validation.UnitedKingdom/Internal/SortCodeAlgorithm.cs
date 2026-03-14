using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates United Kingdom bank sort codes.
/// </summary>
/// <remarks>
/// A sort code is a 6-digit number, typically formatted as XX-XX-XX.
/// Identifies the bank and branch for UK domestic payments.
/// </remarks>
internal static partial class SortCodeAlgorithm
{
    [GeneratedRegex(@"[\s\-]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid UK sort code.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = SeparatorRegex().Replace(value.Trim(), string.Empty);

        return normalized.Length == 6 && normalized.All(char.IsDigit);
    }
}
