// =============================================================================
// FoundationPersistenceModule - Module Foundation pour EF Core interceptors
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Guids;
using DigitalDynamics.Foundation.Persistence.Extensions;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;

namespace DigitalDynamics.Foundation.Persistence;

/// <summary>
/// Module Foundation pour les intercepteurs EF Core (audit HDS + soft delete RGPD).
/// Depend de Timing (IClock), Guids (IGuidGenerator) et Security (ICurrentUserService)
/// utilises par les interceptors au runtime.
/// </summary>
[DependsOn(
    typeof(FoundationTimingModule),
    typeof(FoundationGuidsModule),
    typeof(FoundationSecurityModule))]
public sealed class FoundationPersistenceModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationPersistence();
}
