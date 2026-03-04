using Granit.Core.Modularity;
using Granit.Identity.Extensions;

namespace Granit.Identity;

/// <summary>
/// Granit module for identity provider abstractions.
/// Registers a <see cref="NullIdentityProvider"/> by default.
/// Install a provider package (e.g. <c>Granit.Identity.Keycloak</c>) to connect
/// to a real identity system.
/// </summary>
public sealed class GranitIdentityModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIdentity();
}
