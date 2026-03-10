using Granit.Core.Modularity;
using Granit.ReferenceData.Endpoints.Validators;
using Granit.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.ReferenceData.Endpoints;

/// <summary>
/// Granit module for reference data Minimal API endpoints.
/// </summary>
/// <remarks>
/// <para>
/// This module does not auto-map routes. The host application must call
/// <c>app.MapReferenceDataEndpoints&lt;TEntity&gt;()</c> for each entity type.
/// </para>
/// </remarks>
[DependsOn(typeof(GranitReferenceDataModule))]
public sealed class GranitReferenceDataEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitValidatorsFromAssemblyContaining<ReferenceDataCreateRequestValidator>();
}
