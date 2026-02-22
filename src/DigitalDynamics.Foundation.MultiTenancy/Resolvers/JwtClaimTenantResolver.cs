// =============================================================================
// JwtClaimTenantResolver - Tenant resolution from the JWT claim
// =============================================================================
// Reads the claim MultiTenancyOptions.TenantIdClaimType ("tenant_id" by default).
// Suited for authenticated user flows via Keycloak (validated JWT token).
//
// Inputs  : HttpContext.User.FindFirstValue(TenantIdClaimType)
// Outputs : TenantInfo(id) if claim is valid | null if absent or invalid GUID
// =============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.MultiTenancy.Resolvers;

/// <summary>
/// Resolves the tenant from the JWT claim (order = 200, resolved after the header).
/// </summary>
public sealed class JwtClaimTenantResolver : ITenantResolver
{
    private readonly MultiTenancyOptions _options;

    /// <inheritdoc/>
    public int Order => 200;

    public JwtClaimTenantResolver(IOptions<MultiTenancyOptions> options) =>
        _options = options.Value;

    /// <inheritdoc/>
    public Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        string? claim = context.User.FindFirstValue(_options.TenantIdClaimType);
        if (string.IsNullOrEmpty(claim))
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        if (!Guid.TryParse(claim, out Guid tenantId))
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId));
    }
}
