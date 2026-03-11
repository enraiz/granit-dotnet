using Granit.Core.Modularity;
using Granit.Cors.Extensions;
using Granit.Cors.Options;

namespace Granit.Cors;

/// <summary>
/// Granit module for standardized CORS configuration.
/// </summary>
/// <remarks>
/// Registers CORS middleware with a default policy driven by <see cref="GranitCorsOptions"/>.
/// ISO 27001-compliant: wildcard origins are rejected in non-development environments.
/// </remarks>
public sealed class GranitCorsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitCors();
}
