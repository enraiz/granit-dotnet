using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.Settings.Definitions;
using Granit.Settings.Endpoints.Internal;
using Granit.Settings.Endpoints.Permissions;
using Granit.Settings.Endpoints.Validators;
using Granit.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Settings.Endpoints;

/// <summary>
/// Granit module for settings HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes three sets of endpoints:
/// <list type="bullet">
/// <item>User-scoped settings — any authenticated user can read/write their own settings
///   (<see cref="Extensions.UserSettingsEndpointRouteBuilderExtensions.MapGranitUserSettings"/>).</item>
/// <item>Global settings — requires <c>Settings.Global.Read</c>/<c>Settings.Global.Manage</c>
///   (<see cref="Extensions.AdminSettingsEndpointRouteBuilderExtensions.MapGranitGlobalSettings"/>).</item>
/// <item>Tenant settings — requires <c>Settings.Tenant.Read</c>/<c>Settings.Tenant.Manage</c>
///   (<see cref="Extensions.AdminSettingsEndpointRouteBuilderExtensions.MapGranitTenantSettings"/>).</item>
/// </list>
/// Also registers the <see cref="Middleware.SettingsCultureMiddleware"/> setting definitions
/// for locale and timezone.
/// </remarks>
[DependsOn(
    typeof(GranitSettingsModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitSettingsEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISettingDefinitionProvider, WellKnownSettingDefinitionProvider>();
        context.Services.AddSingleton<IPermissionDefinitionProvider, SettingsPermissionDefinitionProvider>();
        context.Services.AddGranitValidatorsFromAssemblyContaining<UpdateSettingValueRequestValidator>();
    }
}
