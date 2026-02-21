// =============================================================================
// ITenantResolver - Contrat pour la résolution du tenant courant
// =============================================================================
// Implémenté par HeaderTenantResolver et JwtClaimTenantResolver.
// L'ordre de résolution est déterminé par la propriété Order (croissant).
// =============================================================================

using Microsoft.AspNetCore.Http;

namespace DigitalDynamics.Foundation.MultiTenancy.Resolvers;

/// <summary>
/// Résolveur de tenant pour un contexte HTTP.
/// Les résolveurs sont chaînés par ordre croissant de <see cref="Order"/>.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Priorité de résolution. Valeur plus faible = résolu en premier.
    /// HeaderTenantResolver = 100, JwtClaimTenantResolver = 200.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Tente de résoudre le tenant depuis le contexte HTTP.
    /// </summary>
    /// <param name="context">Contexte HTTP de la requête.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    /// <returns>Le <see cref="TenantInfo"/> résolu, ou <c>null</c> si non déterminable.</returns>
    Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
}
