namespace Granit.Validation.Internal;

/// <summary>
/// Validates French National Identification Numbers (NIR — Numéro d'Identification au Répertoire).
/// </summary>
/// <remarks>
/// The NIR is a 15-character identifier: 13 base digits + 2 check digits.
/// <list type="bullet">
///   <item>Position 1 : sex (1 = male, 2 = female)</item>
///   <item>Positions 2–3 : last two digits of birth year</item>
///   <item>Positions 4–5 : birth month (01–12, 20 for abroad)</item>
///   <item>Positions 6–10 : INSEE commune code (2-digit dept + 3-digit commune).
///     Departments 2A (Corse-du-Sud) and 2B (Haute-Corse) are replaced by 19 and 18
///     respectively before the check computation.</item>
///   <item>Positions 11–13 : sequential order within the commune</item>
///   <item>Positions 14–15 : check key = 97 − (13-digit base mod 97)</item>
/// </list>
/// Spaces, dots and dashes are accepted and stripped before validation.
/// </remarks>
internal static class NirAlgorithm
{
    private const int NirLength = 15;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid French NIR.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = Normalize(value);

        if (normalized.Length != NirLength)
        {
            return false;
        }

        // Replace Corse department codes: 2A → 19, 2B → 18 (positions 6–7, index 5–6).
        string forComputation = ApplyCorseReplacement(normalized);

        if (!long.TryParse(forComputation[..13], out long base13))
        {
            return false;
        }

        if (!int.TryParse(forComputation[13..], out int check))
        {
            return false;
        }

        int expected = 97 - (int)(base13 % 97);
        return expected == check;
    }

    private static string Normalize(string value)
    {
        System.Text.StringBuilder sb = new(NirLength + 2);
        foreach (char c in value.Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '-'))
        {
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string ApplyCorseReplacement(string nir)
    {
        // Positions 6–7 (0-indexed 5–6) hold the 2-character department code.
        if (nir.Length < 7)
        {
            return nir;
        }

        string dept = nir[5..7];

        return dept switch
        {
            "2A" or "2a" => nir[..5] + "19" + nir[7..],
            "2B" or "2b" => nir[..5] + "18" + nir[7..],
            _ => nir
        };
    }
}
