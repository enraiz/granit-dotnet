using FluentValidation;
using Granit.Validation.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Validation.Extensions;

/// <summary>
/// Extension methods for registering Granit validation services.
/// </summary>
public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Granit validation services and configures FluentValidation
    /// to emit structured error codes instead of human-readable messages.
    /// </summary>
    /// <remarks>
    /// Sets <c>ValidatorOptions.Global.LanguageManager</c> to
    /// <c>GranitErrorCodeLanguageManager</c> so that all built-in FluentValidation
    /// rules return codes following the <c>Granit:Validation:{ValidatorName}</c>
    /// convention. The SPA resolves these codes from its local localization dictionary.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitValidation(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager = new GranitErrorCodeLanguageManager();
        return services;
    }
}
