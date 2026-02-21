// =============================================================================
// FoundationMultiTenancyModule - Module Foundation pour le multi-tenant
// =============================================================================
// Configure ICurrentTenant, les résolveurs JWT/Header et le middleware.
//
// IMPORTANT : appeler app.UseFoundationMultiTenancy() dans Program.cs
// après UseAuthentication() et avant UseAuthorization() :
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
/// Module Foundation pour la gestion multi-tenant.
/// Résolution du tenant depuis le header HTTP ou le claim JWT Keycloak.
/// </summary>
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationMultiTenancyModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationMultiTenancy(
            context.Configuration.GetSection(MultiTenancyOptions.SectionName));
}
