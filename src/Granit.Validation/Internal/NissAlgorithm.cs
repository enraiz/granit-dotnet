namespace Granit.Validation.Internal;

/// <summary>
/// Validates Belgian National Identification Numbers (NISS / INSZ / SSIN).
/// </summary>
/// <remarks>
/// Format: YY.MM.DD-SSS.CC (11 digits, separators optional).
/// <list type="bullet">
///   <item>YY = last two digits of birth year</item>
///   <item>MM = birth month</item>
///   <item>DD = birth day</item>
///   <item>SSS = sequence number (odd = male, even = female)</item>
///   <item>CC = check digit: 97 − (N mod 97), where N is the first 9 digits.
///     For persons born from 2000: N is prefixed with <c>2</c> before the modulo.</item>
/// </list>
/// </remarks>
internal static class NissAlgorithm
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Belgian NISS number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string digits = ExtractDigits(value);

        if (digits.Length != 11)
        {
            return false;
        }

        if (!long.TryParse(digits[..9], out long base9))
        {
            return false;
        }

        if (!int.TryParse(digits[9..], out int checkDigits))
        {
            return false;
        }

        // Try born before 2000 (no prefix)
        int expected = 97 - (int)(base9 % 97);
        if (expected == checkDigits)
        {
            return true;
        }

        // Try born from 2000 (prefix '2')
        if (!long.TryParse("2" + digits[..9], out long base9With2))
        {
            return false;
        }

        int expectedWith2 = 97 - (int)(base9With2 % 97);
        return expectedWith2 == checkDigits;
    }

    private static string ExtractDigits(string value)
    {
        System.Text.StringBuilder sb = new(11);
        foreach (char c in value.Where(char.IsDigit))
        {
            sb.Append(c);
        }

        return sb.ToString();
    }
}
