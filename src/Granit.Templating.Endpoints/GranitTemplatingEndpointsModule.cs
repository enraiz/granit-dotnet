using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.Templating.Endpoints.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Templating.Endpoints;

/// <summary>
/// Granit module for template administration HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes CRUD endpoints for template draft management under
/// <c>/api/v1/admin/templates</c>, protected by the <c>Templates.Manage</c> permission.
/// <para>
/// The host application must call
/// <see cref="Extensions.TemplatingEndpointRouteBuilderExtensions.MapGranitTemplatingAdmin"/>
/// to register the routes.
/// </para>
/// </remarks>
[DependsOn(
    typeof(GranitTemplatingModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitTemplatingEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            TemplatingPermissionDefinitionProvider>();
}
