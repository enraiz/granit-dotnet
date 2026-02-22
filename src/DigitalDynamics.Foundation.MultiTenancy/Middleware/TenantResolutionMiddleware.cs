// =============================================================================
// TenantResolutionMiddleware - Per-request tenant resolution and activation
// =============================================================================
// ASP.NET Core middleware (IMiddleware): resolves the tenant via the resolver
// pipeline, then activates its context in ICurrentTenant for the entire
// duration of the request. If disabled or no tenant resolved, passes through
// without modifying the context.
//
// Registration: app.UseFoundationMultiTenancy() (before UseAuthorization).
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.MultiTenancy.Middleware;

/// <summary>
/// Middleware for per-request HTTP tenant resolution.
/// Uses <see cref="TenantResolverPipeline"/> and activates <see cref="ICurrentTenant"/>.
/// </summary>
public sealed class TenantResolutionMiddleware : IMiddleware
{
    private readonly ICurrentTenant _currentTenant;
    private readonly TenantResolverPipeline _pipeline;
    private readonly MultiTenancyOptions _options;

    public TenantResolutionMiddleware(
        ICurrentTenant currentTenant,
        TenantResolverPipeline pipeline,
        IOptions<MultiTenancyOptions> options)
    {
        _currentTenant = currentTenant;
        _pipeline = pipeline;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!_options.IsEnabled)
        {
            await next(context);
            return;
        }

        TenantInfo? tenant = await _pipeline.ResolveAsync(context, context.RequestAborted);

        if (tenant is not null)
        {
            using IDisposable _ = _currentTenant.Change(tenant.Id, tenant.Name);
            await next(context);
        }
        else
        {
            await next(context);
        }
    }
}
