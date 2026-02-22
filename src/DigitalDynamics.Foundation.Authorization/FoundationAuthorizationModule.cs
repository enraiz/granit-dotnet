using DigitalDynamics.Foundation.Authorization.Extensions;
using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Security;

namespace DigitalDynamics.Foundation.Authorization;

/// <summary>
/// Foundation module for RBAC permission management.
/// Registers permission definitions, the dynamic ASP.NET Core policy provider,
/// and the caching-aware permission checker.
/// </summary>
[DependsOn(
    typeof(FoundationSecurityModule),
    typeof(FoundationMultiTenancyModule),
    typeof(FoundationCachingModule))]
public sealed class FoundationAuthorizationModule : FoundationModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationAuthorization(context.Configuration);
}
