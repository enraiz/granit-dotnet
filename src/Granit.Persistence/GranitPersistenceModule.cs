using Granit.Core.Modularity;
using Granit.Guids;
using Granit.MultiTenancy;
using Granit.Persistence.Extensions;
using Granit.Security;
using Granit.Timing;

namespace Granit.Persistence;

/// <summary>
/// Module Granit pour les intercepteurs EF Core (audit HDS + soft delete RGPD).
/// Dépend de Timing (IClock), Guids (IGuidGenerator), Security (ICurrentUserService)
/// et MultiTenancy (ICurrentTenant) utilisés par les intercepteurs au runtime.
/// </summary>
[DependsOn(
    typeof(GranitTimingModule),
    typeof(GranitGuidsModule),
    typeof(GranitSecurityModule),
    typeof(GranitMultiTenancyModule))]
public sealed class GranitPersistenceModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitPersistence();
}
