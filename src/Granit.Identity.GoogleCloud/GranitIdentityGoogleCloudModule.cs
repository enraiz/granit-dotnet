using Granit.Core.Modularity;
using Granit.Identity.GoogleCloud.Extensions;

namespace Granit.Identity.GoogleCloud;

/// <summary>
/// Granit module that registers Google Cloud Identity Platform (Firebase Auth) as the
/// <see cref="IIdentityProvider"/> implementation.
/// </summary>
[DependsOn(typeof(GranitIdentityModule))]
public sealed class GranitIdentityGoogleCloudModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIdentityGoogleCloud();
}
