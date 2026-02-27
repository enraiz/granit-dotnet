using Granit.Core.Modularity;
using Granit.Guids.Extensions;
using Granit.Timing;

namespace Granit.Guids;

/// <summary>
/// Module Granit pour IGuidGenerator (GUID sequentiels).
/// </summary>
[DependsOn(typeof(GranitTimingModule))]
public sealed class GranitGuidsModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitGuids();
}
