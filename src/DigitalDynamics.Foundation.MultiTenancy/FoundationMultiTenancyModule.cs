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
//
// Le module ne dépend pas de Foundation.Security : JwtClaimTenantResolver
// lit HttpContext.User.FindFirstValue() (ClaimsPrincipal standard ASP.NET Core)
// et fonctionne avec n'importe quel fournisseur d'identité (Keycloak, Auth0, etc.).
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.MultiTenancy.Extensions;

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Module Foundation pour la gestion multi-tenant.
/// Résolution du tenant depuis le header HTTP ou le claim JWT (standard ClaimsPrincipal).
/// Compatible avec tout fournisseur d'identité (Keycloak, Auth0, Azure AD, etc.).
/// </summary>
public sealed class FoundationMultiTenancyModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationMultiTenancy(
            context.Configuration.GetSection(MultiTenancyOptions.SectionName));
}
