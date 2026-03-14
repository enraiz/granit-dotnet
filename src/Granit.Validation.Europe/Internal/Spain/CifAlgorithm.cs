namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Spanish CIF numbers (Código de Identificación Fiscal) for legal entities.
/// </summary>
/// <remarks>
/// Format: 1 letter + 7 digits + 1 control character (digit or letter).
/// <list type="bullet">
///   <item>Valid leading letters: A, B, C, D, E, F, G, H, J, K, L, M, N, P, Q, R, S, U, V, W.</item>
///   <item>Control algorithm: sum odd-position digits (doubled, subtract 9 if &gt;9) and
///     even-position digits. Control value = <c>(10 − sum mod 10) mod 10</c>.</item>
///   <item>Organisation types K, P, Q, S require a letter control character
///     (0→J, 1→A, 2→B, 3→C, 4→D, 5→E, 6→F, 7→G, 8→H, 9→I).</item>
///   <item>Organisation types A, B, E, H require a digit control character.</item>
///   <item>All other types accept either digit or letter.</item>
/// </list>
/// Spaces and dashes are stripped before validation; input is normalised to uppercase.
/// </remarks>
internal static class CifAlgorithm
{
    private const string ValidLetters = "ABCDEFGHJKLMNPQRSUVW";
    private const string ControlLetterMap = "JABCDEFGHI";
    private const string LetterOnlyTypes = "KPQS";
    private const string DigitOnlyTypes = "ABEH";

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Spanish CIF.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = NifAlgorithm.Normalize(value);

        if (normalized.Length != 9)
        {
            return false;
        }

        char orgType = normalized[0];

        if (!ValidLetters.Contains(orgType))
        {
            return false;
        }

        // Positions 1–7 must be digits.
        for (int i = 1; i <= 7; i++)
        {
            if (!char.IsDigit(normalized[i]))
            {
                return false;
            }
        }

        int controlValue = ComputeControlValue(normalized);
        char controlChar = normalized[8];

        if (LetterOnlyTypes.Contains(orgType))
        {
            return controlChar == ControlLetterMap[controlValue];
        }

        if (DigitOnlyTypes.Contains(orgType))
        {
            return controlChar == (char)('0' + controlValue);
        }

        // Other types accept either digit or letter.
        return controlChar == (char)('0' + controlValue) ||
               controlChar == ControlLetterMap[controlValue];
    }

    private static int ComputeControlValue(string normalized)
    {
        int sumEven = 0;
        int sumOdd = 0;

        // Positions 1–7 (1-indexed within the CIF, 0-indexed in string as [1]–[7]).
        // Odd positions (1, 3, 5, 7) use the doubling rule.
        // Even positions (2, 4, 6) are added directly.
        for (int i = 1; i <= 7; i++)
        {
            int digit = normalized[i] - '0';

            if (i % 2 != 0)
            {
                // Odd position: double, subtract 9 if > 9.
                int doubled = digit * 2;
                if (doubled > 9)
                {
                    doubled -= 9;
                }

                sumOdd += doubled;
            }
            else
            {
                sumEven += digit;
            }
        }

        int total = sumOdd + sumEven;
        return (10 - (total % 10)) % 10;
    }
}
