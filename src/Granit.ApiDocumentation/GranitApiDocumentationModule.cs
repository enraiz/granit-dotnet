using Granit.ApiDocumentation.Extensions;
using Granit.ApiVersioning;
using Granit.Core.Modularity;
using Granit.Security;

namespace Granit.ApiDocumentation;

/// <summary>
/// Granit module for OpenAPI documentation and Scalar UI.
/// Reads <c>ApiDocumentation:MajorVersions</c> from configuration to generate
/// one OpenAPI document per declared API version.
/// </summary>
/// <remarks>
/// Call <c>app.UseGranitApiDocumentation()</c> in <c>Program.cs</c> to expose
/// the OpenAPI JSON endpoints and the Scalar UI.
/// </remarks>
[DependsOn(
    typeof(GranitApiVersioningModule),
    typeof(GranitSecurityModule))]
public sealed class GranitApiDocumentationModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitApiDocumentation();
}
