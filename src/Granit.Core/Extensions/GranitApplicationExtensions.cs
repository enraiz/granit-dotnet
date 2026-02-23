using Granit.Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.Core.Extensions;

/// <summary>
/// Extensions pour l'initialisation post-Build() des modules Granit.
/// </summary>
public static class GranitApplicationExtensions
{
    // --- Variantes synchrones ---

    /// <summary>
    /// Initialise tous les modules Granit (version synchrone).
    /// Appele apres <c>Build()</c>, avant <c>Run()</c>.
    /// </summary>
    public static IApplicationBuilder UseGranit(this IApplicationBuilder app)
    {
        GranitApplication foundationApp = app.ApplicationServices
            .GetRequiredService<GranitApplication>();
        ApplicationInitializationContext context = new(app.ApplicationServices);
        foundationApp.InitializeApplication(context);
        return app;
    }

    /// <summary>
    /// Surcharge synchrone pour WebApplication (resout l'ambiguite IApplicationBuilder / IHost).
    /// </summary>
    public static WebApplication UseGranit(this WebApplication app)
    {
        ((IApplicationBuilder)app).UseGranit();
        return app;
    }

    /// <summary>
    /// Surcharge synchrone pour les hotes non-web (Worker Services).
    /// </summary>
    public static IHost UseGranit(this IHost host)
    {
        GranitApplication foundationApp = host.Services
            .GetRequiredService<GranitApplication>();
        ApplicationInitializationContext context = new(host.Services);
        foundationApp.InitializeApplication(context);
        return host;
    }

    // --- Variantes asynchrones ---

    /// <summary>
    /// Initialise tous les modules Granit (version asynchrone).
    /// Appele apres <c>Build()</c>, avant <c>RunAsync()</c>.
    /// </summary>
    public static async Task<IApplicationBuilder> UseGranitAsync(this IApplicationBuilder app)
    {
        GranitApplication foundationApp = app.ApplicationServices
            .GetRequiredService<GranitApplication>();
        ApplicationInitializationContext context = new(app.ApplicationServices);
        await foundationApp.InitializeApplicationAsync(context);
        return app;
    }

    /// <summary>
    /// Surcharge asynchrone pour WebApplication (resout l'ambiguite IApplicationBuilder / IHost).
    /// </summary>
    public static async Task<WebApplication> UseGranitAsync(this WebApplication app)
    {
        await ((IApplicationBuilder)app).UseGranitAsync();
        return app;
    }

    /// <summary>
    /// Surcharge asynchrone pour les hotes non-web (Worker Services).
    /// </summary>
    public static async Task<IHost> UseGranitAsync(this IHost host)
    {
        GranitApplication foundationApp = host.Services
            .GetRequiredService<GranitApplication>();
        ApplicationInitializationContext context = new(host.Services);
        await foundationApp.InitializeApplicationAsync(context);
        return host;
    }
}
