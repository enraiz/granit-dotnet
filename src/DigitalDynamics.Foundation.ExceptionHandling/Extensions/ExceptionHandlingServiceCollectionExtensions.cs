using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.ExceptionHandling.Extensions;

/// <summary>
/// Extensions for registering Foundation exception handling services.
/// </summary>
public static class ExceptionHandlingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Foundation centralized exception handling pipeline:
    /// <list type="bullet">
    ///   <item>RFC 7807 Problem Details support (<c>AddProblemDetails</c>)</item>
    ///   <item><see cref="FoundationExceptionHandler"/> as the active <see cref="IExceptionHandler"/></item>
    ///   <item><see cref="DefaultExceptionStatusCodeMapper"/> as the fallback mapper</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Call <c>app.UseFoundationExceptionHandling()</c> in the middleware pipeline
    /// <b>before</b> routing and authorization to ensure all exceptions are caught.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration delegate for <see cref="ExceptionHandlingOptions"/>.</param>
    public static IServiceCollection AddFoundationExceptionHandling(
        this IServiceCollection services,
        Action<ExceptionHandlingOptions>? configure = null)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<FoundationExceptionHandler>();
        services.AddSingleton<IExceptionStatusCodeMapper, DefaultExceptionStatusCodeMapper>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
