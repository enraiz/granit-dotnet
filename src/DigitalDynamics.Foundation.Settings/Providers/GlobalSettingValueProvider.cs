// =============================================================================
// GlobalSettingValueProvider - Global setting provider (level G)
// =============================================================================
// Reads/writes ISettingStore for the global scope (providerKey = null).
// Caches values via ICacheService<SettingValue> to avoid round-trips to the database.
// Precedence: User > Tenant > Global > Configuration > Default
//
// Inputs  : ISettingStore, ICacheService<SettingValue>, SettingsOptions
// Outputs : SettingValue(Name, "G", null, value) or null if absent
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Options;
using DigitalDynamics.Foundation.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Global settings provider (application scope, no tenant/user isolation).
/// Caches values read from <see cref="ISettingStore"/> (order = 300).
/// </summary>
public sealed class GlobalSettingValueProvider : ISettingValueProvider
{
    /// <summary>Global provider identifier.</summary>
    public const string ProviderName = "G";

    private readonly ISettingStore _store;
    private readonly ICacheService<SettingValue> _cache;
    private readonly IOptions<SettingsOptions> _options;

    public GlobalSettingValueProvider(
        ISettingStore store,
        ICacheService<SettingValue> cache,
        IOptions<SettingsOptions> options)
    {
        _store = store;
        _cache = cache;
        _options = options;
    }

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 300;

    /// <inheritdoc/>
    public async Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        string cacheKey = SettingCacheKey.Build(ProviderName, null, definition.Name);
        DistributedCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = _options.Value.CacheExpiration
        };

        return await _cache.GetOrAddAsync(
            cacheKey,
            async innerCt =>
            {
                SettingValue? stored = await _store.GetOrNullAsync(
                    definition.Name, ProviderName, null, innerCt);
                // Sentinel to distinguish "stored null" from "absent from cache"
                return stored ?? new SettingValue(definition.Name, ProviderName, null, null);
            },
            cacheOptions,
            ct) is { Value: not null } hit
            ? hit
            : null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default)
    {
        await _store.SetAsync(definition.Name, ProviderName, null, value, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, null, definition.Name), ct);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        await _store.DeleteAsync(definition.Name, ProviderName, null, ct);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, null, definition.Name), ct);
    }
}
