namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Spanish NIE numbers (Número de Identidad de Extranjero).
/// </summary>
/// <remarks>
/// Format: 1 initial letter (X, Y, or Z) + 7 digits + 1 control letter.
/// The initial letter is replaced with a digit (X→0, Y→1, Z→2), then the NIF control
/// letter algorithm is applied: <c>"TRWAGMYFPDXBNJZSQVHLCKE"[number mod 23]</c>.
/// Spaces and dashes are stripped before validation; input is normalised to uppercase.
/// </remarks>
internal static class NieAlgorithm
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Spanish NIE.
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

        char prefix = normalized[0];

        string digitPrefix = prefix switch
        {
            'X' => "0",
            'Y' => "1",
            'Z' => "2",
            _ => string.Empty
        };

        if (digitPrefix.Length == 0)
        {
            return false;
        }

        // Replace the initial letter with the corresponding digit and validate as NIF.
        string asNumber = digitPrefix + normalized[1..8];

        if (!int.TryParse(asNumber, out int number))
        {
            return false;
        }

        char expectedLetter = NifAlgorithm.ControlLetters[number % 23];
        return normalized[8] == expectedLetter;
    }
}
