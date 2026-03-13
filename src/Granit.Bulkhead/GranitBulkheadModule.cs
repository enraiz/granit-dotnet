using Granit.Bulkhead.Extensions;
using Granit.Core.Modularity;
using Granit.ExceptionHandling;
using Granit.Features;
using Granit.Security;

namespace Granit.Bulkhead;

/// <summary>
/// Granit module for per-tenant bulkhead isolation.
/// Registers <see cref="Internal.ConcurrencyLimiterRegistry"/>, <see cref="Internal.TenantPartitionedBulkhead"/>,
/// and all required dependencies from configuration section <c>"Bulkhead"</c>.
/// </summary>
[DependsOn(
    typeof(GranitExceptionHandlingModule),
    typeof(GranitFeaturesModule),
    typeof(GranitSecurityModule))]
public sealed class GranitBulkheadModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitBulkhead();
}
