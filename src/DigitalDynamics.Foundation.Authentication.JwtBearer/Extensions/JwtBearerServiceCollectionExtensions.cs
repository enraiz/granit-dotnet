using DigitalDynamics.Foundation.Authentication.JwtBearer.Authentication;
using DigitalDynamics.Foundation.Authentication.JwtBearer.Options;
using DigitalDynamics.Foundation.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer.Extensions;

/// <summary>
/// Extensions for configuring generic JWT Bearer authentication and <see cref="ICurrentUserService"/>.
/// </summary>
public static class JwtBearerServiceCollectionExtensions
{
    /// <summary>
    /// Adds generic OIDC JWT Bearer authentication and the CurrentUser service.
    /// Reads the <c>"Authentication"</c> section from configuration.
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
