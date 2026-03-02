using Granit.Authentication.Keycloak.Authentication;
using Granit.Authentication.Keycloak.BackChannelLogout;
using Granit.Authentication.Keycloak.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        this IServiceCollection services)
    {
        services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // PostConfigure s'exécute après AddGranitJwtBearer (GranitJwtBearerModule),
        // permettant de surcharger Authority, Audience et NameClaimType pour Keycloak.
        // Deferred configuration: reads KeycloakOptions at resolution time.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<IOptions<KeycloakOptions>>((jwt, keycloakOpts) =>
            {
                KeycloakOptions options = keycloakOpts.Value;
                string audience = options.Audience ?? options.ClientId;
                jwt.Authority = options.Authority;
                jwt.Audience = audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.TokenValidationParameters.NameClaimType = "preferred_username";
                jwt.TokenValidationParameters.ValidIssuer = options.Authority;
                jwt.TokenValidationParameters.ValidAudience = audience;
            });

        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

        // Deferred Admin policy: reads AdminRole from KeycloakOptions at resolution time.
        services
            .AddOptions<AuthorizationOptions>()
            .Configure<IOptions<KeycloakOptions>>((authOpts, keycloakOpts) =>
            {
                authOpts.AddPolicy("Admin",
                    policy => policy.RequireRole(keycloakOpts.Value.AdminRole));
            });

        // Back-channel logout: revoked session store + token validator
        services.AddDistributedMemoryCache();
        services.TryAddSingleton<IRevokedSessionStore, DistributedCacheRevokedSessionStore>();
        services.TryAddSingleton<BackChannelLogoutTokenValidator>();

        // Wire OnTokenValidated to check revoked sessions when back-channel logout is enabled
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<IOptions<KeycloakOptions>>((jwt, keycloakOpts) =>
            {
                if (!keycloakOpts.Value.BackChannelLogout.Enabled)
                {
                    return;
                }

                JwtBearerEvents existing = jwt.Events ?? new JwtBearerEvents();
                Func<TokenValidatedContext, Task> previous = existing.OnTokenValidated;

                existing.OnTokenValidated = async context =>
                {
                    await previous(context).ConfigureAwait(false);

                    IRevokedSessionStore store = context.HttpContext.RequestServices
                        .GetRequiredService<IRevokedSessionStore>();

                    string? sid = context.Principal?.FindFirst("sid")?.Value;
                    string? sub = context.Principal?.FindFirst("sub")?.Value;
                    string? sessionKey = sid ?? sub;

                    if (sessionKey is not null
                        && await store.IsSessionRevokedAsync(sessionKey, context.HttpContext.RequestAborted).ConfigureAwait(false))
                    {
                        context.Fail("Session has been revoked via back-channel logout.");
                    }
                };

                jwt.Events = existing;
            });

        return services;
    }
}
