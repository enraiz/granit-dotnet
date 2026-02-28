using System.Globalization;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Granit.Localization;
using Granit.Localization.Endpoints.Extensions;
using Granit.Localization.Endpoints.Tests.TestResources;
using Granit.Localization.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Localization.Endpoints.Tests;

public sealed class LocalizationEndpointTests : IAsyncDisposable
{
    private static readonly string TestResourcePrefix =
        "Granit.Localization.Endpoints.Tests.TestResources.Localization.Test";

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    public LocalizationEndpointTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddGranitLocalization(options =>
        {
            options.Resources
                .Add<TestResource>("fr")
                .AddJson(typeof(LocalizationEndpointTests).Assembly, TestResourcePrefix);

            options.Languages.Add(new LanguageInfo("fr", "Français", "fr", isDefault: true));
            options.Languages.Add(new LanguageInfo("en", "English", "gb"));
        });

        _app = builder.Build();
        _app.MapGranitLocalization();
        _app.StartAsync().GetAwaiter().GetResult();
        _client = _app.GetTestClient();
    }

    [Fact]
    public async Task GetLocalization_WithFrenchCulture_ReturnsFrenchTranslations()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=fr",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        doc.RootElement.GetProperty("cultureName").GetString().Should().Be("fr");
        doc.RootElement.GetProperty("resources").GetProperty("Test").GetProperty("Hello").GetString().Should().Be("Bonjour");
        doc.RootElement.GetProperty("resources").GetProperty("Test").GetProperty("Goodbye").GetString().Should().Be("Au revoir");
    }

    [Fact]
    public async Task GetLocalization_WithEnglishCulture_ReturnsEnglishTranslations()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=en",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        doc.RootElement.GetProperty("resources").GetProperty("Test").GetProperty("Hello").GetString().Should().Be("Hello");
        doc.RootElement.GetProperty("resources").GetProperty("Test").GetProperty("Goodbye").GetString().Should().Be("Goodbye");
    }

    [Fact]
    public async Task GetLocalization_WithParentCulture_FallsBackToBaseCulture()
    {
        // Act — fr-CA falls back to fr
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=fr-CA",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        doc.RootElement.GetProperty("cultureName").GetString().Should().Be("fr-CA");
        doc.RootElement.GetProperty("resources").GetProperty("Test").GetProperty("Hello").GetString().Should().Be("Bonjour");
    }

    [Fact]
    public async Task GetLocalization_WithInvalidCulture_Returns400()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=en:invalid",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLocalization_WithoutCultureParam_Returns200()
    {
        // Act — falls back to CultureInfo.CurrentUICulture
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLocalization_ReturnsConfiguredLanguages()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=fr",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement languages = doc.RootElement.GetProperty("languages");

        languages.GetArrayLength().Should().Be(2);

        List<string> cultureCodes = [.. languages.EnumerateArray().Select(l => l.GetProperty("cultureName").GetString()!)];
        cultureCodes.Should().Contain("fr").And.Contain("en");
    }

    [Fact]
    public async Task GetLocalization_ReturnsIsDefaultForDefaultLanguage()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=fr",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement languages = doc.RootElement.GetProperty("languages");

        JsonElement frenchLang = languages.EnumerateArray().First(l => l.GetProperty("cultureName").GetString() == "fr");
        frenchLang.GetProperty("isDefault").GetBoolean().Should().BeTrue();

        JsonElement englishLang = languages.EnumerateArray().First(l => l.GetProperty("cultureName").GetString() == "en");
        englishLang.GetProperty("isDefault").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetLocalization_IsAllowedAnonymously()
    {
        // Arrange — no Authorization header
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/granit/localization?cultureName=fr");

        // Act
        HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetLocalization_HasCacheControlHeader()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=fr",
            TestContext.Current.CancellationToken);

        // Assert
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task GetLocalization_HasVaryHeader()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(
            "/api/granit/localization?cultureName=fr",
            TestContext.Current.CancellationToken);

        // Assert
        response.Headers.Vary.Should().Contain("Accept-Language");
    }

    [Fact]
    public async Task MapGranitLocalization_WithCustomRoutePrefix_RespondsOnCustomRoute()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGranitLocalization(opts => opts.Resources.Add<TestResource>("fr"));

        WebApplication app = builder.Build();
        app.MapGranitLocalization(opts => opts.RoutePrefix = "api/v1/granit/localization");
        await app.StartAsync(TestContext.Current.CancellationToken);
        HttpClient client = app.GetTestClient();

        try
        {
            // Default route must not be registered
            HttpResponseMessage notFound = await client.GetAsync(
                "/api/granit/localization",
                TestContext.Current.CancellationToken);
            notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Custom route must respond
            HttpResponseMessage ok = await client.GetAsync(
                "/api/v1/granit/localization",
                TestContext.Current.CancellationToken);
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            client.Dispose();
            await app.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
