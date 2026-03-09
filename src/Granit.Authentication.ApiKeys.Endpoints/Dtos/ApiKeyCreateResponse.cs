namespace Granit.Authentication.ApiKeys.Endpoints.Dtos;

/// <summary>
/// Response returned after creating a new API key.
/// Contains the raw secret that is shown exactly once.
/// </summary>
/// <param name="Id">The unique identifier of the created key.</param>
/// <param name="RawSecret">The full API key (shown once — must be stored securely by the caller).</param>
/// <param name="Prefix">The key prefix for identification (e.g., <c>gk_live_sk_</c>).</param>
/// <param name="LastFourChars">Last four characters for display purposes.</param>
/// <param name="Name">Display name of the key.</param>
/// <param name="Type">Key type.</param>
/// <param name="Environment">Target environment.</param>
/// <param name="ExpiresAt">Expiration date, if set.</param>
public sealed record ApiKeyCreateResponse(
    Guid Id,
    string RawSecret,
    string Prefix,
    string LastFourChars,
    string Name,
    ApiKeyType Type,
    string Environment,
    DateTimeOffset? ExpiresAt);
