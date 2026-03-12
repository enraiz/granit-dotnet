using Granit.Authentication.JwtBearer;
using Granit.Authentication.Keycloak;
using Granit.Authorization;
using Granit.Core.Modularity;
using Granit.Identity;
using Granit.Identity.Endpoints;
using Granit.Identity.EntityFrameworkCore;
using Granit.Identity.Keycloak;
using Granit.Persistence.Migrations;

namespace GranitApiFull;

/// <summary>
/// Root module for the application.
/// Bundles (Api, Notifications) are added via the fluent builder in Program.cs.
/// Module-level dependencies are declared here with <c>[DependsOn]</c>.
/// </summary>
[DependsOn(
    typeof(GranitJwtBearerModule),
    typeof(GranitKeycloakModule),
    typeof(GranitAuthorizationModule),
    typeof(GranitIdentityModule),
    typeof(GranitIdentityKeycloakModule),
    typeof(GranitIdentityEntityFrameworkCoreModule),
    typeof(GranitIdentityEndpointsModule),
    typeof(GranitPersistenceMigrationsModule))]
public sealed class AppHostModule : GranitModule;
