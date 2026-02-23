using Granit.MultiTenancy.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Granit.MultiTenancy.Extensions;

/// <summary>
/// Extensions for registering the MultiTenancy middleware in the ASP.NET Core pipeline.
/// </summary>
public static class MultiTenancyApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="TenantResolutionMiddleware"/> to the request pipeline.
    /// Must be positioned after <c>UseAuthentication()</c> and before <c>UseAuthorization()</c>.
    /// </summary>
    public static IApplicationBuilder UseGranitMultiTenancy(
        this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
