// =============================================================================
// FoundationLocalizationModule - Module Foundation pour la localisation JSON
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Localization.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Module Foundation pour la localisation JSON modulaire.
/// Enregistre IStringLocalizerFactory et la ressource Foundation par défaut (fr/en).
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
