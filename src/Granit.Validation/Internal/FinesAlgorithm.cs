namespace Granit.Validation.Internal;

/// <summary>
/// Validates French Finess numbers (Fichier National des Établissements Sanitaires et Sociaux).
/// </summary>
/// <remarks>
/// Format: 9 characters — 2-digit department code + 6 digits + 1 Luhn check digit.
/// Used to identify French health establishments (hospitals, clinics, pharmacies, etc.).
/// </remarks>
internal static class FinesAlgorithm
{
    private const int RequiredLength = 9;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid French Finess number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();

        if (normalized.Length != RequiredLength)
        {
            return false;
        }

        foreach (char c in normalized)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return LuhnCheck(normalized);
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
