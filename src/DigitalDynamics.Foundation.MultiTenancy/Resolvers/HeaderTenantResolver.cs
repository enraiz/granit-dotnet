// =============================================================================
// HeaderTenantResolver - Tenant resolution from the HTTP header
// =============================================================================
// Reads the header MultiTenancyOptions.TenantIdHeaderName ("X-Tenant-Id" by default).
// Used for service-to-service calls where the tenant is passed explicitly.
//
// Inputs  : HttpContext.Request.Headers[TenantIdHeaderName]
// Outputs : TenantInfo(id) if header is valid | null if absent or invalid GUID
// =============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace DigitalDynamics.Foundation.MultiTenancy.Resolvers;

/// <summary>
/// Resolves the tenant from the HTTP header (order = 100, resolved first).
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
