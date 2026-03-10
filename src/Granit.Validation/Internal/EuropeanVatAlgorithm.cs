using System.Collections.Frozen;
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
internal static partial class EuropeanVatAlgorithm
{
    private static readonly FrozenDictionary<string, Regex> FormatMap =
        new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase)
        {
            ["AT"] = AtRegex(),
            ["BE"] = BeRegex(),
            ["BG"] = BgRegex(),
            ["CY"] = CyRegex(),
            ["CZ"] = CzRegex(),
            ["DE"] = DeRegex(),
            ["DK"] = DkRegex(),
            ["EE"] = EeRegex(),
            ["EL"] = ElRegex(),
            ["ES"] = EsRegex(),
            ["FI"] = FiRegex(),
            ["FR"] = FrRegex(),
            ["HR"] = HrRegex(),
            ["HU"] = HuRegex(),
            ["IE"] = IeRegex(),
            ["IT"] = ItRegex(),
            ["LT"] = LtRegex(),
            ["LU"] = LuRegex(),
            ["LV"] = LvRegex(),
            ["MT"] = MtRegex(),
            ["NL"] = NlRegex(),
            ["PL"] = PlRegex(),
            ["PT"] = PtRegex(),
            ["RO"] = RoRegex(),
            ["SE"] = SeRegex(),
            ["SI"] = SiRegex(),
            ["SK"] = SkRegex(),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

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

    [GeneratedRegex(@"^ATU\d{8}$", RegexOptions.None, 100)]
    private static partial Regex AtRegex();

    [GeneratedRegex(@"^BE0?\d{9}$", RegexOptions.None, 100)]
    private static partial Regex BeRegex();

    [GeneratedRegex(@"^BG\d{9,10}$", RegexOptions.None, 100)]
    private static partial Regex BgRegex();

    [GeneratedRegex(@"^CY\d{8}[A-Z]$", RegexOptions.None, 100)]
    private static partial Regex CyRegex();

    [GeneratedRegex(@"^CZ\d{8,10}$", RegexOptions.None, 100)]
    private static partial Regex CzRegex();

    [GeneratedRegex(@"^DE\d{9}$", RegexOptions.None, 100)]
    private static partial Regex DeRegex();

    [GeneratedRegex(@"^DK\d{8}$", RegexOptions.None, 100)]
    private static partial Regex DkRegex();

    [GeneratedRegex(@"^EE\d{9}$", RegexOptions.None, 100)]
    private static partial Regex EeRegex();

    [GeneratedRegex(@"^EL\d{9}$", RegexOptions.None, 100)]
    private static partial Regex ElRegex();

    [GeneratedRegex(@"^ES[A-Z0-9]\d{7}[A-Z0-9]$", RegexOptions.None, 100)]
    private static partial Regex EsRegex();

    [GeneratedRegex(@"^FI\d{8}$", RegexOptions.None, 100)]
    private static partial Regex FiRegex();

    [GeneratedRegex(@"^FR[0-9]{2}\d{9}$", RegexOptions.None, 100)]
    private static partial Regex FrRegex();

    [GeneratedRegex(@"^HR\d{11}$", RegexOptions.None, 100)]
    private static partial Regex HrRegex();

    [GeneratedRegex(@"^HU\d{8}$", RegexOptions.None, 100)]
    private static partial Regex HuRegex();

    [GeneratedRegex(@"^IE\d[A-Z0-9+*]\d{5}[A-Z]{1,2}$", RegexOptions.None, 100)]
    private static partial Regex IeRegex();

    [GeneratedRegex(@"^IT\d{11}$", RegexOptions.None, 100)]
    private static partial Regex ItRegex();

    [GeneratedRegex(@"^LT(\d{9}|\d{12})$", RegexOptions.None, 100)]
    private static partial Regex LtRegex();

    [GeneratedRegex(@"^LU\d{8}$", RegexOptions.None, 100)]
    private static partial Regex LuRegex();

    [GeneratedRegex(@"^LV\d{11}$", RegexOptions.None, 100)]
    private static partial Regex LvRegex();

    [GeneratedRegex(@"^MT\d{8}$", RegexOptions.None, 100)]
    private static partial Regex MtRegex();

    [GeneratedRegex(@"^NL\d{9}B\d{2}$", RegexOptions.None, 100)]
    private static partial Regex NlRegex();

    [GeneratedRegex(@"^PL\d{10}$", RegexOptions.None, 100)]
    private static partial Regex PlRegex();

    [GeneratedRegex(@"^PT\d{9}$", RegexOptions.None, 100)]
    private static partial Regex PtRegex();

    [GeneratedRegex(@"^RO\d{2,10}$", RegexOptions.None, 100)]
    private static partial Regex RoRegex();

    [GeneratedRegex(@"^SE\d{12}$", RegexOptions.None, 100)]
    private static partial Regex SeRegex();

    [GeneratedRegex(@"^SI\d{8}$", RegexOptions.None, 100)]
    private static partial Regex SiRegex();

    [GeneratedRegex(@"^SK\d{10}$", RegexOptions.None, 100)]
    private static partial Regex SkRegex();
}
