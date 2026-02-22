// =============================================================================
// FoundationPersistenceModule - Module Foundation pour EF Core interceptors
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Guids;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Persistence.Extensions;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;

namespace DigitalDynamics.Foundation.Persistence;

/// <summary>
/// Module Foundation pour les intercepteurs EF Core (audit HDS + soft delete RGPD).
/// Dépend de Timing (IClock), Guids (IGuidGenerator), Security (ICurrentUserService)
/// et MultiTenancy (ICurrentTenant) utilisés par les intercepteurs au runtime.
/// </summary>
[DependsOn(
    typeof(FoundationTimingModule),
    typeof(FoundationGuidsModule),
    typeof(FoundationSecurityModule),
    typeof(FoundationMultiTenancyModule))]
public sealed class FoundationPersistenceModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationPersistence();
}
