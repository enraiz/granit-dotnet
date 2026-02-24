using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.Localization.Endpoints.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Localization.Endpoints;

/// <summary>
/// Granit module for localization HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes two sets of endpoints:
/// <list type="bullet">
/// <item><c>GET /api/granit/localization</c> — anonymous SPA bootstrapping, via
///   <see cref="Extensions.LocalizationEndpointRouteBuilderExtensions.MapGranitLocalization"/>.</item>
/// <item>CRUD <c>/api/granit/localization/overrides</c> — admin override management
///   (requires <c>Localization.Overrides.Manage</c> permission), via
///   <see cref="Extensions.LocalizationEndpointRouteBuilderExtensions.MapGranitLocalizationOverrides"/>.</item>
/// </list>
/// </remarks>
[DependsOn(
    typeof(GranitLocalizationModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitLocalizationEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            LocalizationOverridesPermissionDefinitionProvider>();
}
