using Microsoft.Extensions.DependencyInjection;

namespace Granit.Authentication.ApiKeys.Endpoints.Extensions;

/// <summary>
/// Extension methods to register API key endpoint services.
/// </summary>
public static class ApiKeysEndpointsServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required by the API key management endpoints.
    /// </summary>
    public static IServiceCollection AddGranitApiKeysEndpoints(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
