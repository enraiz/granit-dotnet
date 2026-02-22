using Microsoft.AspNetCore.Http;

namespace DigitalDynamics.Foundation.MultiTenancy.Resolvers;

/// <summary>
/// Tenant resolver for an HTTP context.
/// Resolvers are chained in ascending order of <see cref="Order"/>.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolution priority. Lower value = resolved first.
    /// HeaderTenantResolver = 100, JwtClaimTenantResolver = 200.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Attempts to resolve the tenant from the HTTP context.
    /// </summary>
    /// <param name="context">HTTP context of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved <see cref="TenantInfo"/>, or <c>null</c> if it cannot be determined.</returns>
    Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
}
