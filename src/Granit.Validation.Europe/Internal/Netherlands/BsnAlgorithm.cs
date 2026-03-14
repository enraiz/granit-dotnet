namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Dutch Citizen Service Numbers (Burgerservicenummer / BSN).
/// </summary>
/// <remarks>
/// Format: exactly 9 digits.
/// <list type="bullet">
///   <item>Elfproef (eleven-test): <c>9×d1 + 8×d2 + 7×d3 + 6×d4 + 5×d5 + 4×d6 + 3×d7 + 2×d8 − 1×d9</c>
///     must be divisible by 11 and must not equal 0.</item>
/// </list>
/// Spaces and dashes are stripped before validation.
/// </remarks>
internal static class BsnAlgorithm
{
    private const int RequiredLength = 9;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Dutch BSN.
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

        // Elfproef: 9×d1 + 8×d2 + ... + 2×d8 - 1×d9, divisible by 11, not 0.
        int sum = 0;
        for (int i = 0; i < 8; i++)
        {
            sum += (9 - i) * (digits[i] - '0');
        }

        sum -= digits[8] - '0';

        return sum != 0 && sum % 11 == 0;
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
