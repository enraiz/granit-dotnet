namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Generates cryptographically secure API keys with typed prefixes.
/// </summary>
public interface IApiKeyGenerator
{
    /// <summary>
    /// Generates a new API key with a typed prefix based on <paramref name="type"/>
    /// and <paramref name="environment"/>.
    /// </summary>
    /// <param name="type">The kind of key (Secret, Publishable, Webhook, Ephemeral).</param>
    /// <param name="environment">Target environment (<c>live</c>, <c>test</c>, <c>dev</c>).</param>
    /// <returns>The generation result containing the raw secret (returned once), hash, and prefix.</returns>
    ApiKeyGenerationResult Generate(ApiKeyType type, string environment);
}

/// <summary>
/// Result of an API key generation. The <see cref="RawSecret"/> is shown to the
/// caller exactly once and must never be stored in plain text.
/// </summary>
/// <param name="RawSecret">The full key including prefix (e.g., <c>gk_live_sk_abc123...</c>).</param>
/// <param name="HashedKey">SHA-256 hex digest of <see cref="RawSecret"/> for persistent storage.</param>
/// <param name="Prefix">The typed prefix (e.g., <c>gk_live_sk_</c>).</param>
/// <param name="LastFourChars">Last four characters of <see cref="RawSecret"/> for identification.</param>
public sealed record ApiKeyGenerationResult(
    string RawSecret,
    string HashedKey,
    string Prefix,
    string LastFourChars);
