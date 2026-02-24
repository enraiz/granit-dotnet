using Granit.Core.Modularity;
using Granit.Localization.DatabaseSource.Extensions;

namespace Granit.Localization.DatabaseSource;

/// <summary>
/// Granit module for the localization DB override cache infrastructure.
/// </summary>
/// <remarks>
/// Registers <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> for the
/// override cache. The concrete <see cref="Granit.Localization.ILocalizationOverrideStore"/>
/// must be registered by a companion module, e.g.
/// <c>GranitLocalizationDatabaseSourceEntityFrameworkCoreModule</c>.
/// </remarks>
[DependsOn(typeof(GranitLocalizationModule))]
public sealed class GranitLocalizationDatabaseSourceModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitLocalizationDatabaseSource();
}
