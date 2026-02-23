using Granit.Core.Modularity;
using Granit.Timing.Extensions;

namespace Granit.Timing;

/// <summary>
/// Module Granit pour IClock, ICurrentTimezoneProvider et TimeProvider.
/// Aucune dependance sur d'autres modules Granit.
/// </summary>
public sealed class GranitTimingModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitTiming();
}
