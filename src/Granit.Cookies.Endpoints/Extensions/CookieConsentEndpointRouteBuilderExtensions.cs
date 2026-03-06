using Granit.Cookies.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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

        endpoints
            .MapGet($"{prefix}/config", HandleGetConfigAsync)
            .AllowAnonymous()
            .WithName("GetCookieConsentConfig")
            .WithTags(options.TagName)
            .WithSummary("Returns the cookie consent configuration for CMP setup.")
            .Produces<CookieConsentConfigResponse>();

        return endpoints;
    }

    private static Ok<CookieConsentConfigResponse> HandleGetConfigAsync(
        ICookieRegistry cookieRegistry,
        IThirdPartyServiceRegistry serviceRegistry,
        HttpContext context)
    {
        context.Response.Headers.CacheControl = "public, max-age=3600";

        var cookies = cookieRegistry.GetAll()
            .Select(c => new CookieDefinitionResponse(
                c.Name,
                CategoryToSnakeCase(c.Category),
                c.RetentionDays,
                c.Purpose))
            .ToList();

        var services = serviceRegistry.GetAll()
            .Select(s => new ThirdPartyServiceResponse(
                s.Name,
                CategoryToSnakeCase(s.Category),
                s.CookiePatterns))
            .ToList();

        return TypedResults.Ok(new CookieConsentConfigResponse(cookies, services));
    }

    private static string BuildRoutePrefix(CookieConsentEndpointsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiPrefix))
        {
            return options.RoutePrefix;
        }

        return $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";
    }

    private static string CategoryToSnakeCase(CookieCategory category) => category switch
    {
        CookieCategory.StrictlyNecessary => "strictly_necessary",
        CookieCategory.Preferences => "preferences",
        CookieCategory.Analytics => "analytics",
        CookieCategory.Marketing => "marketing",
        _ => category.ToString().ToLowerInvariant(),
    };
}
