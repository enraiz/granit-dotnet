using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates NHS (National Health Service) numbers.
/// </summary>
/// <remarks>
/// An NHS number is a 10-digit number where the last digit is a MOD 11 check digit.
/// Calculation: multiply digits 1–9 by weights 10–2, sum, take MOD 11.
/// If remainder is 0 → check digit is 0. If remainder is 11 → check digit is 0 (some specs say invalid).
/// Any other remainder → check digit = 11 − remainder. If result is 10, the number is invalid.
/// </remarks>
internal static partial class NhsNumberAlgorithm
{
    [GeneratedRegex(@"[\s\-]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid NHS number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = SeparatorRegex().Replace(value.Trim(), string.Empty);

        if (normalized.Length != 10 || !normalized.All(char.IsDigit))
        {
            return false;
        }

        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            int digit = normalized[i] - '0';
            int weight = 10 - i;
            sum += digit * weight;
        }

        int remainder = sum % 11;
        int expectedCheckDigit = 11 - remainder;

        // If check digit would be 11, treat as 0.
        if (expectedCheckDigit == 11)
        {
            expectedCheckDigit = 0;
        }

        // If check digit would be 10, the number is invalid.
        if (expectedCheckDigit == 10)
        {
            return false;
        }

        int actualCheckDigit = normalized[9] - '0';

        return actualCheckDigit == expectedCheckDigit;
    }
}
