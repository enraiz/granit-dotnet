// =============================================================================
// SecurityServiceCollectionExtensions - Enregistrement Keycloak + CurrentUser
// =============================================================================
// Point d'entrée unique pour configurer l'authentification Keycloak
// dans une application .NET Digital Dynamics.
//
// Usage :
//   builder.Services.AddFoundationSecurity(builder.Configuration);
// =============================================================================

using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Security.Authentication;
using DigitalDynamics.Foundation.Security.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DigitalDynamics.Foundation.Security.Extensions;

/// <summary>
/// Extensions pour configurer l'authentification Keycloak et les services de sécurité.
/// </summary>
public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute l'authentification Keycloak OIDC, la transformation des claims
    /// et le service CurrentUser.
    /// </summary>
    public static IServiceCollection AddFoundationSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakSection = configuration.GetSection(KeycloakOptions.SectionName);
        services.Configure<KeycloakOptions>(keycloakSection);

        var options = keycloakSection.Get<KeycloakOptions>()
                      ?? new KeycloakOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience ?? options.ClientId;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Authority,
                    ValidAudience = options.Audience ?? options.ClientId,
                    NameClaimType = "preferred_username",
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorization(auth =>
        {
            auth.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());

            auth.AddPolicy("FhirAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "fhir-access"));

            auth.AddPolicy("Admin", policy =>
                policy.RequireRole(options.AdminRole));
        });

        services.AddHttpContextAccessor();
        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
