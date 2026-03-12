using Microsoft.Extensions.DependencyInjection;

namespace Granit.MyModule.Extensions;

/// <summary>
/// Extension methods for registering MyModule services.
/// </summary>
public static class MyModuleServiceCollectionExtensions
{
    /// <summary>
    /// Adds MyModule services to the service collection.
    /// </summary>
    public static IServiceCollection AddGranitMyModule(this IServiceCollection services)
    {
        // Register services here.
        return services;
    }
}
