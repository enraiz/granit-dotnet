// =============================================================================
// HeaderTenantResolver - Résolution du tenant depuis le header HTTP
// =============================================================================
// Lit le header MultiTenancyOptions.TenantIdHeaderName ("X-Tenant-Id" par défaut).
// Utilisé pour les appels service-à-service où le tenant est transmis explicitement.
//
// Inputs  : HttpContext.Request.Headers[TenantIdHeaderName]
// Outputs : TenantInfo(id) si header valide | null si absent ou GUID invalide
// =============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace DigitalDynamics.Foundation.MultiTenancy.Resolvers;

/// <summary>
/// Résout le tenant depuis le header HTTP (ordre = 100, résolu en premier).
/// </summary>
public sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly MultiTenancyOptions _options;

    /// <inheritdoc/>
    public int Order => 100;

    public HeaderTenantResolver(IOptions<MultiTenancyOptions> options) =>
        _options = options.Value;

    /// <inheritdoc/>
    public Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Request.Headers.TryGetValue(_options.TenantIdHeaderName, out StringValues values))
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        string? header = values.FirstOrDefault();
        if (string.IsNullOrEmpty(header))
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        if (!Guid.TryParse(header, out Guid tenantId))
        {
            return Task.FromResult<TenantInfo?>(null);
        }

        return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId));
    }
}
