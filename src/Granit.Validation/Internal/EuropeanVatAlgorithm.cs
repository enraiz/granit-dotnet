using System.Text.RegularExpressions;

namespace Granit.Validation.Internal;

/// <summary>
/// Validates EU VAT numbers by dispatching to country-specific validators.
/// </summary>
/// <remarks>
/// Each member state has its own format. This validator applies the strictest check
/// available per country — algorithm-based for FR and BE, format-based (regex) for others.
/// Country codes follow ISO 3166-1 alpha-2 (except Greece which uses <c>EL</c> in tax contexts).
/// </remarks>
internal static class EuropeanVatAlgorithm
{
    // Country-code → (min length of number, max length, regex of the full VAT string)
    // Format: CC + local number (regex anchored on the full string after uppercasing).
    private static readonly Dictionary<string, Regex> FormatMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AT"] = Compiled(@"^ATU\d{8}$"),
        ["BE"] = Compiled(@"^BE0?\d{9}$"),
        ["BG"] = Compiled(@"^BG\d{9,10}$"),
        ["CY"] = Compiled(@"^CY\d{8}[A-Z]$"),
        ["CZ"] = Compiled(@"^CZ\d{8,10}$"),
        ["DE"] = Compiled(@"^DE\d{9}$"),
        ["DK"] = Compiled(@"^DK\d{8}$"),
        ["EE"] = Compiled(@"^EE\d{9}$"),
        ["EL"] = Compiled(@"^EL\d{9}$"),  // Greece uses EL in EU VAT context
        ["ES"] = Compiled(@"^ES[A-Z0-9]\d{7}[A-Z0-9]$"),
        ["FI"] = Compiled(@"^FI\d{8}$"),
        ["FR"] = Compiled(@"^FR[0-9]{2}\d{9}$"),
        ["HR"] = Compiled(@"^HR\d{11}$"),
        ["HU"] = Compiled(@"^HU\d{8}$"),
        ["IE"] = Compiled(@"^IE\d[A-Z0-9+*]\d{5}[A-Z]{1,2}$"),
        ["IT"] = Compiled(@"^IT\d{11}$"),
        ["LT"] = Compiled(@"^LT(\d{9}|\d{12})$"),
        ["LU"] = Compiled(@"^LU\d{8}$"),
        ["LV"] = Compiled(@"^LV\d{11}$"),
        ["MT"] = Compiled(@"^MT\d{8}$"),
        ["NL"] = Compiled(@"^NL\d{9}B\d{2}$"),
        ["PL"] = Compiled(@"^PL\d{10}$"),
        ["PT"] = Compiled(@"^PT\d{9}$"),
        ["RO"] = Compiled(@"^RO\d{2,10}$"),
        ["SE"] = Compiled(@"^SE\d{12}$"),
        ["SI"] = Compiled(@"^SI\d{8}$"),
        ["SK"] = Compiled(@"^SK\d{10}$"),
    };

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid EU VAT number.
    /// Unsupported country codes return <see langword="false"/>.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
                                 .ToUpperInvariant();

        if (normalized.Length < 4)
        {
            return false;
        }

        string countryCode = normalized[..2];

        if (!FormatMap.TryGetValue(countryCode, out Regex? pattern))
        {
            return false;
        }

        if (!pattern.IsMatch(normalized))
        {
            return false;
        }

        // Deep validation for countries with algorithmic checks.
        return countryCode switch
        {
            "FR" => FrenchVatAlgorithm.IsValid(normalized),
            "BE" => ValidateBelgianVat(normalized),
            _ => true // Format-only validation for other countries.
        };
    }

    private static bool ValidateBelgianVat(string normalized)
    {
        // Belgian VAT: BE + BCE number (may start with 0 or 1, with leading 0 optional).
        string digits = normalized[2..];

        // Normalise to 10 digits: prepend 0 if only 9 digits.
        if (digits.Length == 9)
        {
            digits = "0" + digits;
        }

        return BceAlgorithm.IsValid(digits);
    }

    private static Regex Compiled(string pattern) =>
        new(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
}
