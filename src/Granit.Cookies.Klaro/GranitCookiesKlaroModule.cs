using Granit.Cookies.Klaro.Extensions;
using Granit.Core.Modularity;

namespace Granit.Cookies.Klaro;

/// <summary>
/// Granit module for the Klaro CMP integration.
/// Depends on <see cref="GranitCookiesModule"/> for the cookie management infrastructure.
/// Registration is done via <see cref="GranitCookiesBuilderExtensions.UseKlaro"/>
/// or automatically through the module system.
/// </summary>
[DependsOn(typeof(GranitCookiesModule))]
public sealed class GranitCookiesKlaroModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitCookiesKlaro();
}
