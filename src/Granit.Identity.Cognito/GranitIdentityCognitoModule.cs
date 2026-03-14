using Granit.Core.Modularity;
using Granit.Identity.Cognito.Extensions;

namespace Granit.Identity.Cognito;

/// <summary>
/// Granit module that registers the AWS Cognito User Pools as the
/// <see cref="IIdentityProvider"/> implementation.
/// </summary>
[DependsOn(typeof(GranitIdentityModule))]
public sealed class GranitIdentityCognitoModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIdentityCognito();
}
