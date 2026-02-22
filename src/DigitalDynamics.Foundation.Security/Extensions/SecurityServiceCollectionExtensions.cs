// =============================================================================
// SecurityServiceCollectionExtensions - Enregistrement JWT Bearer générique + CurrentUser
// =============================================================================
// Point d'entrée unique pour configurer l'authentification JWT Bearer OIDC générique
// dans une application .NET Digital Dynamics.
//
// Usage :
//   builder.Services.AddFoundationSecurity(builder.Configuration);
//
// Lit la section "Authentication" de la configuration.
// Pour Keycloak : utiliser AddFoundationSecurityKeycloak() (Foundation.Security.Keycloak).
// =============================================================================

using DigitalDynamics.Foundation.Security.Authentication;
using DigitalDynamics.Foundation.Security.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DigitalDynamics.Foundation.Security.Extensions;

/// <summary>
/// Extensions pour configurer l'authentification JWT Bearer générique et <see cref="ICurrentUserService"/>.
/// </summary>
public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute l'authentification JWT Bearer OIDC générique et le service CurrentUser.
    /// Lit la section <c>"Authentication"</c> de la configuration.
    /// </summary>
    public static IServiceCollection AddFoundationSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(JwtBearerAuthOptions.SectionName);
        services.Configure<JwtBearerAuthOptions>(section);

        JwtBearerAuthOptions options = section.Get<JwtBearerAuthOptions>() ?? new JwtBearerAuthOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
