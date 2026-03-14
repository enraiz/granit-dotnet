using System.Text.RegularExpressions;

namespace Granit.Validation.Europe.Internal;

/// <summary>
/// Validates Economic Operators Registration and Identification (EORI) numbers.
/// </summary>
/// <remarks>
/// An EORI number consists of an ISO 3166-1 alpha-2 country code followed by
/// up to 15 alphanumeric characters (the national identifier). The format varies
/// by member state but always starts with a 2-letter EU country code.
/// For countries with known VAT formats (e.g. FR, BE, DE), the national part
/// is validated against the VAT format. For others, format-only validation is applied.
/// </remarks>
internal static partial class EoriAlgorithm
{
    // EORI: 2 alpha country code + 1–15 alphanumeric characters.
    [GeneratedRegex(@"^[A-Z]{2}[A-Z0-9]{1,15}$", RegexOptions.None, 100)]
    private static partial Regex EoriRegex();

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid EORI number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
                                 .ToUpperInvariant();

        return EoriRegex().IsMatch(normalized);
    }
}
