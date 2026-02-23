using Granit.Core.Modularity;

namespace Granit.Localization.Endpoints;

/// <summary>
/// Granit module for localization HTTP endpoints.
/// Requires <see cref="GranitLocalizationModule"/> to be registered.
/// Exposes <c>GET /api/granit/localization</c> via
/// <see cref="Extensions.LocalizationEndpointRouteBuilderExtensions.MapGranitLocalization"/>.
/// </summary>
[DependsOn(typeof(GranitLocalizationModule))]
public sealed class GranitLocalizationEndpointsModule : GranitModule;
