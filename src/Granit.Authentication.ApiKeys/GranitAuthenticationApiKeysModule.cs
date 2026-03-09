using Granit.Authentication.ApiKeys.Extensions;
using Granit.Core.Modularity;
using Granit.ExceptionHandling;
using Granit.Guids;
using Granit.Security;
using Granit.Timing;

namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Granit module that registers API key authentication services.
/// </summary>
[DependsOn(typeof(GranitSecurityModule))]
[DependsOn(typeof(GranitTimingModule))]
[DependsOn(typeof(GranitGuidsModule))]
[DependsOn(typeof(GranitExceptionHandlingModule))]
public sealed class GranitAuthenticationApiKeysModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitApiKeyAuthentication();
}
