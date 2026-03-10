using Granit.Authorization.Abstractions;
using Granit.Authorization.Endpoints.Permissions;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Authorization.Endpoints;

/// <summary>
/// Granit module for authorization management HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes permission management routes via
/// <see cref="Extensions.AuthorizationEndpointRouteBuilderExtensions.MapAuthorizationEndpoints"/>.
/// Requires <see cref="GranitAuthorizationModule"/> for permission policy enforcement.
/// The application host must register an implementation of
/// <see cref="Abstractions.IPermissionManagerReader"/>/<see cref="Abstractions.IPermissionManagerWriter"/>
/// (e.g. via <c>[DependsOn(typeof(GranitAuthorizationEntityFrameworkCoreModule))]</c>).
/// </remarks>
[DependsOn(typeof(GranitAuthorizationModule))]
public sealed class GranitAuthorizationEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            AuthorizationEndpointsPermissionDefinitionProvider>();
}
