using Granit.Core.Modularity;
using Granit.Guids.Extensions;

namespace Granit.Guids;

/// <summary>
/// Module Granit pour IGuidGenerator (GUID sequentiels).
/// Aucune dependance sur d'autres modules Granit.
/// </summary>
public sealed class GranitGuidsModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitGuids();
}
