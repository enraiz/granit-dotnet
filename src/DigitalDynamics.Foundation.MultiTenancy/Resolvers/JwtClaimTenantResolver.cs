// =============================================================================
// JwtClaimTenantResolver - Résolution du tenant depuis le claim JWT
// =============================================================================
// Lit le claim MultiTenancyOptions.TenantIdClaimType ("tenant_id" par défaut).
// Adapté aux flux utilisateur authentifiés via Keycloak (token JWT validé).
//
// Inputs  : HttpContext.User.FindFirstValue(TenantIdClaimType)
// Outputs : TenantInfo(id) si claim valide | null si absent ou GUID invalide
// =============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.MultiTenancy.Resolvers;

/// <summary>
/// Résout le tenant depuis le claim JWT (ordre = 200, résolu après le header).
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
