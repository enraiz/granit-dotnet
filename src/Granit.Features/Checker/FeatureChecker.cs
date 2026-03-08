using Granit.Core.MultiTenancy;
using Granit.Features.Cache;
using Granit.Features.Definitions;
using Granit.Features.Exceptions;
using Granit.Features.ValueProviders;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Features.Checker;

/// <summary>
/// Resolves feature values using the Tenant → Plan → Default cascade,
/// backed by <see cref="HybridCache"/> (L1 in-process + L2 Redis).
/// </summary>
internal sealed class FeatureChecker(
    IFeatureDefinitionStore definitionStore,
    IEnumerable<IFeatureValueProvider> valueProviders,
    IServiceProvider serviceProvider,
    HybridCache hybridCache) : IFeatureChecker
{
    private readonly IFeatureDefinitionStore _definitionStore = definitionStore;
    private readonly IReadOnlyList<IFeatureValueProvider> _providers =
        [.. valueProviders.OrderBy(p => p.Order)];
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly HybridCache _hybridCache = hybridCache;

    /// <inheritdoc/>
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        string value = await GetValueAsync(featureName, cancellationToken).ConfigureAwait(false);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task<long> GetNumericAsync(string featureName, CancellationToken cancellationToken = default)
    {
        string value = await GetValueAsync(featureName, cancellationToken).ConfigureAwait(false);
        return long.TryParse(value, out long parsed) ? parsed : 0L;
    }

    /// <inheritdoc/>
    public async Task<string> GetValueAsync(string featureName, CancellationToken cancellationToken = default)
    {
        FeatureDefinition definition = _definitionStore.GetRequired(featureName);
        ICurrentTenant? currentTenant = _serviceProvider.GetService<ICurrentTenant>();
        Guid? tenantId = currentTenant?.IsAvailable == true ? currentTenant.Id : null;
        string cacheKey = FeatureCacheKey.Build(tenantId, featureName);

        string resolved = await _hybridCache.GetOrCreateAsync<string>(
            cacheKey,
            async innerCt =>
            {
                string? value = await ResolveAsync(definition, innerCt).ConfigureAwait(false);
                return value ?? definition.DefaultValue;
            },
            cancellationToken: cancellationToken);

        return resolved;
    }

    /// <inheritdoc/>
    public async Task RequireEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        bool enabled = await IsEnabledAsync(featureName, cancellationToken).ConfigureAwait(false);
        if (!enabled)
        {
            throw new FeatureNotEnabledException(featureName);
        }
    }

    private async Task<string?> ResolveAsync(FeatureDefinition definition, CancellationToken cancellationToken)
    {
        foreach (IFeatureValueProvider provider in _providers)
        {
            string? value = await provider.GetOrNullAsync(definition, cancellationToken).ConfigureAwait(false);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }
}
