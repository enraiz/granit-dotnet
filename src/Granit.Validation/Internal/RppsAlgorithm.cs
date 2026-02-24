namespace Granit.Validation.Internal;

/// <summary>
/// Validates French RPPS numbers (Répertoire Partagé des Professionnels de Santé).
/// </summary>
/// <remarks>
/// Format: 11 digits, Luhn check digit on the last digit.
/// All French health professionals registered since 2009 have an RPPS number.
/// </remarks>
internal static class RppsAlgorithm
{
    private const int RequiredLength = 11;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid French RPPS number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string digits = value.Trim();

        if (digits.Length != RequiredLength)
        {
            return false;
        }

        if (!digits.All(char.IsDigit))
        {
            return false;
        }

        return LuhnCheck(digits);
    }

    private static bool LuhnCheck(string digits)
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
}
