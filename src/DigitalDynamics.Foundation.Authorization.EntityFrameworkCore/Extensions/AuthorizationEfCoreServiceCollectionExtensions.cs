using DigitalDynamics.Foundation.Authorization.Abstractions;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.DbContext;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Services;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core authorization services.
/// </summary>
public static class AuthorizationEfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core persistence for permission grants.
    /// Replaces the default <see cref="NullPermissionGrantStore"/> registered by
    /// <c>Foundation.Authorization</c> with <see cref="EfCorePermissionGrantStore{TContext}"/>,
    /// and registers <see cref="IPermissionManager"/>.
    /// </summary>
    /// <typeparam name="TContext">
    /// The application DbContext, which must implement <see cref="IPermissionGrantDbContext"/>.
    /// </typeparam>
    public static IServiceCollection AddFoundationAuthorizationEntityFrameworkCore<TContext>(
        this IServiceCollection services)
        where TContext : Microsoft.EntityFrameworkCore.DbContext, IPermissionGrantDbContext
    {
        // Replace the NullPermissionGrantStore registered by Foundation.Authorization
        services.Replace(ServiceDescriptor.Scoped<IPermissionGrantStore,
            EfCorePermissionGrantStore<TContext>>());

        services.AddScoped<IPermissionManager, PermissionManager<TContext>>();

        return services;
    }
}
