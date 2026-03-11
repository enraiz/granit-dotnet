using Granit.Features.AspNetCore;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Features;

/// <summary>
/// Declares that the decorated controller, action, or Wolverine message handler requires a
/// specific feature to be enabled for the current tenant/plan context.
/// </summary>
/// <remarks>
/// <para>
/// <b>ASP.NET Core controllers / Razor Pages</b> — apply directly on the action or controller;
/// the filter is resolved via DI on each request:
/// <code>
/// [RequiresFeature(AcmeFeatures.VideoConsultation.Name)]
/// public IActionResult StartConsultation() { ... }
/// </code>
/// </para>
/// <para>
/// <b>Minimal API</b> — use the <c>.RequiresFeature()</c> extension method instead:
/// <code>
/// app.MapPost("/consultations", ...).RequiresFeature(AcmeFeatures.VideoConsultation.Name);
/// </code>
/// </para>
/// <para>
/// <b>Wolverine message handlers</b> — decorate the message class; register
/// <see cref="Wolverine.RequiresFeatureMiddleware"/> in your Wolverine setup:
/// <code>
/// [RequiresFeature(AcmeFeatures.ExportPdf.Name)]
/// public sealed class GenerateExportCommand { }
/// </code>
/// </para>
/// <para>
/// When the feature is disabled, <see cref="Exceptions.FeatureNotEnabledException"/> is thrown
/// and mapped to HTTP 403 with <c>errorCode: "Features:NotEnabled"</c> by
/// <c>DefaultExceptionStatusCodeMapper</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresFeatureAttribute(string featureName) : Attribute, IFilterFactory
{
    /// <summary>The feature that must be enabled for the decorated action to execute.</summary>
    public string FeatureName { get; } = featureName;

    /// <inheritdoc/>
    public bool IsReusable => false;

    /// <inheritdoc/>
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        IFeatureChecker featureChecker = serviceProvider.GetRequiredService<IFeatureChecker>();
        return new RequiresFeatureFilter(featureChecker, FeatureName);
    }
}
