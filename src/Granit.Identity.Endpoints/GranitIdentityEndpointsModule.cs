using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.Identity.Endpoints.Permissions;
using Granit.Identity.Endpoints.Validators;
using Granit.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Identity.Endpoints;

/// <summary>
/// Granit module for identity user cache Minimal API endpoints.
/// </summary>
/// <remarks>
/// <para>
/// Depends only on <see cref="GranitIdentityModule"/> (abstractions) and
/// <see cref="GranitAuthorizationModule"/> (permission policy enforcement).
/// Does <b>not</b> depend on <c>Granit.Identity.EntityFrameworkCore</c> —
/// the host application is responsible for registering the EF Core implementation.
/// </para>
/// <para>
/// Exposes user cache management routes via
/// <see cref="Extensions.IdentityEndpointRouteBuilderExtensions.MapIdentityUserCacheEndpoints"/>.
/// </para>
/// </remarks>
[DependsOn(
    typeof(GranitIdentityModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitIdentityEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            IdentityPermissionDefinitionProvider>();
        context.Services.AddGranitValidatorsFromAssemblyContaining<IdentityUserCacheListRequestValidator>();
    }
}
