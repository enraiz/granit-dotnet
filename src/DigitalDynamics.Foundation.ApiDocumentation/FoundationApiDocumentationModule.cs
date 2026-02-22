using DigitalDynamics.Foundation.ApiDocumentation.Extensions;
using DigitalDynamics.Foundation.ApiVersioning;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security;

namespace DigitalDynamics.Foundation.ApiDocumentation;

/// <summary>
/// Foundation module for OpenAPI documentation and Scalar UI.
/// Reads <c>ApiDocumentation:MajorVersions</c> from configuration to generate
/// one OpenAPI document per declared API version.
/// </summary>
/// <remarks>
/// Call <c>app.UseFoundationApiDocumentation()</c> in <c>Program.cs</c> to expose
/// the OpenAPI JSON endpoints and the Scalar UI.
/// </remarks>
[DependsOn(
    typeof(FoundationApiVersioningModule),
    typeof(FoundationSecurityModule))]
public sealed class FoundationApiDocumentationModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationApiDocumentation(context.Configuration);
}
