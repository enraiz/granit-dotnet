// =============================================================================
// UserSettingValueProvider - Provider de paramètre utilisateur (niveau U)
// =============================================================================
// Lit/écrit ISettingStore pour la portée utilisateur (providerKey = UserId).
// Retourne null si aucun utilisateur n'est authentifié dans le contexte courant.
// Cache les valeurs via ICacheService<SettingValue>.
// Précédence : User > Tenant > Global > Configuration > Default
//
// Inputs  : ICurrentUserService, ISettingStore, ICacheService<SettingValue>, SettingsOptions
// Outputs : SettingValue(Name, "U", userId, value) ou null si absent/non authentifié
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Options;
using DigitalDynamics.Foundation.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Provider de paramètres utilisateur (isolation par utilisateur courant via <see cref="ICurrentUserService"/>).
/// Cache les valeurs lues depuis <see cref="ISettingStore"/> (order = 100).
/// </summary>
public sealed class UserSettingValueProvider : ISettingValueProvider
{
    /// <summary>Identifiant du provider User.</summary>
    public const string ProviderName = "U";

    private readonly ICurrentUserService _currentUser;
    private readonly ISettingStore _store;
    private readonly ICacheService<SettingValue> _cache;
    private readonly IOptions<SettingsOptions> _options;

    public UserSettingValueProvider(
        ICurrentUserService currentUser,
        ISettingStore store,
        ICacheService<SettingValue> cache,
        IOptions<SettingsOptions> options)
    {
        _currentUser = currentUser;
        _store = store;
        _cache = cache;
        _options = options;
    }

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 100;

    /// <inheritdoc/>
    public async Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return null;
        }

        string userId = _currentUser.UserId;
        string cacheKey = SettingCacheKey.Build(ProviderName, userId, definition.Name);
        DistributedCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = _options.Value.CacheExpiration
        };

        return await _cache.GetOrAddAsync(
            cacheKey,
            async innerCt =>
            {
                SettingValue? stored = await _store.GetOrNullAsync(
                    definition.Name, ProviderName, userId, innerCt);
                return stored ?? new SettingValue(definition.Name, ProviderName, userId, null);
            },
            cacheOptions,
            ct) is { Value: not null } hit
            ? hit
            : null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return;
        }

        string userId = _currentUser.UserId;
        await _store.SetAsync(definition.Name, ProviderName, userId, value, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, userId, definition.Name), ct);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return;
        }

        string userId = _currentUser.UserId;
        await _store.DeleteAsync(definition.Name, ProviderName, userId, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, userId, definition.Name), ct);
    }
}
