namespace Granit.Validation.Internal;

/// <summary>
/// Implements the Luhn algorithm (ISO/IEC 7812-1) for numeric check-digit validation.
/// </summary>
/// <remarks>
/// Used by SIREN (9 digits), SIRET (14 digits), and any other numeric identifier
/// that uses a Luhn check digit as the rightmost character.
/// </remarks>
internal static class LuhnAlgorithm
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="digits"/> passes the Luhn check.
    /// </summary>
    /// <param name="digits">Pre-validated all-digit string of the expected length.</param>
    public static bool IsValid(string digits)
    {
        int sum = 0;
        bool doubleDigit = false;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int digit = digits[i] - '0';

            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// Validates that <paramref name="value"/> consists of exactly <paramref name="expectedLength"/>
    /// digits and passes the Luhn check.
    /// </summary>
    public static bool IsValidFixedLength(string? value, int expectedLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();

        if (normalized.Length != expectedLength)
        {
            return false;
        }

        if (!normalized.All(char.IsDigit))
        {
            return false;
        }

        return IsValid(normalized);
    }
}
