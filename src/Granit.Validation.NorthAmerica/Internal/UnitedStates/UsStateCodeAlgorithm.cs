using System.Collections.Frozen;

namespace Granit.Validation.NorthAmerica.Internal.UnitedStates;

/// <summary>
/// Validates USPS state and territory codes (2-letter abbreviations).
/// </summary>
/// <remarks>
/// Includes 50 states, DC, and US territories (AS, GU, MP, PR, VI, UM).
/// Also includes armed forces codes (AA, AE, AP).
/// Reference: USPS Publication 28.
/// </remarks>
internal static class UsStateCodeAlgorithm
{
    // 50 states + DC + 6 territories + 3 armed forces = 60 codes.
    private static readonly FrozenSet<string> ValidCodes = new[]
    {
        // States
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
        // District of Columbia
        "DC",
        // Territories
        "AS", "GU", "MP", "PR", "VI", "UM",
        // Armed forces
        "AA", "AE", "AP",
    }.ToFrozenSet();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid USPS state/territory code.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return ValidCodes.Contains(value.Trim().ToUpperInvariant());
    }
}
