namespace Granit.Validation.Internal;

/// <summary>
/// Validates Belgian BCE enterprise numbers (Banque-Carrefour des Entreprises / KBO).
/// </summary>
/// <remarks>
/// Format: 10 digits, optionally formatted as <c>0xxx.xxx.xxx</c> or <c>1xxx.xxx.xxx</c>.
/// The last two digits form the check pair: <c>97 − (first 8 digits mod 97)</c>.
/// A result of 97 is represented as <c>97</c> (not <c>00</c>).
/// </remarks>
internal static class BceAlgorithm
{
    private const int RequiredLength = 10;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Belgian BCE number.
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

        if (!long.TryParse(digits[..8], out long base8))
        {
            return false;
        }

        if (!int.TryParse(digits[8..], out int check))
        {
            return false;
        }

        int expected = 97 - (int)(base8 % 97);
        return expected == check;
    }

    private static string ExtractDigits(string value)
    {
        System.Text.StringBuilder sb = new(RequiredLength);
        foreach (char c in value)
        {
            if (char.IsDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
