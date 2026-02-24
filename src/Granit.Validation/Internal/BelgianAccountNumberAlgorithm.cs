namespace Granit.Validation.Internal;

/// <summary>
/// Validates Belgian bank account numbers in the legacy pre-IBAN format.
/// </summary>
/// <remarks>
/// Format: <c>NNN-NNNNNNN-NN</c> — 3-digit bank code + 7-digit account + 2-digit check pair.
/// The check pair equals <c>(first 10 digits as integer) mod 97</c>, or 97 when the remainder is 0.
/// <para>
/// This format pre-dates IBAN. Belgian banks display IBAN on all statements since 2014.
/// Prefer <see cref="IbanAlgorithm"/> for new integrations.
/// </para>
/// Dashes and spaces are stripped before validation.
/// </remarks>
internal static class BelgianAccountNumberAlgorithm
{
    private const int RequiredLength = 12;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Belgian bank account number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = ExtractDigits(value);

        if (normalized.Length != RequiredLength)
        {
            return false;
        }

        if (!long.TryParse(normalized[..10], out long base10))
        {
            return false;
        }

        if (!int.TryParse(normalized[10..], out int providedKey))
        {
            return false;
        }

        long remainder = base10 % 97;
        int expectedKey = remainder == 0 ? 97 : (int)remainder;

        return expectedKey == providedKey;
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
