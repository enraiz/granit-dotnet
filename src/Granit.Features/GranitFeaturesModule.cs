using Granit.Caching.Hybrid;
using Granit.Core.Modularity;

namespace Granit.Features;

/// <summary>
/// Granit module for SaaS feature management.
/// </summary>
/// <remarks>
/// Provides plan-based feature activation with multi-level resolution:
/// Tenant override → Plan value → Default (code).
/// <para>
/// Values are cached via <c>IHybridCache</c> (L1 in-process + L2 Redis)
/// with per-tenant invalidation via <see cref="Events.FeatureValueChangedEvent"/>.
/// </para>
/// <para>
/// The application must implement <see cref="Plans.IPlanIdProvider"/> and
/// <see cref="Plans.IPlanFeatureStore"/> to activate plan-level resolution.
/// Without these, features fall back to their default values.
/// </para>
/// <para>
/// Multi-tenancy support is optional. If <c>GranitMultiTenancyModule</c> is registered,
/// tenant-level feature overrides are resolved automatically. Without it, the cascade
/// falls through Plan → Default with no DI error.
/// </para>
/// </remarks>
[DependsOn(typeof(GranitCachingHybridModule))]
public sealed class GranitFeaturesModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitFeatures();
}
