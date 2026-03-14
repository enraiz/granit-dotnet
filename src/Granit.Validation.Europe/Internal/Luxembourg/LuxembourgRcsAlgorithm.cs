using System.Text.RegularExpressions;

namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Luxembourg RCS numbers (Registre de Commerce et des Sociétés).
/// </summary>
/// <remarks>
/// Format: a single letter prefix (A–J or S) followed by 1 to 6 digits.
/// <list type="bullet">
///   <item>A: société anonyme</item>
///   <item>B: société à responsabilité limitée</item>
///   <item>C: société en commandite par actions</item>
///   <item>D: société en commandite simple</item>
///   <item>E: société en nom collectif</item>
///   <item>F: société civile</item>
///   <item>G: groupement d'intérêt économique</item>
///   <item>H: société européenne</item>
///   <item>I: association sans but lucratif</item>
///   <item>J: société coopérative</item>
///   <item>S: succursale d'une société étrangère</item>
/// </list>
/// Spaces are stripped and input is uppercased before validation.
/// </remarks>
internal static partial class LuxembourgRcsAlgorithm
{
    [GeneratedRegex(@"^[A-JS]\d{1,6}$", RegexOptions.None, 100)]
    private static partial Regex RcsRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Luxembourg RCS number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
                                  .Trim()
                                  .ToUpperInvariant();

        return RcsRegex().IsMatch(normalized);
    }
}
