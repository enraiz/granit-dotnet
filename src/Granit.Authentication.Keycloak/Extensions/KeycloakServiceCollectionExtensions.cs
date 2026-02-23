using Granit.Authentication.Keycloak.Authentication;
using Granit.Authentication.Keycloak.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Authentication.Keycloak.Extensions;

/// <summary>
/// Extensions pour configurer les extras Keycloak sur <c>Granit.Authentication.JwtBearer</c>.
/// </summary>
public static class KeycloakServiceCollectionExtensions
{
    /// <summary>
    /// Surcharge la configuration JWT Bearer avec les valeurs Keycloak,
    /// enregistre <see cref="KeycloakClaimsTransformation"/> et la policy <c>"Admin"</c>.
    /// </summary>
    public static IServiceCollection AddGranitKeycloak(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(KeycloakOptions.SectionName);
        services.Configure<KeycloakOptions>(section);

        KeycloakOptions options = section.Get<KeycloakOptions>() ?? new KeycloakOptions();

        // PostConfigure s'exécute après AddGranitJwtBearer (GranitJwtBearerModule),
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
