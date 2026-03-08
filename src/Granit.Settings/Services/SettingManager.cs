using Granit.Caching;
using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Values;

namespace Granit.Settings.Services;

/// <summary>
/// Implementation of <see cref="ISettingManager"/>: writes to <see cref="ISettingStoreWriter"/>
/// and invalidates the cache for the Global, Tenant, and User scopes.
/// </summary>
public sealed class SettingManager(
    ISettingStoreWriter storeWriter,
    ICacheService<SettingValue> cache,
    SettingDefinitionManager definitions) : ISettingManager
{
    private readonly ISettingStoreWriter _storeWriter = storeWriter;
    private readonly ICacheService<SettingValue> _cache = cache;
    private readonly SettingDefinitionManager _definitions = definitions;

    /// <inheritdoc/>
    public async Task SetGlobalAsync(string name, string? value, CancellationToken ct = default)
    {
        _definitions.Get(name); // Validates that the setting is declared
        await _storeWriter.SetAsync(name, GlobalSettingValueProvider.ProviderName, null, value, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(
            SettingCacheKey.Build(GlobalSettingValueProvider.ProviderName, null, name), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetForTenantAsync(Guid tenantId, string name, string? value, CancellationToken ct = default)
    {
        _definitions.Get(name);
        string tenantKey = tenantId.ToString();
        await _storeWriter.SetAsync(name, TenantSettingValueProvider.ProviderName, tenantKey, value, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(
            SettingCacheKey.Build(TenantSettingValueProvider.ProviderName, tenantKey, name), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetForUserAsync(string userId, string name, string? value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _definitions.Get(name);
        await _storeWriter.SetAsync(name, UserSettingValueProvider.ProviderName, userId, value, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(
            SettingCacheKey.Build(UserSettingValueProvider.ProviderName, userId, name), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey = null,
        CancellationToken ct = default)
    {
        await _storeWriter.DeleteAsync(name, providerName, providerKey, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(providerName, providerKey, name), ct).ConfigureAwait(false);
    }
}
