using Microsoft.Extensions.DependencyInjection;

namespace Granit.Imaging.Extensions;

/// <summary>
/// Extension methods for registering <c>Granit.Imaging</c> services.
/// </summary>
public static class ImagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Granit imaging abstraction layer.
    /// </summary>
    /// <remarks>
    /// The base package provides interfaces only. An implementation package
    /// (e.g. <c>Granit.Imaging.MagickNet</c>) must be registered separately
    /// to provide the concrete <see cref="IImageProcessor"/>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitImaging(this IServiceCollection services) =>
        services;
}
