// =============================================================================
// FoundationTimingModule - Module Foundation pour IClock et TimeProvider
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Timing.Extensions;

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Module Foundation pour IClock, ICurrentTimezoneProvider et TimeProvider.
/// Aucune dependance sur d'autres modules Foundation.
/// </summary>
public sealed class FoundationTimingModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFoundationTiming();
    }
}
