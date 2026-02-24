namespace Granit.Validation.Internal;

/// <summary>
/// Validates Belgian electronic identity card numbers (eID).
/// </summary>
/// <remarks>
/// Format: 12 digits, optionally formatted as <c>NNN-NNNNNNN-NN</c>.
/// The last two digits form the check pair: <c>97 − (first 10 digits mod 97)</c>.
/// A result of 97 is represented as <c>97</c> (not <c>00</c>).
/// </remarks>
internal static class EidAlgorithm
{
    private const int RequiredLength = 12;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Belgian eID number.
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

        if (!long.TryParse(digits[..10], out long base10))
        {
            return false;
        }

        if (!int.TryParse(digits[10..], out int check))
        {
            return false;
        }

        int expected = 97 - (int)(base10 % 97);
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
