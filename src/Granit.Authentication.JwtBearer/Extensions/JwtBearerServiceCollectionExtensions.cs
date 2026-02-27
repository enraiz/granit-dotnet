using Granit.Authentication.JwtBearer.Authentication;
using Granit.Authentication.JwtBearer.Options;
using Granit.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Granit.Authentication.JwtBearer.Extensions;

/// <summary>
/// Extensions for configuring generic JWT Bearer authentication and <see cref="ICurrentUserService"/>.
/// </summary>
public static class JwtBearerServiceCollectionExtensions
{
    /// <summary>
    /// Adds generic OIDC JWT Bearer authentication and the CurrentUser service.
    /// Reads the <c>"Authentication"</c> section from configuration.
    /// </summary>
    public static IServiceCollection AddGranitJwtBearer(
        this IServiceCollection services)
    {
        services
            .AddOptions<JwtBearerAuthOptions>()
            .BindConfiguration(JwtBearerAuthOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Deferred configuration: JwtBearerOptions reads JwtBearerAuthOptions at resolution time.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtBearerAuthOptions>>((jwt, granitOpts) =>
            {
                JwtBearerAuthOptions options = granitOpts.Value;
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
