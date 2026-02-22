// =============================================================================
// JwtBearerServiceCollectionExtensions - Enregistrement JWT Bearer générique + CurrentUser
// =============================================================================
// Point d'entrée pour configurer l'authentification JWT Bearer OIDC générique
// dans une application .NET Digital Dynamics.
//
// Usage :
//   builder.Services.AddFoundationJwtBearer(builder.Configuration);
//
// Lit la section "Authentication" de la configuration.
// Pour Keycloak : utiliser AddFoundationKeycloak() (Foundation.Authentication.Keycloak).
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer.Authentication;
using DigitalDynamics.Foundation.Authentication.JwtBearer.Options;
using DigitalDynamics.Foundation.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer.Extensions;

/// <summary>
/// Extensions pour configurer l'authentification JWT Bearer générique et <see cref="ICurrentUserService"/>.
/// </summary>
public static class JwtBearerServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute l'authentification JWT Bearer OIDC générique et le service CurrentUser.
    /// Lit la section <c>"Authentication"</c> de la configuration.
    /// </summary>
    public static IServiceCollection AddFoundationJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(JwtBearerAuthOptions.SectionName);
        services.Configure<JwtBearerAuthOptions>(section);

        JwtBearerAuthOptions options = section.Get<JwtBearerAuthOptions>() ?? new JwtBearerAuthOptions();

        services
            .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Authority,
                    ValidAudience = options.Audience,
                    NameClaimType = options.NameClaimType,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
