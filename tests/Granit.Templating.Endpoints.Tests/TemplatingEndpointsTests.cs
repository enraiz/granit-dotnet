using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentValidation;
using Granit.Templating.Endpoints.Dtos;
using Granit.Templating.Endpoints.Extensions;
using Granit.Templating.Endpoints.Permissions;
using Granit.Templating.Endpoints.Validators;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Store;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Granit.Templating.Endpoints.Tests;

/// <summary>
/// Integration tests for the template administration endpoints
/// (GET/POST/PUT/DELETE). Uses a TestServer + NSubstitute mocks for
/// <see cref="IDocumentTemplateStoreReader"/> and <see cref="IDocumentTemplateStoreWriter"/>.
/// </summary>
public sealed class TemplatingEndpointsTests : IAsyncDisposable
{
    private const string Prefix = "/api/v1/admin/templates";
    private const string ManageRole = "template-admin";

    private readonly IDocumentTemplateStoreReader _storeReader = Substitute.For<IDocumentTemplateStoreReader>();
    private readonly IDocumentTemplateStoreWriter _storeWriter = Substitute.For<IDocumentTemplateStoreWriter>();
    private readonly WebApplication _app;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _anonClient;

    public TemplatingEndpointsTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(TemplatingPermissions.Manage,
                policy => policy.RequireRole(ManageRole));

        builder.Services.AddSingleton(_storeReader);
        builder.Services.AddSingleton(_storeWriter);
        builder.Services.AddSingleton<IValidator<SaveTemplateRequest>, SaveTemplateRequestValidator>();

        _app = builder.Build();
        _app.MapGranitTemplatingAdmin();
        _app.StartAsync().GetAwaiter().GetResult();

        _adminClient = BuildClient(ManageRole);
        _anonClient = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _adminClient.Dispose();
        _anonClient.Dispose();
        await _app.DisposeAsync();
    }

    // =========================================================================
    // GET / — List templates
    // =========================================================================

    [Fact]
    public async Task ListTemplates_WhenStoreNotRegistered_Returns501()
    {
        await using WebApplication app = await BuildAppWithoutStoreAsync();
        using HttpClient client = BuildClient(app, ManageRole);

        HttpResponseMessage response = await client.GetAsync(
            Prefix,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task ListTemplates_WithDefaults_Returns200()
    {
        _storeReader.ListTemplatesAsync(Arg.Any<TemplateListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedTemplateResult([], 0));

        HttpResponseMessage response = await _adminClient.GetAsync(
            Prefix,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TemplateListResponse? result =
            await response.Content.ReadFromJsonAsync<TemplateListResponse>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListTemplates_WithResults_ReturnsMappedItems()
    {
        var summary = new TemplateSummary
        {
            Name = "Billing.Invoice",
            Culture = "fr",
            MimeType = "text/html",
            CurrentStatus = TemplateLifecycleStatus.Draft,
            LastModifiedAt = DateTimeOffset.UtcNow,
            LastModifiedBy = "user-1",
            HasPublishedVersion = false,
        };
        _storeReader.ListTemplatesAsync(Arg.Any<TemplateListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedTemplateResult([summary], 1));

        HttpResponseMessage response = await _adminClient.GetAsync(
            Prefix,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TemplateListResponse? result =
            await response.Content.ReadFromJsonAsync<TemplateListResponse>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Billing.Invoice");
        result.Items[0].Culture.ShouldBe("fr");
    }

    [Fact]
    public async Task ListTemplates_WithInvalidPage_Returns400()
    {
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}?page=0",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTemplates_WithPageSizeOver100_Returns400()
    {
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}?pageSize=101",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTemplates_WithInvalidCulture_Returns400()
    {
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}?culture=en:invalid",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTemplates_PassesFilterToStore()
    {
        _storeReader.ListTemplatesAsync(Arg.Any<TemplateListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedTemplateResult([], 0));

        await _adminClient.GetAsync(
            $"{Prefix}?page=2&pageSize=10&search=invoice&culture=fr",
            TestContext.Current.CancellationToken);

        await _storeReader.Received(1).ListTemplatesAsync(
            Arg.Is<TemplateListFilter>(f =>
                f.Page == 2 && f.PageSize == 10 && f.Search == "invoice" && f.Culture == "fr"),
            Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // GET /{name} — Template detail
    // =========================================================================

    [Fact]
    public async Task GetDetail_WhenStoreNotRegistered_Returns501()
    {
        await using WebApplication app = await BuildAppWithoutStoreAsync();
        using HttpClient client = BuildClient(app, ManageRole);

        HttpResponseMessage response = await client.GetAsync(
            $"{Prefix}/Billing.Invoice",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task GetDetail_WhenNotFound_Returns404()
    {
        _storeReader.TryGetDraftAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns((TemplateRevision?)null);
        _storeReader.TryGetPublishedAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns((TemplateDescriptor?)null);

        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/Billing.Invoice",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDetail_WithDraftOnly_Returns200()
    {
        var draft = new TemplateRevision
        {
            RevisionId = Guid.NewGuid(),
            Content = "<h1>Draft</h1>",
            MimeType = "text/html",
            Status = TemplateLifecycleStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "user-1",
        };
        _storeReader.TryGetDraftAsync(
                Arg.Is<TemplateKey>(k => k.Name == "Billing.Invoice"), Arg.Any<CancellationToken>())
            .Returns(draft);
        _storeReader.TryGetPublishedAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns((TemplateDescriptor?)null);

        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/Billing.Invoice",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TemplateDetailResponse? result =
            await response.Content.ReadFromJsonAsync<TemplateDetailResponse>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Billing.Invoice");
        result.Draft.ShouldNotBeNull();
        result.Draft.Content.ShouldBe("<h1>Draft</h1>");
        result.Published.ShouldBeNull();
    }

    [Fact]
    public async Task GetDetail_WithPublishedRevision_Returns200WithBoth()
    {
        var draft = new TemplateRevision
        {
            RevisionId = Guid.NewGuid(),
            Content = "<h1>Draft v2</h1>",
            MimeType = "text/html",
            Status = TemplateLifecycleStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "user-1",
        };
        var publishedRevision = new TemplateRevision
        {
            RevisionId = Guid.NewGuid(),
            Content = "<h1>Published</h1>",
            MimeType = "text/html",
            Status = TemplateLifecycleStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = "user-1",
            PublishedAt = DateTimeOffset.UtcNow.AddHours(-1),
            PublishedBy = "user-1",
        };
        var descriptor = new TemplateDescriptor
        {
            Content = "<h1>Published</h1>",
            MimeType = "text/html",
            RevisionId = publishedRevision.RevisionId,
        };

        _storeReader.TryGetDraftAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns(draft);
        _storeReader.TryGetPublishedAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns(descriptor);
        _storeReader.GetHistoryAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns(new List<TemplateRevision> { draft, publishedRevision });

        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/Billing.Invoice",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TemplateDetailResponse? result =
            await response.Content.ReadFromJsonAsync<TemplateDetailResponse>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Draft.ShouldNotBeNull();
        result.Published.ShouldNotBeNull();
        result.Published.Content.ShouldBe("<h1>Published</h1>");
    }

    [Fact]
    public async Task GetDetail_WithInvalidName_Returns400()
    {
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/invalid-name",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDetail_WithInvalidCulture_Returns400()
    {
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/Billing.Invoice?culture=en:bad",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // POST / — Create draft
    // =========================================================================

    [Fact]
    public async Task CreateDraft_WhenStoreNotRegistered_Returns501()
    {
        await using WebApplication app = await BuildAppWithoutStoreAsync();
        using HttpClient client = BuildClient(app, ManageRole);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            Prefix,
            new SaveTemplateRequest("Billing.Invoice", null, "<h1>Hello</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task CreateDraft_WithValidRequest_Returns201()
    {
        var createdDraft = new TemplateRevision
        {
            RevisionId = Guid.NewGuid(),
            Content = "<h1>Hello</h1>",
            MimeType = "text/html",
            Status = TemplateLifecycleStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
        };
        _storeReader.TryGetDraftAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns(createdDraft);
        _storeReader.TryGetPublishedAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns((TemplateDescriptor?)null);

        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            Prefix,
            new SaveTemplateRequest("Billing.Invoice", "fr", "<h1>Hello</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        TemplateDetailResponse? result =
            await response.Content.ReadFromJsonAsync<TemplateDetailResponse>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Billing.Invoice");
        result.Draft.ShouldNotBeNull();
        result.Draft.Content.ShouldBe("<h1>Hello</h1>");

        await _storeWriter.Received(1).SaveDraftAsync(
            Arg.Is<TemplateKey>(k => k.Name == "Billing.Invoice" && k.Culture == "fr"),
            "<h1>Hello</h1>",
            "text/html",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraft_WithoutName_Returns400()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            Prefix,
            new SaveTemplateRequest(null, null, "<h1>Hello</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDraft_WithInvalidName_Returns422()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            Prefix,
            new SaveTemplateRequest("invalid", null, "<h1>Hello</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateDraft_WithEmptyContent_Returns422()
    {
        HttpResponseMessage response = await _adminClient.PostAsJsonAsync(
            Prefix,
            new SaveTemplateRequest("Billing.Invoice", null, ""),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // =========================================================================
    // PUT /{name} — Update draft
    // =========================================================================

    [Fact]
    public async Task UpdateDraft_WhenStoreNotRegistered_Returns501()
    {
        await using WebApplication app = await BuildAppWithoutStoreAsync();
        using HttpClient client = BuildClient(app, ManageRole);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"{Prefix}/Billing.Invoice",
            new SaveTemplateRequest(null, null, "<h1>Updated</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task UpdateDraft_WithValidRequest_Returns200()
    {
        var updatedDraft = new TemplateRevision
        {
            RevisionId = Guid.NewGuid(),
            Content = "<h1>Updated</h1>",
            MimeType = "text/html",
            Status = TemplateLifecycleStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
        };
        _storeReader.TryGetDraftAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns(updatedDraft);
        _storeReader.TryGetPublishedAsync(Arg.Any<TemplateKey>(), Arg.Any<CancellationToken>())
            .Returns((TemplateDescriptor?)null);

        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            $"{Prefix}/Billing.Invoice",
            new SaveTemplateRequest(null, "fr", "<h1>Updated</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TemplateDetailResponse? result =
            await response.Content.ReadFromJsonAsync<TemplateDetailResponse>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Billing.Invoice");
        result.Draft.ShouldNotBeNull();
        result.Draft.Content.ShouldBe("<h1>Updated</h1>");

        await _storeWriter.Received(1).SaveDraftAsync(
            Arg.Is<TemplateKey>(k => k.Name == "Billing.Invoice" && k.Culture == "fr"),
            "<h1>Updated</h1>",
            "text/html",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDraft_WithInvalidName_Returns400()
    {
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            $"{Prefix}/bad",
            new SaveTemplateRequest(null, null, "<h1>Updated</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // DELETE /{name}/draft — Delete draft
    // =========================================================================

    [Fact]
    public async Task DeleteDraft_WhenStoreNotRegistered_Returns501()
    {
        await using WebApplication app = await BuildAppWithoutStoreAsync();
        using HttpClient client = BuildClient(app, ManageRole);

        HttpResponseMessage response = await client.DeleteAsync(
            $"{Prefix}/Billing.Invoice/draft",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task DeleteDraft_WithValidRequest_Returns204()
    {
        _storeWriter.DeleteDraftAsync(Arg.Any<TemplateKey>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        HttpResponseMessage response = await _adminClient.DeleteAsync(
            $"{Prefix}/Billing.Invoice/draft",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _storeWriter.Received(1).DeleteDraftAsync(
            Arg.Is<TemplateKey>(k => k.Name == "Billing.Invoice"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDraft_WhenNoDraftExists_Returns404()
    {
        _storeWriter.DeleteDraftAsync(Arg.Any<TemplateKey>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("No draft exists for this key."));

        HttpResponseMessage response = await _adminClient.DeleteAsync(
            $"{Prefix}/Billing.Invoice/draft",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDraft_WithInvalidName_Returns400()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync(
            $"{Prefix}/bad/draft",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteDraft_WithInvalidCulture_Returns400()
    {
        HttpResponseMessage response = await _adminClient.DeleteAsync(
            $"{Prefix}/Billing.Invoice/draft?culture=en:bad",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Security tests
    // =========================================================================

    [Fact]
    public async Task ListTemplates_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync(
            Prefix,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDraft_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await _anonClient.PostAsJsonAsync(
            Prefix,
            new SaveTemplateRequest("Billing.Invoice", null, "<h1>Hello</h1>"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDraft_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await _anonClient.DeleteAsync(
            $"{Prefix}/Billing.Invoice/draft",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTemplates_WithWrongRole_Returns403()
    {
        using HttpClient client = BuildClient(_app, "regular-user");

        HttpResponseMessage response = await client.GetAsync(
            Prefix,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // Route prefix tests
    // =========================================================================

    [Fact]
    public async Task MapGranitTemplatingAdmin_WithCustomPrefix_RespondsOnCustomRoute()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(TemplatingPermissions.Manage,
                policy => policy.RequireRole(ManageRole));
        builder.Services.AddSingleton(_storeReader);
        builder.Services.AddSingleton(_storeWriter);
        builder.Services.AddSingleton<IValidator<SaveTemplateRequest>, SaveTemplateRequestValidator>();

        _storeReader.ListTemplatesAsync(Arg.Any<TemplateListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedTemplateResult([], 0));

        await using WebApplication app = builder.Build();
        app.MapGranitTemplatingAdmin(opts =>
        {
            opts.ApiPrefix = "api/v2";
            opts.RoutePrefix = "templates";
        });
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = BuildClient(app, ManageRole);

        HttpResponseMessage notFound = await client.GetAsync(
            Prefix,
            TestContext.Current.CancellationToken);

        HttpResponseMessage ok = await client.GetAsync(
            "/api/v2/templates",
            TestContext.Current.CancellationToken);

        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static async Task<WebApplication> BuildAppWithoutStoreAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(TemplatingPermissions.Manage,
                policy => policy.RequireRole(ManageRole));

        WebApplication app = builder.Build();
        app.MapGranitTemplatingAdmin();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private HttpClient BuildClient(string role) => BuildClient(_app, role);

    private static HttpClient BuildClient(WebApplication app, string role)
    {
        HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, role);
        return client;
    }

    // =========================================================================
    // Fake authentication handler
    // =========================================================================

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string RolesHeader = "X-Test-Roles";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RolesHeader, out Microsoft.Extensions.Primitives.StringValues rolesHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string[] roles = rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            Claim[] claims =
            [
                new(ClaimTypes.Name, "test-user"),
                .. roles.Select(r => new Claim(ClaimTypes.Role, r.Trim())),
            ];

            ClaimsIdentity identity = new(claims, SchemeName);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
