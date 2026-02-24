namespace Granit.Validation.Internal;

/// <summary>
/// Validates French SIRET numbers (Système d'Identification du Répertoire des ÉTablissements).
/// </summary>
/// <remarks>
/// Format: exactly 14 digits — SIREN (9) + NIC (5) — with a valid Luhn check digit
/// computed over all 14 digits.
/// Spaces are stripped before validation: <c>732 829 320 00074</c> and <c>73282932000074</c> are both accepted.
/// </remarks>
internal static class SiretAlgorithm
{
    private const int RequiredLength = 14;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid French SIRET.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        return LuhnAlgorithm.IsValidFixedLength(normalized, RequiredLength);
    }
}
