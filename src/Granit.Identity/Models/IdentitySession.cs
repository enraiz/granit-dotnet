namespace Granit.Identity.Models;

/// <summary>
/// Represents an active SSO session for a user in an external identity provider.
/// </summary>
/// <param name="SessionId">Unique session identifier in the identity provider.</param>
/// <param name="IpAddress">IP address of the client that initiated the session.</param>
/// <param name="StartedAt">When the session was created.</param>
/// <param name="LastAccess">When the session was last used.</param>
/// <param name="RememberMe">Whether the session was created with a "remember me" flag.</param>
/// <param name="Clients">Names of the clients (applications) participating in this session.</param>
public sealed record IdentitySession(
    string SessionId,
    string? IpAddress,
    DateTimeOffset StartedAt,
    DateTimeOffset LastAccess,
    bool RememberMe,
    IReadOnlyList<string> Clients);
