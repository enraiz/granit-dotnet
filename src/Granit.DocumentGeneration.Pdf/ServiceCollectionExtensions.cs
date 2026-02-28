using Granit.DocumentGeneration.Pdf.Internal;
using Granit.DocumentGeneration.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DocumentGeneration.Pdf;

/// <summary>
/// Extension methods for registering <c>Granit.DocumentGeneration.Pdf</c> services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PuppeteerSharp PDF renderer and Chromium lifecycle service.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="IDocumentRenderer"/> → <c>PuppeteerSharpRenderer</c> (singleton)</item>
    ///   <item><c>ChromiumLifetimeService</c> as <see cref="Microsoft.Extensions.Hosting.IHostedService"/> (singleton)</item>
    /// </list>
    /// <para>
    /// Configuration section: <c>DocumentGeneration:Pdf</c> → <see cref="PdfRenderOptions"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDocumentGenerationPdf(
        this IServiceCollection services)
    {
        services.AddOptions<PdfRenderOptions>()
            .BindConfiguration(PdfRenderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ChromiumLifetimeService>();
        services.AddHostedService(sp => sp.GetRequiredService<ChromiumLifetimeService>());
        services.AddSingleton<IDocumentRenderer, PuppeteerSharpRenderer>();

        return services;
    }
}
