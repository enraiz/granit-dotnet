using Granit.Identity.EntityFrameworkCore.DbContext;
using Granit.Identity.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Identity.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core identity user cache services.
/// </summary>
public static class IdentityEfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core persistence for the identity user cache.
    /// Replaces the default <c>NullUserLookupService</c> registered by
    /// <c>Granit.Identity</c> with <see cref="CachedUserLookupService"/>,
    /// and registers the <see cref="EfCoreUserCacheStore{TContext}"/>.
    /// </summary>
    /// <typeparam name="TContext">
    /// The application DbContext, which must implement <see cref="IUserCacheDbContext"/>.
    /// </typeparam>
    public static IServiceCollection AddGranitIdentityEntityFrameworkCore<TContext>(
        this IServiceCollection services)
        where TContext : Microsoft.EntityFrameworkCore.DbContext, IUserCacheDbContext
    {
        services.Replace(ServiceDescriptor.Scoped<IUserLookupService, CachedUserLookupService>());
        services.TryAddScoped<IUserCacheStore, EfCoreUserCacheStore<TContext>>();
        services.AddOptions<UserCacheOptions>()
            .BindConfiguration(UserCacheOptions.SectionName);

        return services;
    }
}
