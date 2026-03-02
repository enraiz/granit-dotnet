using Granit.Caching;
using Granit.Security;
using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Values;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Granit.Settings.Providers;

/// <summary>
/// User settings provider (isolated per current user via <see cref="ICurrentUserService"/>).
/// Caches values read from <see cref="ISettingStore"/> (order = 100).
/// </summary>
public sealed class UserSettingValueProvider(
    ICurrentUserService currentUser,
    ISettingStore store,
    ICacheService<SettingValue> cache,
    IOptions<SettingsOptions> options) : ISettingValueProvider
{
    /// <summary>User provider identifier.</summary>
    public const string ProviderName = "U";

    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ISettingStore _store = store;
    private readonly ICacheService<SettingValue> _cache = cache;
    private readonly IOptions<SettingsOptions> _options = options;

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
                    definition.Name, ProviderName, userId, innerCt).ConfigureAwait(false);
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
        await _store.SetAsync(definition.Name, ProviderName, userId, value, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, userId, definition.Name), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            return;
        }

        string userId = _currentUser.UserId;
        await _store.DeleteAsync(definition.Name, ProviderName, userId, ct).ConfigureAwait(false);
        await _cache.RemoveAsync(SettingCacheKey.Build(ProviderName, userId, definition.Name), ct).ConfigureAwait(false);
    }
}
