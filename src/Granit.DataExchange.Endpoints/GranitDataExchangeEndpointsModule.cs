using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.DataExchange.Endpoints.Permissions;
using Granit.DataExchange.Endpoints.Validators;
using Granit.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataExchange.Endpoints;

/// <summary>
/// Granit module for data import HTTP endpoints.
/// </summary>
/// <remarks>
/// Exposes import management routes via
/// <see cref="Extensions.DataExchangeEndpointRouteBuilderExtensions.MapDataExchangeEndpoints"/>.
/// Requires both <see cref="GranitDataExchangeModule"/> (pipeline infrastructure)
/// and <see cref="GranitAuthorizationModule"/> (permission policy enforcement).
/// </remarks>
[DependsOn(
    typeof(GranitDataExchangeModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitDataExchangeEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            DataExchangePermissionDefinitionProvider>();
        context.Services.AddGranitValidatorsFromAssemblyContaining<ConfirmMappingsRequestValidator>();
    }
}
