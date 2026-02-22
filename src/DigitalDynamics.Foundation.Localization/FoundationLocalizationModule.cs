// =============================================================================
// FoundationLocalizationModule - Module Foundation pour la localisation JSON
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Localization.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Foundation module for modular JSON localization.
/// Registers IStringLocalizerFactory and the default Foundation resource (fr/en).
/// </summary>
public sealed class FoundationLocalizationModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFoundationLocalization();

        context.Services.Configure<FoundationLocalizationOptions>(options =>
        {
            options.Resources
                .Add<FoundationLocalizationResource>("fr")
                .AddJson(
                    typeof(FoundationLocalizationResource).Assembly,
                    "DigitalDynamics.Foundation.Localization.Localization.Foundation");
        });
    }
}
