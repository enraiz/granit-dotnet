using Granit.Authorization.Abstractions;
using Granit.Authorization.Endpoints.Permissions;
using Granit.Authorization.EntityFrameworkCore;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Authorization.Endpoints;

/// <summary>
/// Granit module for authorization management HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes permission management routes via
/// <see cref="Extensions.AuthorizationEndpointRouteBuilderExtensions.MapAuthorizationEndpoints"/>.
/// Requires both <see cref="GranitAuthorizationModule"/> (permission policy enforcement)
/// and <see cref="GranitAuthorizationEntityFrameworkCoreModule"/> (<see cref="Abstractions.IPermissionManager"/>).
/// </remarks>
[DependsOn(
    typeof(GranitAuthorizationModule),
    typeof(GranitAuthorizationEntityFrameworkCoreModule))]
public sealed class GranitAuthorizationEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            AuthorizationEndpointsPermissionDefinitionProvider>();
}
