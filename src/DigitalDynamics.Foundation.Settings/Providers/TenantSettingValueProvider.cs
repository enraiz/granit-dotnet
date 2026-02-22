// =============================================================================
// TenantSettingValueProvider - Tenant setting provider (level T)
// =============================================================================
// Reads/writes ISettingStore for the tenant scope (providerKey = tenantId.ToString()).
// Returns null when no tenant is active in the current context.
// Caches values via ICacheService<SettingValue>.
// Precedence: User > Tenant > Global > Configuration > Default
//
// Inputs  : ICurrentTenant, ISettingStore, ICacheService<SettingValue>, SettingsOptions
// Outputs : SettingValue(Name, "T", tenantId, value) or null if absent/outside tenant
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Options;
using DigitalDynamics.Foundation.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Tenant settings provider (isolated per current tenant via <see cref="ICurrentTenant"/>).
/// Caches values read from <see cref="ISettingStore"/> (order = 200).
/// </summary>
public sealed class TenantSettingValueProvider : ISettingValueProvider
{
    /// <summary>Tenant provider identifier.</summary>
    public const string ProviderName = "T";

    private readonly ICurrentTenant _currentTenant;
    private readonly ISettingStore _store;
    private readonly ICacheService<SettingValue> _cache;
    private readonly IOptions<SettingsOptions> _options;

    public TenantSettingValueProvider(
        ICurrentTenant currentTenant,
        ISettingStore store,
        ICacheService<SettingValue> cache,
        IOptions<SettingsOptions> options)
    {
        _currentTenant = currentTenant;
        _store = store;
        _cache = cache;
        _options = options;
    }

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
