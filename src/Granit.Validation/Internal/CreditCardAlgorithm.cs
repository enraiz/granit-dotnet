namespace Granit.Validation.Internal;

/// <summary>
/// Validates credit/debit card numbers per ISO/IEC 7812-1.
/// </summary>
/// <remarks>
/// Strips spaces and dashes, verifies the Luhn check digit, and validates the
/// IIN (Issuer Identification Number) prefix to detect the card network.
/// Supported networks: Visa, Mastercard, American Express, Discover, Diners Club,
/// JCB, UnionPay, Maestro.
/// </remarks>
internal static class CreditCardAlgorithm
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a valid credit card number.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        if (normalized.Length < 12 || normalized.Length > 19)
        {
            return false;
        }

        if (!normalized.All(char.IsDigit))
        {
            return false;
        }

        if (!HasKnownIinPrefix(normalized))
        {
            return false;
        }

        return LuhnAlgorithm.IsValid(normalized);
    }

    /// <summary>
    /// Checks whether the card number starts with a known IIN prefix for major networks.
    /// </summary>
    private static bool HasKnownIinPrefix(string digits) =>
        IsVisa(digits)
        || IsMastercard(digits)
        || IsAmex(digits)
        || IsDiscover(digits)
        || IsDinersClub(digits)
        || IsJcb(digits)
        || IsUnionPay(digits)
        || IsMaestro(digits);

    // Visa: starts with 4, length 13 or 16 or 19.
    private static bool IsVisa(string d) =>
        d[0] == '4' && d.Length is 13 or 16 or 19;

    // Mastercard: 2221–2720 or 51–55, length 16.
    private static bool IsMastercard(string d)
    {
        if (d.Length != 16)
        {
            return false;
        }

        if (d.Length >= 4)
        {
            int prefix4 = int.Parse(d[..4], System.Globalization.CultureInfo.InvariantCulture);
            if (prefix4 >= 2221 && prefix4 <= 2720)
            {
                return true;
            }
        }

        int prefix2 = int.Parse(d[..2], System.Globalization.CultureInfo.InvariantCulture);
        return prefix2 >= 51 && prefix2 <= 55;
    }

    // American Express: starts with 34 or 37, length 15.
    private static bool IsAmex(string d) =>
        d.Length == 15 && (d.StartsWith("34", StringComparison.Ordinal) || d.StartsWith("37", StringComparison.Ordinal));

    // Discover: 6011, 622126–622925, 644–649, 65, length 16–19.
    private static bool IsDiscover(string d)
    {
        if (d.Length < 16 || d.Length > 19)
        {
            return false;
        }

        if (d.StartsWith("6011", StringComparison.Ordinal) || d.StartsWith("65", StringComparison.Ordinal))
        {
            return true;
        }

        if (d.Length >= 3)
        {
            int prefix3 = int.Parse(d[..3], System.Globalization.CultureInfo.InvariantCulture);
            if (prefix3 >= 644 && prefix3 <= 649)
            {
                return true;
            }
        }

        if (d.Length >= 6)
        {
            int prefix6 = int.Parse(d[..6], System.Globalization.CultureInfo.InvariantCulture);
            if (prefix6 >= 622126 && prefix6 <= 622925)
            {
                return true;
            }
        }

        return false;
    }

    // Diners Club: 300–305, 36, 38, length 14–19.
    private static bool IsDinersClub(string d)
    {
        if (d.Length < 14 || d.Length > 19)
        {
            return false;
        }

        if (d.StartsWith("36", StringComparison.Ordinal) || d.StartsWith("38", StringComparison.Ordinal))
        {
            return true;
        }

        if (d.Length >= 3)
        {
            int prefix3 = int.Parse(d[..3], System.Globalization.CultureInfo.InvariantCulture);
            return prefix3 >= 300 && prefix3 <= 305;
        }

        return false;
    }

    // JCB: 3528–3589, length 16–19.
    private static bool IsJcb(string d)
    {
        if (d.Length < 16 || d.Length > 19)
        {
            return false;
        }

        if (d.Length >= 4)
        {
            int prefix4 = int.Parse(d[..4], System.Globalization.CultureInfo.InvariantCulture);
            return prefix4 >= 3528 && prefix4 <= 3589;
        }

        return false;
    }

    // UnionPay: starts with 62, length 16–19.
    private static bool IsUnionPay(string d) =>
        d.Length >= 16 && d.Length <= 19 && d.StartsWith("62", StringComparison.Ordinal)
        && !IsDiscover(d); // Avoid overlap with Discover 622126–622925.

    // Maestro: 5018, 5020, 5038, 5893, 6304, 6759, 6761, 6762, 6763, length 12–19.
    private static bool IsMaestro(string d)
    {
        if (d.Length < 12 || d.Length > 19)
        {
            return false;
        }

        if (d.Length >= 4)
        {
            string prefix4 = d[..4];
            return prefix4 is "5018" or "5020" or "5038" or "5893" or "6304" or "6759" or "6761" or "6762" or "6763";
        }

        return false;
    }
}
