using Granit.Core.Modularity;

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
public sealed class GranitReferenceDataEndpointsModule : GranitModule;
