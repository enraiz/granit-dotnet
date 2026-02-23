using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Granit.MultiTenancy.Resolvers;

/// <summary>
/// Resolves the tenant from the HTTP header (order = 100, resolved first).
/// </summary>
public sealed class HeaderTenantResolver(IOptions<MultiTenancyOptions> options) : ITenantResolver
{
    private readonly MultiTenancyOptions _options = options.Value;

    /// <inheritdoc/>
    public int Order => 100;

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
