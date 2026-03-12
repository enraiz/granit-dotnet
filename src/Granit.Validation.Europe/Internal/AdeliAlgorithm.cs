namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates French ADELI numbers (Automatisation DEs LIstes).
/// </summary>
/// <remarks>
/// Format: exactly 9 digits. ADELI predates RPPS and is still used
/// for certain health professional categories in France.
/// </remarks>
internal static class AdeliAlgorithm
{
    private const int RequiredLength = 9;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid French ADELI number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string digits = value.Trim();

        if (digits.Length != RequiredLength)
        {
            return false;
        }

        if (!digits.All(char.IsDigit))
        {
            return false;
        }

        return true;
    }
}
