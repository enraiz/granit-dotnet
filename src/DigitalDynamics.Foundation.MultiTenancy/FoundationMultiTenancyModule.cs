using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.MultiTenancy.Extensions;

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Foundation module for multi-tenant management.
/// Resolves the tenant from the HTTP header or the JWT claim (standard ClaimsPrincipal).
/// Compatible with any identity provider (Keycloak, Auth0, Azure AD, etc.).
/// </summary>
public sealed class FoundationMultiTenancyModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationMultiTenancy(
            context.Configuration.GetSection(MultiTenancyOptions.SectionName));
}
