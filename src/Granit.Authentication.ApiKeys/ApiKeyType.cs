namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Defines the type of API key, controlling the prefix and default permissions.
/// </summary>
public enum ApiKeyType
{
    /// <summary>Secret key for server-to-server M2M authentication (<c>gk_{env}_sk_</c>).</summary>
    Secret,

    /// <summary>Publishable key for client-side usage, read-only (<c>gk_{env}_pk_</c>).</summary>
    Publishable,

    /// <summary>Webhook signing key for HMAC verification of inbound callbacks (<c>gk_{env}_wh_</c>).</summary>
    Webhook,

    /// <summary>Ephemeral short-lived key for workflows and background jobs (<c>gk_{env}_ep_</c>).</summary>
    Ephemeral,
}
