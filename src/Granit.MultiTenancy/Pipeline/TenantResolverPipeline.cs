using Granit.MultiTenancy.Resolvers;
using Microsoft.AspNetCore.Http;

namespace Granit.MultiTenancy.Pipeline;

/// <summary>
/// Pipeline for resolving the current tenant.
/// Executes resolvers in ascending order of <see cref="ITenantResolver.Order"/>.
/// </summary>
public sealed class TenantResolverPipeline(IEnumerable<ITenantResolver> resolvers)
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers = [.. resolvers.OrderBy(r => r.Order)];

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
            TenantInfo? tenant = await resolver.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
            if (tenant is not null)
            {
                return tenant;
            }
        }

        return null;
    }
}
