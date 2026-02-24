using Granit.Core.Modularity;
using Granit.Guids;
using Granit.Persistence.Extensions;
using Granit.Security;
using Granit.Timing;

namespace Granit.Persistence;

/// <summary>
/// Module Granit pour les intercepteurs EF Core (audit HDS + soft delete RGPD).
/// Dépend de Timing (IClock), Guids (IGuidGenerator) et Security (ICurrentUserService).
/// ICurrentTenant est résolu via Granit.Core.MultiTenancy — Granit.MultiTenancy
/// n'est pas une dépendance directe de ce module.
/// </summary>
[DependsOn(
    typeof(GranitTimingModule),
    typeof(GranitGuidsModule),
    typeof(GranitSecurityModule))]
public sealed class GranitPersistenceModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitPersistence();
}
