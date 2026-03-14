using Granit.Validation.Internal;

namespace Granit.Validation.Europe.Internal.Luxembourg;

/// <summary>
/// Validates Luxembourg national identification numbers (matricule national).
/// </summary>
/// <remarks>
/// Format: exactly 13 digits (YYYYMMDDXXXCC).
/// <list type="bullet">
///   <item>YYYY: birth year</item>
///   <item>MM: birth month</item>
///   <item>DD: birth day</item>
///   <item>XXX: sequence number</item>
///   <item>CC: check digits</item>
/// </list>
/// Validation: all 13 digits must pass the Luhn check.
/// Spaces and dashes are stripped before validation.
/// </remarks>
internal static class LuxembourgMatriculeAlgorithm
{
    private const int RequiredLength = 13;

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Luxembourg matricule national.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
                                  .Replace("-", string.Empty, StringComparison.Ordinal)
                                  .Trim();

        return LuhnAlgorithm.IsValidFixedLength(normalized, RequiredLength);
    }
}
