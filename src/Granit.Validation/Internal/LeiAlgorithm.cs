namespace Granit.Validation.Internal;

/// <summary>
/// Validates Legal Entity Identifiers (LEI) per ISO 17442.
/// </summary>
/// <remarks>
/// A LEI is exactly 20 alphanumeric characters:
/// <list type="bullet">
///   <item>Characters 1–4: LOU (Local Operating Unit) prefix — alphanumeric.</item>
///   <item>Characters 5–18: Entity-specific part — alphanumeric.</item>
///   <item>Characters 19–20: Check digits — numeric, validated using ISO 7064 MOD 97-10.</item>
/// </list>
/// The check is identical to IBAN: convert letters to digits (A=10, …, Z=35),
/// compute the resulting number mod 97 — the remainder must equal 1.
/// </remarks>
internal static class LeiAlgorithm
{
    private const int ExpectedLength = 20;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid LEI.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != ExpectedLength)
        {
            return false;
        }

        if (!normalized.All(char.IsLetterOrDigit))
        {
            return false;
        }

        // Check digits (positions 19–20) must be digits.
        if (!char.IsDigit(normalized[18]) || !char.IsDigit(normalized[19]))
        {
            return false;
        }

        // ISO 7064 MOD 97-10 validation.
        string numeric = Mod97Algorithm.LettersToDigits(normalized);
        return Mod97Algorithm.Compute(numeric) == 1;
    }
}
