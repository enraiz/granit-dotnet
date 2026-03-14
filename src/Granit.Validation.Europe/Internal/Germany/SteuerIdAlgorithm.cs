namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates German Tax Identification Numbers (Steuerliche Identifikationsnummer).
/// </summary>
/// <remarks>
/// Format: exactly 11 digits, first digit non-zero.
/// <list type="bullet">
///   <item>Positions 1–10: among these digits, exactly one digit (0–9) must appear twice and
///     exactly one digit must not appear at all.</item>
///   <item>Position 11: check digit computed using ISO 7064 MOD 11,10 over the first 10 digits.</item>
/// </list>
/// Spaces and dashes are stripped before validation.
/// </remarks>
internal static class SteuerIdAlgorithm
{
    private const int RequiredLength = 11;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid German Steuer-ID.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string digits = ExtractDigits(value);

        if (digits.Length != RequiredLength)
        {
            return false;
        }

        if (digits[0] == '0')
        {
            return false;
        }

        if (!HasValidDigitDistribution(digits))
        {
            return false;
        }

        int checkDigit = ComputeCheckDigit(digits);
        return checkDigit == digits[10] - '0';
    }

    /// <summary>
    /// Verifies that among positions 1–10, exactly one digit appears twice and exactly one digit
    /// does not appear at all.
    /// </summary>
    private static bool HasValidDigitDistribution(string digits)
    {
        int[] frequency = new int[10];
        for (int i = 0; i < 10; i++)
        {
            frequency[digits[i] - '0']++;
        }

        int doubles = 0;
        int zeros = 0;
        foreach (int count in frequency)
        {
            if (count == 2)
            {
                doubles++;
            }
            else if (count == 0)
            {
                zeros++;
            }
            else if (count > 2)
            {
                return false;
            }
        }

        return doubles == 1 && zeros == 1;
    }

    /// <summary>
    /// Computes the check digit using the ISO 7064 MOD 11,10 algorithm.
    /// </summary>
    private static int ComputeCheckDigit(string digits)
    {
        int product = 10;

        for (int i = 0; i < 10; i++)
        {
            int sum = (digits[i] - '0' + product) % 10;
            if (sum == 0)
            {
                sum = 10;
            }

            product = (sum * 2) % 11;
        }

        int result = 11 - product;
        return result == 10 ? 0 : result;
    }

    private static string ExtractDigits(string value)
    {
        System.Text.StringBuilder sb = new(RequiredLength);
        foreach (char c in value.Where(char.IsDigit))
        {
            sb.Append(c);
        }

        return sb.ToString();
    }
}
