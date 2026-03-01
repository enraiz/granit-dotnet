using Granit.Core.Modularity;
using Granit.ReferenceData.Extensions;

namespace Granit.ReferenceData;

/// <summary>
/// Granit module for generic reference data management.
/// Provides base entity, store/seeder abstractions, options, and memory cache.
/// </summary>
/// <remarks>
/// Register via:
/// <code>
/// services.AddGranitReferenceData();
/// </code>
/// </remarks>
public sealed class GranitReferenceDataModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitReferenceData();
}
