namespace Granit.Authentication.Keycloak.Options;

/// <summary>
/// Configuration options for Keycloak back-channel logout (OIDC Back-Channel Logout 1.0).
/// Nested under <see cref="KeycloakOptions.BackChannelLogout"/>.
/// </summary>
public sealed class BackChannelLogoutOptions
{
    /// <summary>Enable back-channel logout endpoint and session revocation checking. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; }

    /// <summary>Route path for the back-channel logout endpoint. Default: <c>"/auth/back-channel-logout"</c>.</summary>
    public string EndpointPath { get; set; } = "/auth/back-channel-logout";

    /// <summary>
    /// How long a revoked session identifier is kept in the distributed cache.
    /// Should be at least as long as the longest access-token lifetime.
    /// Default: 1 hour.
    /// </summary>
    public TimeSpan SessionRevocationTtl { get; set; } = TimeSpan.FromHours(1);
}
