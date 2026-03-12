using Granit.Authorization;
using Granit.Core.Modularity;

namespace Granit.Authentication.ApiKeys.Endpoints;

/// <summary>
/// Module for API key management endpoints.
/// Permission definition providers are auto-discovered by <c>GranitAuthorizationModule</c>.
/// </summary>
[DependsOn(
    typeof(GranitAuthenticationApiKeysModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitAuthenticationApiKeysEndpointsModule : GranitModule;
