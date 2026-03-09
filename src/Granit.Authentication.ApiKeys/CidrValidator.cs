using System.Net;

namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Validates whether an IP address falls within a set of allowed CIDR ranges.
/// Uses <see cref="IPNetwork"/> (native .NET 8+).
/// </summary>
public static class CidrValidator
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="ipAddress"/> is contained in at least
    /// one of the <paramref name="allowedCidrs"/> ranges.
    /// Returns <c>true</c> if <paramref name="allowedCidrs"/> is empty (no restriction).
    /// </summary>
    public static bool IsAllowed(IPAddress? ipAddress, IReadOnlyList<string> allowedCidrs)
    {
        if (allowedCidrs is not { Count: > 0 })
        {
            return true;
        }

        if (ipAddress is null)
        {
            return false;
        }

        for (int i = 0; i < allowedCidrs.Count; i++)
        {
            if (IPNetwork.TryParse(allowedCidrs[i], out IPNetwork network) && network.Contains(ipAddress))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that <paramref name="cidr"/> is a valid CIDR notation string.
    /// </summary>
    public static bool IsValidCidr(string cidr) => IPNetwork.TryParse(cidr, out _);
}
