namespace Granit.Cookies.Endpoints;

/// <summary>
/// Configuration options for the cookie consent endpoints.
/// </summary>
public sealed class CookieConsentEndpointsOptions
{
    /// <summary>Optional API version prefix (e.g. "api/v1").</summary>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>Base route prefix for the cookie consent endpoints. Default: "cookies".</summary>
    public string RoutePrefix { get; set; } = "cookies";

    /// <summary>OpenAPI tag name. Default: "Cookies".</summary>
    public string TagName { get; set; } = "Cookies";
}
