namespace Granit.Validation.Internal;

/// <summary>
/// Validates Belgian INAMI numbers (Institut National d'Assurance Maladie-Invalidité / RIZIV).
/// </summary>
/// <remarks>
/// Format: 11 digits, optionally formatted as <c>XXXXXX/XXX-XX</c>.
/// The last two digits form the check pair: <c>97 − (first 9 digits mod 97)</c>.
/// A result of 97 is represented as <c>97</c> (not <c>00</c>).
/// </remarks>
internal static class InamiAlgorithm
{
    private const int RequiredLength = 11;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Belgian INAMI number.
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

        if (!long.TryParse(digits[..9], out long base9))
        {
            return false;
        }

        if (!int.TryParse(digits[9..], out int check))
        {
            return false;
        }

        int expected = 97 - (int)(base9 % 97);
        return expected == check;
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
