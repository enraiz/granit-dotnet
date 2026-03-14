using System.Text.RegularExpressions;

namespace Granit.Validation.UnitedKingdom.Internal;

/// <summary>
/// Validates HMRC Unique Taxpayer Reference (UTR) numbers.
/// </summary>
/// <remarks>
/// A UTR is a 10-digit number. The first digit is a MOD 11 check digit computed
/// from digits 2–10 using weights 6, 7, 8, 9, 10, 5, 4, 3, 2.
/// The check digit is (11 − (sum MOD 11)) MOD 11. If the result is 10, the check digit is K
/// (but since UTR is digits-only, numbers with check digit K are invalid).
/// </remarks>
internal static partial class UtrAlgorithm
{
    private static readonly int[] Weights = [6, 7, 8, 9, 10, 5, 4, 3, 2];

    [GeneratedRegex(@"[\s\-]+", RegexOptions.None, 100)]
    private static partial Regex SeparatorRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid UTR.
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
            int digit = normalized[i + 1] - '0';
            sum += digit * Weights[i];
        }

        int remainder = sum % 11;
        int expectedCheckDigit = (11 - remainder) % 11;

        // If check digit would be 10 (K), number is invalid.
        if (expectedCheckDigit == 10)
        {
            return false;
        }

        int actualCheckDigit = normalized[0] - '0';

        return actualCheckDigit == expectedCheckDigit;
    }
}
