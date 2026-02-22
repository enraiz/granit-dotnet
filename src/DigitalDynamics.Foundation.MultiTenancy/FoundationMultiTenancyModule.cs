// =============================================================================
// FoundationMultiTenancyModule - Foundation module for multi-tenancy
// =============================================================================
// Configures ICurrentTenant, JWT/Header resolvers, and the middleware.
//
// IMPORTANT: call app.UseFoundationMultiTenancy() in Program.cs
// after UseAuthentication() and before UseAuthorization():
//
//   app.UseAuthentication();
//   app.UseFoundationMultiTenancy();
//   app.UseAuthorization();
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.MultiTenancy.Extensions;
using DigitalDynamics.Foundation.Security;

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Foundation module for multi-tenant management.
/// Resolves the tenant from the HTTP header or the Keycloak JWT claim.
/// </summary>
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationMultiTenancyModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationMultiTenancy(
            context.Configuration.GetSection(MultiTenancyOptions.SectionName));
}
