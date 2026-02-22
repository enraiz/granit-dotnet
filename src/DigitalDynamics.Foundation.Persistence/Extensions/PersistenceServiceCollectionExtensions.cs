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
    /// </summary>
    public static IServiceCollection AddFoundationPersistence(this IServiceCollection services)
    {
        services.AddScoped<AuditedEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        return services;
    }
}
