namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Italian Partita IVA (VAT identification number).
/// </summary>
/// <remarks>
/// Format: exactly 11 digits.
/// <list type="bullet">
///   <item>Positions 1–7: progressive number assigned to the taxpayer</item>
///   <item>Positions 8–10: province code (001–100, 120, 121, 888, 999)</item>
///   <item>Position 11: check digit computed with a Luhn-variant algorithm</item>
/// </list>
/// Spaces are stripped before validation.
/// </remarks>
internal static class PartitaIvaAlgorithm
{
    private const int RequiredLength = 11;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Italian Partita IVA.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

        if (normalized.Length != RequiredLength)
        {
            return false;
        }

        if (!normalized.All(char.IsDigit))
        {
            return false;
        }

        // All zeros is not a valid Partita IVA.
        if (normalized.All(c => c == '0'))
        {
            return false;
        }

        int oddSum = 0;
        int evenSum = 0;

        for (int i = 0; i < RequiredLength - 1; i++)
        {
            int digit = normalized[i] - '0';

            if ((i + 1) % 2 != 0)
            {
                // Odd positions (1-based): 1st, 3rd, 5th, 7th, 9th — add directly.
                oddSum += digit;
            }
            else
            {
                // Even positions (1-based): 2nd, 4th, 6th, 8th, 10th — double, subtract 9 if > 9.
                int doubled = digit * 2;
                if (doubled > 9)
                {
                    doubled -= 9;
                }

                evenSum += doubled;
            }
        }

        int checkDigit = (10 - ((oddSum + evenSum) % 10)) % 10;
        return checkDigit == normalized[RequiredLength - 1] - '0';
    }
}
