using Granit.Wolverine.ClaimCheck.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Wolverine.ClaimCheck;

/// <summary>
/// Extension methods for registering <see cref="IClaimCheckStore"/> implementations.
/// </summary>
public static class ClaimCheckServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory claim check store for development and testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The in-memory store is <b>not shared across instances</b> and does not
    /// enforce expiry. Use a persistent implementation (blob storage, Redis)
    /// in production environments.
    /// </para>
    /// <para>
    /// Uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService,TImplementation}"/>
    /// — if a production implementation is already registered, this call is a no-op.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryClaimCheckStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IClaimCheckStore, InMemoryClaimCheckStore>();
        return services;
    }
}
