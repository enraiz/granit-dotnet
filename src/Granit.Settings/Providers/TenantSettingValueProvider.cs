using Granit.Caching;
using Granit.MultiTenancy;
using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Granit.Settings.Providers;

/// <summary>
/// Tenant settings provider (isolated per current tenant via <see cref="ICurrentTenant"/>).
/// Caches values read from <see cref="ISettingStore"/> (order = 200).
/// </summary>
public sealed class TenantSettingValueProvider(
    ICurrentTenant currentTenant,
    ISettingStore store,
    ICacheService<SettingValue> cache,
    IOptions<SettingsOptions> options) : ISettingValueProvider
{
    /// <summary>Tenant provider identifier.</summary>
    public const string ProviderName = "T";

    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ISettingStore _store = store;
    private readonly ICacheService<SettingValue> _cache = cache;
    private readonly IOptions<SettingsOptions> _options = options;

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 200;

    /// <inheritdoc/>
    public async Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return null;
        }

        string tenantKey = _currentTenant.Id!.Value.ToString();
        string cacheKey = SettingCacheKey.Build(ProviderName, tenantKey, definition.Name);
        DistributedCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = _options.Value.CacheExpiration
        };

        return await _cache.GetOrAddAsync(
            cacheKey,
            async innerCt =>
            {
                SettingValue? stored = await _store.GetOrNullAsync(
                    definition.Name, ProviderName, tenantKey, innerCt);
                return stored ?? new SettingValue(definition.Name, ProviderName, tenantKey, null);
            },
            cacheOptions,
            ct) is { Value: not null } hit
            ? hit
            : null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return;
        }

        string tenantKey = _currentTenant.Id!.Value.ToString();
        await _store.SetAsync(definition.Name, ProviderName, tenantKey, value, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, tenantKey, definition.Name), ct);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return;
        }

        string tenantKey = _currentTenant.Id!.Value.ToString();
        await _store.DeleteAsync(definition.Name, ProviderName, tenantKey, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, tenantKey, definition.Name), ct);
    }
}
