using Granit.Authentication.ApiKeys.Domain;

namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Persistence abstraction for API keys. Implemented by the EF Core store.
/// </summary>
public interface IApiKeyStore
{
    /// <summary>
    /// Finds an active API key by its SHA-256 hash.
    /// Returns <c>null</c> if no active key matches.
    /// </summary>
    Task<ApiKeyEntry?> FindByHashAsync(string hashedKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the <see cref="ApiKeyEntry.LastUsedAt"/> timestamp for the given key.
    /// </summary>
    Task UpdateLastUsedAsync(Guid id, DateTimeOffset usedAt, CancellationToken cancellationToken = default);
}
