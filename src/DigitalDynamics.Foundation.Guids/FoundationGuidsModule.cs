using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Guids.Extensions;

namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Module Foundation pour IGuidGenerator (GUID sequentiels).
/// Aucune dependance sur d'autres modules Foundation.
/// </summary>
public sealed class FoundationGuidsModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationGuids();
}
