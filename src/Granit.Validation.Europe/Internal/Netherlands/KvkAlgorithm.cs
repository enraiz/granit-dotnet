namespace Granit.Validation.Europe.Internal.Netherlands;

/// <summary>
/// Validates Dutch Chamber of Commerce numbers (Kamer van Koophandel / KVK).
/// </summary>
/// <remarks>
/// Format: exactly 8 digits. This is a simple format check; no check digit algorithm is applied.
/// Spaces are stripped before validation.
/// </remarks>
internal static class KvkAlgorithm
{
    private const int RequiredLength = 8;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Dutch KVK number.
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

        return normalized.All(char.IsDigit);
    }
}
