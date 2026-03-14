using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Granit.Validation.Europe.Internal.Italy;

/// <summary>
/// Validates Italian Codice Fiscale (fiscal code) for natural persons.
/// </summary>
/// <remarks>
/// Format: 16 alphanumeric characters.
/// <list type="bullet">
///   <item>Positions 1–3: three consonants from surname</item>
///   <item>Positions 4–6: three consonants/vowels from first name</item>
///   <item>Positions 7–8: last two digits of birth year</item>
///   <item>Position 9: month letter (A=Jan, B=Feb, C=Mar, D=Apr, E=May, H=Jun,
///     L=Jul, M=Aug, P=Sep, R=Oct, S=Nov, T=Dec)</item>
///   <item>Positions 10–11: birth day (females add 40)</item>
///   <item>Positions 12–15: municipality code (letter + 3 digits)</item>
///   <item>Position 16: check character computed from the first 15</item>
/// </list>
/// Spaces are stripped and input is uppercased before validation.
/// </remarks>
internal static partial class CodiceFiscaleAlgorithm
{
    [GeneratedRegex(@"^[A-Z]{6}\d{2}[A-EHLMPRST]\d{2}[A-Z]\d{3}[A-Z]$", RegexOptions.None, 100)]
    private static partial Regex FormatRegex();

    /// <summary>
    /// Odd-position character values used by the check character algorithm.
    /// Position numbering is 1-based: characters at positions 1, 3, 5, …, 15 are "odd".
    /// </summary>
    private static readonly FrozenDictionary<char, int> OddValues =
        new Dictionary<char, int>
        {
            ['0'] = 1,
            ['1'] = 0,
            ['2'] = 5,
            ['3'] = 7,
            ['4'] = 9,
            ['5'] = 13,
            ['6'] = 15,
            ['7'] = 17,
            ['8'] = 19,
            ['9'] = 21,
            ['A'] = 1,
            ['B'] = 0,
            ['C'] = 5,
            ['D'] = 7,
            ['E'] = 9,
            ['F'] = 13,
            ['G'] = 15,
            ['H'] = 17,
            ['I'] = 19,
            ['J'] = 21,
            ['K'] = 2,
            ['L'] = 4,
            ['M'] = 18,
            ['N'] = 20,
            ['O'] = 11,
            ['P'] = 3,
            ['Q'] = 6,
            ['R'] = 8,
            ['S'] = 12,
            ['T'] = 14,
            ['U'] = 16,
            ['V'] = 10,
            ['W'] = 22,
            ['X'] = 25,
            ['Y'] = 24,
            ['Z'] = 23,
        }.ToFrozenDictionary();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Italian Codice Fiscale.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
                                  .Trim()
                                  .ToUpperInvariant();

        if (normalized.Length != 16)
        {
            return false;
        }

        if (!FormatRegex().IsMatch(normalized))
        {
            return false;
        }

        return ComputeCheckChar(normalized[..15]) == normalized[15];
    }

    private static char ComputeCheckChar(string first15)
    {
        int sum = 0;

        for (int i = 0; i < 15; i++)
        {
            char c = first15[i];

            if ((i + 1) % 2 == 0)
            {
                // Even position (1-based): digits 0–9 → 0–9, letters A–Z → 0–25.
                sum += char.IsDigit(c) ? c - '0' : c - 'A';
            }
            else
            {
                // Odd position (1-based): use the special mapping.
                sum += OddValues[c];
            }
        }

        return (char)('A' + (sum % 26));
    }
}
