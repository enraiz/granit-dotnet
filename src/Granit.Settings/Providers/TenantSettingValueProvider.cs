using Granit.Caching;
using Granit.Core.MultiTenancy;
using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Granit.Settings.Providers;

/// <summary>
/// Tenant settings provider (isolated per current tenant via <see cref="ICurrentTenant"/>).
/// Caches values read from <see cref="ISettingStoreReader"/> (order = 200).
/// </summary>
public sealed class TenantSettingValueProvider(
    ICurrentTenant currentTenant,
    ISettingStoreReader storeReader,
    ISettingStoreWriter storeWriter,
    ICacheService<SettingValue> cache,
    IOptions<SettingsOptions> options) : ISettingValueProvider
{
    /// <summary>Tenant provider identifier.</summary>
    public const string ProviderName = "T";

    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ISettingStoreReader _storeReader = storeReader;
    private readonly ISettingStoreWriter _storeWriter = storeWriter;
    private readonly ICacheService<SettingValue> _cache = cache;
    private readonly IOptions<SettingsOptions> _options = options;

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 200;

    /// <inheritdoc/>
    public async Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
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
                SettingValue? stored = await _storeReader.GetOrNullAsync(
                    definition.Name, ProviderName, tenantKey, innerCt).ConfigureAwait(false);
                return stored ?? new SettingValue(definition.Name, ProviderName, tenantKey, null);
            },
            cacheOptions,
            cancellationToken) is { Value: not null } hit
            ? hit
            : null;
    }

    /// <inheritdoc/>
    public async Task SetAsync(SettingDefinition definition, string? value, CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return;
        }

        string tenantKey = _currentTenant.Id!.Value.ToString();
        await _storeWriter.SetAsync(definition.Name, ProviderName, tenantKey, value, cancellationToken).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, tenantKey, definition.Name), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return;
        }

        string tenantKey = _currentTenant.Id!.Value.ToString();
        await _storeWriter.DeleteAsync(definition.Name, ProviderName, tenantKey, cancellationToken).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, tenantKey, definition.Name), cancellationToken).ConfigureAwait(false);
    }
}
