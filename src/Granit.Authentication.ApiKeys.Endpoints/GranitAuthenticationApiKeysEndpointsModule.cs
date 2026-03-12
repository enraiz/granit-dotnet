using Granit.Authentication.ApiKeys.Endpoints.Permissions;
using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Authentication.ApiKeys.Endpoints;

/// <summary>
/// Module for API key management endpoints.
/// </summary>
[DependsOn(
    typeof(GranitAuthenticationApiKeysModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitAuthenticationApiKeysEndpointsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            ApiKeyPermissionDefinitionProvider>();
    }
}
