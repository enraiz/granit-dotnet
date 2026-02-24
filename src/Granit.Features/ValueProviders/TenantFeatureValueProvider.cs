using Granit.Features.Definitions;
using Granit.Features.Store;
using Granit.MultiTenancy;

namespace Granit.Features.ValueProviders;

/// <summary>
/// Resolves tenant-specific feature overrides from <see cref="IFeatureStore"/>.
/// Runs first in the cascade (order = 100) — highest priority.
/// </summary>
/// <remarks>
/// Returns <c>null</c> when no tenant context is available (host / global requests).
/// </remarks>
internal sealed class TenantFeatureValueProvider(
    ICurrentTenant currentTenant,
    IFeatureStore featureStore) : IFeatureValueProvider
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IFeatureStore _featureStore = featureStore;

    /// <inheritdoc/>
    public string Name => "Tenant";

    /// <inheritdoc/>
    public int Order => 100;

    /// <inheritdoc/>
    public async Task<string?> GetOrNullAsync(FeatureDefinition definition, CancellationToken ct = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return null;
        }

        string tenantId = _currentTenant.Id!.Value.ToString();
        return await _featureStore.GetOrNullAsync(definition.Name, tenantId, ct);
    }
}
