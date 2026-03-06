using Granit.Identity.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Identity.Extensions;

/// <summary>
/// Extension methods for registering Granit.Identity services.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Granit identity abstractions with a <see cref="NullIdentityProvider"/>
    /// as the default <see cref="IIdentityProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitIdentity(
        this IServiceCollection services)
    {
        services.TryAddScoped<IIdentityProvider, NullIdentityProvider>();
        services.TryAddScoped<IUserLookupService, NullUserLookupService>();
        services.TryAddScoped<IUserCacheStats, NullUserCacheStats>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="IIdentityProvider"/> implementation,
    /// replacing any previously registered provider.
    /// </summary>
    /// <typeparam name="TProvider">The identity provider implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentityProvider<TProvider>(
        this IServiceCollection services)
        where TProvider : class, IIdentityProvider
    {
        services.Replace(ServiceDescriptor.Scoped<IIdentityProvider, TProvider>());
        return services;
    }
}
