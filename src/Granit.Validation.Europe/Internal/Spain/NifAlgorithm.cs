namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Spanish NIF/DNI numbers (Número de Identificación Fiscal / Documento Nacional de Identidad).
/// </summary>
/// <remarks>
/// Format: 8 digits followed by 1 control letter.
/// The control letter is computed as: <c>"TRWAGMYFPDXBNJZSQVHLCKE"[number mod 23]</c>.
/// Spaces and dashes are stripped before validation; input is normalised to uppercase.
/// </remarks>
internal static class NifAlgorithm
{
    internal const string ControlLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Spanish NIF/DNI.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = Normalize(value);

        if (normalized.Length != 9)
        {
            return false;
        }

        // First 8 characters must be digits.
        if (!int.TryParse(normalized[..8], out int number))
        {
            return false;
        }

        char expectedLetter = ControlLetters[number % 23];
        return normalized[8] == expectedLetter;
    }

    internal static string Normalize(string value) =>
        value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
}
