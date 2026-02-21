// =============================================================================
// FoundationApplicationExtensions - Initialisation post-Build()
// =============================================================================
// Extensions app.UseFoundation() (sync) et app.UseFoundationAsync() (async)
// qui appellent OnApplicationInitialization / OnApplicationInitializationAsync
// sur tous les modules dans l'ordre topologique.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DigitalDynamics.Foundation.Core.Extensions;

/// <summary>
/// Extensions pour l'initialisation post-Build() des modules Foundation.
/// </summary>
public static class FoundationApplicationExtensions
{
    // --- Variantes synchrones ---

    /// <summary>
    /// Initialise tous les modules Foundation (version synchrone).
    /// Appele apres <c>Build()</c>, avant <c>Run()</c>.
    /// </summary>
    public static IApplicationBuilder UseFoundation(this IApplicationBuilder app)
    {
        var foundationApp = app.ApplicationServices
            .GetRequiredService<FoundationApplication>();
        var context = new ApplicationInitializationContext(app.ApplicationServices);
        foundationApp.InitializeApplication(context);
        return app;
    }

    /// <summary>
    /// Surcharge synchrone pour WebApplication (resout l'ambiguite IApplicationBuilder / IHost).
    /// </summary>
    public static WebApplication UseFoundation(this WebApplication app)
    {
        ((IApplicationBuilder)app).UseFoundation();
        return app;
    }

    /// <summary>
    /// Surcharge synchrone pour les hotes non-web (Worker Services).
    /// </summary>
    public static IHost UseFoundation(this IHost host)
    {
        var foundationApp = host.Services
            .GetRequiredService<FoundationApplication>();
        var context = new ApplicationInitializationContext(host.Services);
        foundationApp.InitializeApplication(context);
        return host;
    }

    // --- Variantes asynchrones ---

    /// <summary>
    /// Initialise tous les modules Foundation (version asynchrone).
    /// Appele apres <c>Build()</c>, avant <c>RunAsync()</c>.
    /// </summary>
    public static async Task<IApplicationBuilder> UseFoundationAsync(this IApplicationBuilder app)
    {
        var foundationApp = app.ApplicationServices
            .GetRequiredService<FoundationApplication>();
        var context = new ApplicationInitializationContext(app.ApplicationServices);
        await foundationApp.InitializeApplicationAsync(context);
        return app;
    }

    /// <summary>
    /// Surcharge asynchrone pour WebApplication (resout l'ambiguite IApplicationBuilder / IHost).
    /// </summary>
    public static async Task<WebApplication> UseFoundationAsync(this WebApplication app)
    {
        await ((IApplicationBuilder)app).UseFoundationAsync();
        return app;
    }

    /// <summary>
    /// Surcharge asynchrone pour les hotes non-web (Worker Services).
    /// </summary>
    public static async Task<IHost> UseFoundationAsync(this IHost host)
    {
        var foundationApp = host.Services
            .GetRequiredService<FoundationApplication>();
        var context = new ApplicationInitializationContext(host.Services);
        await foundationApp.InitializeApplicationAsync(context);
        return host;
    }
}
