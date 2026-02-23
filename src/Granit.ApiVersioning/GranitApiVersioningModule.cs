using Granit.ApiVersioning.Extensions;
using Granit.Core.Modularity;

namespace Granit.ApiVersioning;

/// <summary>
/// Granit module for URL-based API versioning.
/// Registers <c>Asp.Versioning</c> with URL segment and query string readers.
/// Route template: <c>/api/v{version:apiVersion}/resource</c>.
/// </summary>
public sealed class GranitApiVersioningModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitApiVersioning(context.Configuration);
}
