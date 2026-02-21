// =============================================================================
// MultiTenancyApplicationBuilderExtensions - Enregistrement du middleware
// =============================================================================
// À appeler dans Program.cs, après UseAuthentication et avant UseAuthorization :
//
//   app.UseAuthentication();
//   app.UseFoundationMultiTenancy();   // résout le tenant depuis JWT ou header
//   app.UseAuthorization();
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy.Middleware;
using Microsoft.AspNetCore.Builder;

namespace DigitalDynamics.Foundation.MultiTenancy.Extensions;

/// <summary>
/// Extensions pour enregistrer le middleware MultiTenancy dans le pipeline ASP.NET Core.
/// </summary>
public static class MultiTenancyApplicationBuilderExtensions
{
    /// <summary>
    /// Ajoute <see cref="TenantResolutionMiddleware"/> dans le pipeline de requêtes.
    /// À positionner après <c>UseAuthentication()</c> et avant <c>UseAuthorization()</c>.
    /// </summary>
    public static IApplicationBuilder UseFoundationMultiTenancy(
        this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
