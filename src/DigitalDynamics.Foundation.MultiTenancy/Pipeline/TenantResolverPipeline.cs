// =============================================================================
// TenantResolverPipeline - Tenant resolution chain
// =============================================================================
// Executes ITenantResolver instances in ascending order of their Order property.
// Returns the first non-null result ("first-wins" strategy).
//
// Inputs  : IEnumerable<ITenantResolver> (resolvers sorted by Order)
// Outputs : TenantInfo if a resolver succeeds | null if no resolver resolves
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy.Resolvers;
using Microsoft.AspNetCore.Http;

namespace DigitalDynamics.Foundation.MultiTenancy.Pipeline;

/// <summary>
/// Pipeline for resolving the current tenant.
/// Executes resolvers in ascending order of <see cref="ITenantResolver.Order"/>.
/// </summary>
public sealed class TenantResolverPipeline
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers;

    public TenantResolverPipeline(IEnumerable<ITenantResolver> resolvers) =>
        _resolvers = [.. resolvers.OrderBy(r => r.Order)];

    /// <summary>
    /// Resolves the current tenant from the HTTP context.
    /// Returns the result of the first resolver that produces a non-null response.
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
