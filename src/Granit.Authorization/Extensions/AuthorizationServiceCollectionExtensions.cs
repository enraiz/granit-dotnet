using Granit.Authorization.Abstractions;
using Granit.Authorization.Authorization;
using Granit.Authorization.Options;
using Granit.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Authorization.Extensions;

/// <summary>Service collection extensions for registering Granit.Authorization services.</summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Granit RBAC authorization services including permission definitions,
    /// the dynamic policy provider, and the caching-aware permission checker.
    /// </summary>
    public static IServiceCollection AddGranitAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GranitAuthorizationOptions>(
            configuration.GetSection(GranitAuthorizationOptions.SectionName));

        services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();

        services.TryAddSingleton<IPermissionGrantStore, NullPermissionGrantStore>();

        services.AddScoped<IPermissionChecker, PermissionChecker>();

        services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
