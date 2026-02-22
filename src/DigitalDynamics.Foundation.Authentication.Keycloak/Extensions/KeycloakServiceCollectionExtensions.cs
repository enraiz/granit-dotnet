// =============================================================================
// KeycloakServiceCollectionExtensions - Extension Keycloak pour JWT Bearer
// =============================================================================
// Surcharge la configuration JWT Bearer de Foundation.Authentication.JwtBearer
// avec les valeurs Keycloak (Authority, Audience, preferred_username) via PostConfigure.
// Enregistre KeycloakClaimsTransformation et la policy "Admin".
//
// Usage :
//   builder.Services.AddFoundationKeycloak(builder.Configuration);
//
// Lit la section "Keycloak" de la configuration.
// =============================================================================

using DigitalDynamics.Foundation.Authentication.Keycloak.Authentication;
using DigitalDynamics.Foundation.Authentication.Keycloak.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Authentication.Keycloak.Extensions;

/// <summary>
/// Extensions pour configurer les extras Keycloak sur <c>Foundation.Authentication.JwtBearer</c>.
/// </summary>
public static class KeycloakServiceCollectionExtensions
{
    /// <summary>
    /// Surcharge la configuration JWT Bearer avec les valeurs Keycloak,
    /// enregistre <see cref="KeycloakClaimsTransformation"/> et la policy <c>"Admin"</c>.
    /// </summary>
    public static IServiceCollection AddFoundationKeycloak(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(KeycloakOptions.SectionName);
        services.Configure<KeycloakOptions>(section);

        KeycloakOptions options = section.Get<KeycloakOptions>() ?? new KeycloakOptions();

        // PostConfigure s'exécute après AddFoundationJwtBearer (FoundationJwtBearerModule),
        // permettant de surcharger Authority, Audience et NameClaimType pour Keycloak.
        services.PostConfigureAll<JwtBearerOptions>(jwt =>
        {
            string audience = options.Audience ?? options.ClientId;
            jwt.Authority = options.Authority;
            jwt.Audience = audience;
            jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
            jwt.TokenValidationParameters.NameClaimType = "preferred_username";
            jwt.TokenValidationParameters.ValidIssuer = options.Authority;
            jwt.TokenValidationParameters.ValidAudience = audience;
        });

        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole(options.AdminRole));

        return services;
    }
}
