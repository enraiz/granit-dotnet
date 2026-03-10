using Granit.MultiTenancy.Options;
using Granit.MultiTenancy.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Granit.MultiTenancy.Middleware;

/// <summary>
/// Middleware for per-request HTTP tenant resolution.
/// Uses <see cref="TenantResolverPipeline"/> and activates <see cref="ICurrentTenant"/>.
/// </summary>
public sealed class TenantResolutionMiddleware(
    ICurrentTenant currentTenant,
    TenantResolverPipeline pipeline,
    IOptions<MultiTenancyOptions> options) : IMiddleware
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly TenantResolverPipeline _pipeline = pipeline;
    private readonly MultiTenancyOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!_options.IsEnabled)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        TenantInfo? tenant = await _pipeline.ResolveAsync(context, context.RequestAborted).ConfigureAwait(false);

        if (tenant is not null)
        {
            using IDisposable _ = _currentTenant.Change(tenant.Id, tenant.Name);
            await next(context).ConfigureAwait(false);
        }
        else
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
