using System.Text.Json;
using Granit.Cookies.Klaro.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Cookies.Klaro.Tests;

public sealed class KlaroConsentResolverTests
{
    private readonly KlaroOptions _options = new()
    {
        CookieName = "klaro",
        ServiceMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["google-analytics"] = CookieCategory.Analytics,
            ["matomo"] = CookieCategory.Analytics,
            ["youtube"] = CookieCategory.Marketing,
            ["theme-preference"] = CookieCategory.Preferences,
        },
    };

    private KlaroConsentResolver CreateResolver(KlaroOptions? options = null)
    {
        KlaroOptions opts = options ?? _options;
        IOptions<KlaroOptions> wrappedOptions = Microsoft.Extensions.Options.Options.Create(opts);
        ILogger<KlaroConsentResolver> logger = NullLogger<KlaroConsentResolver>.Instance;
        return new KlaroConsentResolver(wrappedOptions, logger);
    }

    private static DefaultHttpContext CreateHttpContext(string cookieName, string cookieValue)
    {
        DefaultHttpContext context = new();
        context.Request.Headers.Cookie = $"{cookieName}={Uri.EscapeDataString(cookieValue)}";
        return context;
    }

    private static DefaultHttpContext CreateHttpContextNoCookie() => new();

    [Fact]
    public async Task StrictlyNecessary_AlwaysTrue()
    {
        KlaroConsentResolver resolver = CreateResolver();
        DefaultHttpContext httpContext = CreateHttpContextNoCookie();

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.StrictlyNecessary);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task MissingCookie_ReturnsFalse()
    {
        KlaroConsentResolver resolver = CreateResolver();
        DefaultHttpContext httpContext = CreateHttpContextNoCookie();

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EmptyCookie_ReturnsFalse()
    {
        KlaroConsentResolver resolver = CreateResolver();
        DefaultHttpContext httpContext = CreateHttpContext("klaro", "");

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task AllServicesConsented_ReturnsTrue()
    {
        KlaroConsentResolver resolver = CreateResolver();
        string json = JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["google-analytics"] = true,
            ["matomo"] = true,
        });
        DefaultHttpContext httpContext = CreateHttpContext("klaro", json);

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task OneServiceNotConsented_ReturnsFalse()
    {
        KlaroConsentResolver resolver = CreateResolver();
        string json = JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["google-analytics"] = true,
            ["matomo"] = false,
        });
        DefaultHttpContext httpContext = CreateHttpContext("klaro", json);

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ServiceMissing_ReturnsFalse()
    {
        KlaroConsentResolver resolver = CreateResolver();
        string json = JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["google-analytics"] = true,
            // matomo is missing from the cookie
        });
        DefaultHttpContext httpContext = CreateHttpContext("klaro", json);

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task NoMappingsForCategory_ReturnsFalse()
    {
        KlaroOptions options = new()
        {
            CookieName = "klaro",
            ServiceMappings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["google-analytics"] = CookieCategory.Analytics,
            },
        };
        KlaroConsentResolver resolver = CreateResolver(options);
        string json = JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["google-analytics"] = true,
        });
        DefaultHttpContext httpContext = CreateHttpContext("klaro", json);

        // Marketing has no mappings
        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Marketing);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task InvalidJson_ReturnsFalse()
    {
        KlaroConsentResolver resolver = CreateResolver();
        DefaultHttpContext httpContext = CreateHttpContext("klaro", "not-valid-json{{{");

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CustomCookieName_ReadsCookie()
    {
        KlaroOptions options = new()
        {
            CookieName = "my-consent",
            ServiceMappings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["youtube"] = CookieCategory.Marketing,
            },
        };
        KlaroConsentResolver resolver = CreateResolver(options);
        string json = JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["youtube"] = true,
        });
        DefaultHttpContext httpContext = CreateHttpContext("my-consent", json);

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Marketing);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CaseSensitiveServiceNames()
    {
        KlaroConsentResolver resolver = CreateResolver();
        // JSON keys are case-sensitive: "Google-Analytics" ≠ "google-analytics"
        string json = JsonSerializer.Serialize(new Dictionary<string, bool>
        {
            ["Google-Analytics"] = true,
            ["Matomo"] = true,
        });
        DefaultHttpContext httpContext = CreateHttpContext("klaro", json);

        bool result = await resolver.ResolveAsync(httpContext, CookieCategory.Analytics);

        // Should return false because JSON property names are case-sensitive
        result.ShouldBeFalse();
    }
}
