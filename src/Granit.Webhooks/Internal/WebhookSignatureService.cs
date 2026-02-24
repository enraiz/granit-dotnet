using System.Security.Cryptography;
using System.Text;

namespace Granit.Webhooks.Internal;

/// <summary>
/// Computes the <c>x-granit-signature</c> header value using HMAC-SHA256 in Stripe format.
/// </summary>
/// <remarks>
/// <para>
/// Format: <c>t=&lt;unix_timestamp&gt;,v1=&lt;hmac_sha256_hex&gt;</c>
/// </para>
/// <para>
/// The signed payload is <c>"&lt;unix_timestamp&gt;.&lt;body_json&gt;"</c>.
/// The timestamp component allows receivers to reject replayed requests older than a
/// configurable window (recommended: 5 minutes on the client side).
/// </para>
/// <para>
/// Reference: <see href="https://stripe.com/docs/webhooks#verify-official-libraries"/>
/// </para>
/// </remarks>
internal static class WebhookSignatureService
{
    /// <summary>
    /// Computes the <c>x-granit-signature</c> header value.
    /// </summary>
    /// <param name="plainSecret">The plain-text signing secret (after unprotection).</param>
    /// <param name="timestamp">The timestamp to embed in the signature.</param>
    /// <param name="bodyJson">The serialized webhook envelope body.</param>
    /// <returns>The header value in the format <c>t=&lt;unix&gt;,v1=&lt;hex&gt;</c>.</returns>
    internal static string Compute(string plainSecret, DateTimeOffset timestamp, string bodyJson)
    {
        long unixSeconds = timestamp.ToUnixTimeSeconds();
        string toSign = $"{unixSeconds}.{bodyJson}";
        byte[] hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(plainSecret),
            Encoding.UTF8.GetBytes(toSign));
        return $"t={unixSeconds},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Computes the SHA-256 hex digest of a string, used for the HDS payload hash.
    /// </summary>
    internal static string ComputePayloadHash(string bodyJson) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bodyJson))).ToLowerInvariant();
}
