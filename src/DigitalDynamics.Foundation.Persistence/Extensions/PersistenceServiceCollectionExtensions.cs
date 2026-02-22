using DigitalDynamics.Foundation.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Persistence.Extensions;

/// <summary>
/// Extensions pour enregistrer les services de persistance Foundation.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les intercepteurs EF Core Foundation (audit HDS + soft delete RGPD).
    /// </summary>
    public static IServiceCollection AddFoundationPersistence(this IServiceCollection services)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        return services;
    }
}
