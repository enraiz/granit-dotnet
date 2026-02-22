// =============================================================================
// SettingManager - Implementation of ISettingManager
// =============================================================================
// Writes directly to ISettingStore (bypassing providers) with explicit keys,
// then invalidates the corresponding cache entry.
//
// Inputs  : ISettingStore, ICacheService<SettingValue>, SettingDefinitionManager
// Outputs : void (side effects: store + cache)
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Services;

/// <summary>
/// Implementation of <see cref="ISettingManager"/>: writes to <see cref="ISettingStore"/>
/// and invalidates the cache for the Global, Tenant, and User scopes.
/// </summary>
public sealed class SettingManager : ISettingManager
{
    private readonly ISettingStore _store;
    private readonly ICacheService<SettingValue> _cache;
    private readonly SettingDefinitionManager _definitions;

    public SettingManager(
        ISettingStore store,
        ICacheService<SettingValue> cache,
        SettingDefinitionManager definitions)
    {
        _store = store;
        _cache = cache;
        _definitions = definitions;
    }

    /// <inheritdoc/>
    public async Task SetGlobalAsync(string name, string? value, CancellationToken ct = default)
    {
        _definitions.Get(name); // Validates that the setting is declared
        await _store.SetAsync(name, GlobalSettingValueProvider.ProviderName, null, value, ct);
        await _cache.RemoveAsync(
            SettingCacheKey.Build(GlobalSettingValueProvider.ProviderName, null, name), ct);
    }

    /// <inheritdoc/>
    public async Task SetForTenantAsync(Guid tenantId, string name, string? value, CancellationToken ct = default)
    {
        _definitions.Get(name);
        string tenantKey = tenantId.ToString();
        await _store.SetAsync(name, TenantSettingValueProvider.ProviderName, tenantKey, value, ct);
        await _cache.RemoveAsync(
            SettingCacheKey.Build(TenantSettingValueProvider.ProviderName, tenantKey, name), ct);
    }

    /// <inheritdoc/>
    public async Task SetForUserAsync(string userId, string name, string? value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _definitions.Get(name);
        await _store.SetAsync(name, UserSettingValueProvider.ProviderName, userId, value, ct);
        await _cache.RemoveAsync(
            SettingCacheKey.Build(UserSettingValueProvider.ProviderName, userId, name), ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey = null,
        CancellationToken ct = default)
    {
        await _store.DeleteAsync(name, providerName, providerKey, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(providerName, providerKey, name), ct);
    }
}
