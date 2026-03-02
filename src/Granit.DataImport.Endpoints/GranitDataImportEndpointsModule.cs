using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.DataImport.Endpoints.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport.Endpoints;

/// <summary>
/// Granit module for data import HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes import management routes via
/// <see cref="Extensions.DataImportEndpointRouteBuilderExtensions.MapDataImportEndpoints"/>.
/// Requires both <see cref="GranitDataImportModule"/> (pipeline infrastructure)
/// and <see cref="GranitAuthorizationModule"/> (permission policy enforcement).
/// </remarks>
[DependsOn(
    typeof(GranitDataImportModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitDataImportEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            DataImportPermissionDefinitionProvider>();
}
