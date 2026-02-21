// =============================================================================
// TenantResolverPipeline - Chaîne de résolution du tenant
// =============================================================================
// Exécute les ITenantResolver dans l'ordre croissant de leur propriété Order.
// Retourne le premier résultat non-null (stratégie "first-wins").
//
// Inputs  : IEnumerable<ITenantResolver> (résolveurs triés par Order)
// Outputs : TenantInfo si un résolveur réussit | null si aucun résolveur ne résout
// =============================================================================

using Microsoft.AspNetCore.Http;
using DigitalDynamics.Foundation.MultiTenancy.Resolvers;

namespace DigitalDynamics.Foundation.MultiTenancy.Pipeline;

/// <summary>
/// Pipeline de résolution du tenant courant.
/// Exécute les résolveurs dans l'ordre croissant de <see cref="ITenantResolver.Order"/>.
/// </summary>
public sealed class TenantResolverPipeline
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers;

    public TenantResolverPipeline(IEnumerable<ITenantResolver> resolvers) =>
        _resolvers = [.. resolvers.OrderBy(r => r.Order)];

    /// <summary>
    /// Résout le tenant courant depuis le contexte HTTP.
    /// Retourne le résultat du premier résolveur ayant une réponse non-null.
    /// </summary>
    public async Task<TenantInfo?> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (ITenantResolver resolver in _resolvers)
        {
            TenantInfo? tenant = await resolver.ResolveAsync(context, cancellationToken);
            if (tenant is not null)
            {
                return tenant;
            }
        }

        return null;
    }
}
