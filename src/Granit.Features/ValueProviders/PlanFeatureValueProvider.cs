using Granit.Features.Definitions;
using Granit.Features.Plans;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Features.ValueProviders;

/// <summary>
/// Resolves feature values from the commercial plan associated with the current context.
/// Runs second in the cascade (order = 200).
/// </summary>
/// <remarks>
/// Requires <see cref="IPlanIdProvider"/> and <see cref="IPlanFeatureStore"/> to be
/// registered by the application. If either is absent, this provider silently returns
/// <c>null</c> and the cascade falls through to <see cref="DefaultValueFeatureValueProvider"/>.
/// </remarks>
internal sealed class PlanFeatureValueProvider(IServiceProvider serviceProvider) : IFeatureValueProvider
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc/>
    public string Name => "Plan";

    /// <inheritdoc/>
    public int Order => 200;

    /// <inheritdoc/>
    public async Task<string?> GetOrNullAsync(FeatureDefinition definition, CancellationToken ct = default)
    {
        IPlanIdProvider? planIdProvider = _serviceProvider.GetService<IPlanIdProvider>();
        IPlanFeatureStore? planFeatureStore = _serviceProvider.GetService<IPlanFeatureStore>();

        if (planIdProvider is null || planFeatureStore is null)
        {
            return null;
        }

        string? planId = await planIdProvider.GetCurrentPlanIdAsync(ct);
        if (planId is null)
        {
            return null;
        }

        return await planFeatureStore.GetOrNullAsync(planId, definition.Name, ct);
    }
}
