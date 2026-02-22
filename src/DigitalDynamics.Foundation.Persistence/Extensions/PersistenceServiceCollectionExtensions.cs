using DigitalDynamics.Foundation.Core.DataFiltering;
using DigitalDynamics.Foundation.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Persistence.Extensions;

/// <summary>
/// Extensions for registering Foundation persistence services.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Adds Foundation EF Core services:
    /// <list type="bullet">
    ///   <item>HDS audit interceptor (<see cref="AuditedEntityInterceptor"/>)</item>
    ///   <item>GDPR soft delete interceptor (<see cref="SoftDeleteInterceptor"/>)</item>
    ///   <item>
    ///     Data filter service (<see cref="IDataFilter"/>) for runtime filter control.
    ///     Registered as Singleton: state lives in a <c>static AsyncLocal</c> field,
    ///     not in instance fields.
    ///   </item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddFoundationPersistence(this IServiceCollection services)
    {
        services.AddScoped<AuditedEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddSingleton<IDataFilter, DataFilter>();

        return services;
    }
}
