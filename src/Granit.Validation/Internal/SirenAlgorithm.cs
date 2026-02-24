namespace Granit.Validation.Internal;

/// <summary>
/// Validates French SIREN numbers (Système d'Identification du Répertoire des ENtreprises).
/// </summary>
/// <remarks>
/// Format: exactly 9 digits with a valid Luhn check digit as the rightmost digit.
/// Spaces are stripped before validation: <c>732 829 320</c> and <c>732829320</c> are both accepted.
/// Issued by INSEE to legal entities registered in France.
/// </remarks>
internal static class SirenAlgorithm
{
    private const int RequiredLength = 9;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid French SIREN.
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
