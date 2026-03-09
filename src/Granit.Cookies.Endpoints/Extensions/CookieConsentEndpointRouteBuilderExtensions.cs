using Granit.Cookies.Endpoints.Dtos;
using Granit.Core.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.Cookies.Endpoints.Extensions;

/// <summary>
/// Extension methods to map cookie consent endpoints on <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class CookieConsentEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps <c>GET /cookies/config</c> — returns the full cookie consent configuration
    /// (internal cookies + third-party services) for CMP setup.
    /// Anonymous endpoint, cacheable.
    /// </summary>
    public static IEndpointRouteBuilder MapGranitCookieConsent(
        this IEndpointRouteBuilder endpoints,
        Action<CookieConsentEndpointsOptions>? configure = null)
    {
        CookieConsentEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = BuildRoutePrefix(options);

        return endpoints.MapGranitModuleConfig<CookieConsentConfigProvider, CookieConsentConfigResponse>(
            prefix, "GetCookieConsentConfig", options.TagName,
            route => route
                .AllowAnonymous()
                .AddEndpointFilter(async (context, next) =>
                {
                    context.HttpContext.Response.Headers.CacheControl = "public, max-age=3600";
                    return await next(context).ConfigureAwait(false);
                }));
    }

    private static string BuildRoutePrefix(CookieConsentEndpointsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiPrefix))
        {
            return options.RoutePrefix;
        }

        return $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";
    }
}
