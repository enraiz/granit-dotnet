using Granit.Core.Modularity;
using Granit.Identity.EntraId.Extensions;

namespace Granit.Identity.EntraId;

/// <summary>
/// Granit module that registers the Microsoft Graph API as the
/// <see cref="IIdentityProvider"/> implementation.
/// </summary>
[DependsOn(typeof(GranitIdentityModule))]
public sealed class GranitIdentityEntraIdModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIdentityEntraId();
}
