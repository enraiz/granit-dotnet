namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Controls how an API key is cached by the authentication handler.
/// </summary>
public enum CacheBehavior
{
    /// <summary>Standard caching with configurable TTL (default).</summary>
    Normal,

    /// <summary>No caching — always lookup from the store. Use for ISO 27001-critical keys.</summary>
    NoCache,
}
