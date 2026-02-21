// =============================================================================
// TenantResolutionMiddleware - Résolution et activation du tenant par requête
// =============================================================================
// Middleware ASP.NET Core (IMiddleware) : résout le tenant via le pipeline de
// résolveurs, puis active son contexte dans ICurrentTenant pour toute la
// durée de la requête. Si désactivé ou aucun tenant résolu, passe sans modifier
// le contexte.
//
// Enregistrement : app.UseFoundationMultiTenancy() (avant UseAuthorization).
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.MultiTenancy.Middleware;

/// <summary>
/// Middleware de résolution du tenant courant par requête HTTP.
/// Utilise <see cref="TenantResolverPipeline"/> et active <see cref="ICurrentTenant"/>.
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
