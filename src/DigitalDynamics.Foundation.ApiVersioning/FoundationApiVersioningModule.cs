using DigitalDynamics.Foundation.ApiVersioning.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.ApiVersioning;

/// <summary>
/// Foundation module for URL-based API versioning.
/// Registers <c>Asp.Versioning</c> with URL segment and query string readers.
/// Route template: <c>/api/v{version:apiVersion}/resource</c>.
/// </summary>
public sealed class FoundationApiVersioningModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationApiVersioning(context.Configuration);
}
