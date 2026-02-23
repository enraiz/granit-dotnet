using Granit.Authorization.Extensions;
using Granit.Caching;
using Granit.Core.Modularity;
using Granit.MultiTenancy;
using Granit.Security;

namespace Granit.Authorization;

/// <summary>
/// Granit module for RBAC permission management.
/// Registers permission definitions, the dynamic ASP.NET Core policy provider,
/// and the caching-aware permission checker.
/// </summary>
[DependsOn(
    typeof(GranitSecurityModule),
    typeof(GranitMultiTenancyModule),
    typeof(GranitCachingModule))]
public sealed class GranitAuthorizationModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitAuthorization(context.Configuration);
}
