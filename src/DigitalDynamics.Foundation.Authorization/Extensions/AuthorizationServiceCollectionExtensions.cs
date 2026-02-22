using DigitalDynamics.Foundation.Authorization.Abstractions;
using DigitalDynamics.Foundation.Authorization.Authorization;
using DigitalDynamics.Foundation.Authorization.Options;
using DigitalDynamics.Foundation.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.Authorization.Extensions;

/// <summary>Service collection extensions for registering Foundation.Authorization services.</summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Foundation RBAC authorization services including permission definitions,
    /// the dynamic policy provider, and the caching-aware permission checker.
    /// </summary>
    public static IServiceCollection AddFoundationAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FoundationAuthorizationOptions>(
            configuration.GetSection(FoundationAuthorizationOptions.SectionName));

        services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();

        services.TryAddSingleton<IPermissionGrantStore, NullPermissionGrantStore>();

        services.AddScoped<IPermissionChecker, PermissionChecker>();

        services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
