using DigitalDynamics.Foundation.ExceptionHandling;
using DigitalDynamics.Foundation.Persistence.ExceptionHandling;
using DigitalDynamics.Foundation.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Persistence.Extensions;

/// <summary>
/// Extensions for registering Foundation persistence services.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Adds Foundation EF Core interceptors (HDS audit + GDPR soft delete).
    /// Also registers <see cref="EfCoreExceptionStatusCodeMapper"/> if
    /// <c>Foundation.ExceptionHandling</c> is present in the container
    /// (<see cref="IExceptionStatusCodeMapper"/> already registered).
    /// </summary>
    public static IServiceCollection AddFoundationPersistence(this IServiceCollection services)
    {
        services.AddScoped<AuditedEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // Register the EF Core exception mapper only when Foundation.ExceptionHandling
        // has been configured (IExceptionStatusCodeMapper already in the container).
        // This avoids a hard dependency on ExceptionHandling for consumers that don't use it.
        if (services.Any(d => d.ServiceType == typeof(IExceptionStatusCodeMapper)))
        {
            services.AddSingleton<IExceptionStatusCodeMapper, EfCoreExceptionStatusCodeMapper>();
        }

        return services;
    }
}
