using System.Net;
using Granit.DataExchange.Endpoints.Extensions;
using Granit.DataExchange.Export;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Mapping;
using Granit.DataExchange.Import.Parsing;
using Granit.DataExchange.Import.Pipeline;
using Granit.Timing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Endpoints.Tests;

/// <summary>
/// Integration tests for custom options (ApiPrefix, RequiredRole).
/// </summary>
public sealed class ImportOptionsEndpointsTests
{
    [Fact]
    public async Task MapDataExchangeEndpoints_WithCustomRole_EnforcesCustomRole()
    {
        // Arrange — separate app with a custom required role
        await using WebApplication customApp = BuildApp();
        customApp.MapDataExchangeEndpoints(opts => opts.RequiredRole = "ops-team");
        await customApp.StartAsync(TestContext.Current.CancellationToken);

        // Client with "ops-team" role
        using HttpClient opsClient = customApp.GetTestClient();
        opsClient.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "ops-team");

        // Client with default admin role (should be rejected)
        using HttpClient adminClient = customApp.GetTestClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "granit-data-exchange-admin");

        var jobId = Guid.NewGuid();

        // Act
        HttpResponseMessage opsResponse = await opsClient.GetAsync(
            $"/data-exchange/{jobId}", TestContext.Current.CancellationToken);
        HttpResponseMessage adminResponse = await adminClient.GetAsync(
            $"/data-exchange/{jobId}", TestContext.Current.CancellationToken);

        // Assert — ops client can access (even if 404), admin client is forbidden
        opsResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        adminResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MapDataExchangeEndpoints_WithApiPrefix_RespondsOnPrefixedRoute()
    {
        // Arrange — separate app with ApiPrefix, mock a job so GET returns 200
        IImportJobStore jobStore = Substitute.For<IImportJobStore>();
        var jobId = Guid.NewGuid();
        ImportJob job = new()
        {
            Id = jobId,
            DefinitionName = "Test.Import",
            EntityTypeName = "Object",
            OriginalFileName = "test.csv",
            MimeType = "text/csv",
            FileSizeBytes = 100,
            BlobReference = "blob-ref-1",
            Status = ImportJobStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        await using WebApplication prefixedApp = BuildApp(jobStore);
        prefixedApp.MapDataExchangeEndpoints(opts => opts.ApiPrefix = "api/v1");
        await prefixedApp.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = prefixedApp.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "granit-data-exchange-admin");

        // Act — default route must not be registered
        HttpResponseMessage notFound = await client.GetAsync(
            $"/data-exchange/{jobId}", TestContext.Current.CancellationToken);

        // Act — prefixed route must respond with 200
        HttpResponseMessage ok = await client.GetAsync(
            $"/api/v1/data-exchange/{jobId}", TestContext.Current.CancellationToken);

        // Assert
        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static WebApplication BuildApp(IImportJobStore? jobStore = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        // Register all service dependencies required by endpoint parameter inference
        builder.Services.AddSingleton(jobStore ?? Substitute.For<IImportJobStore>());
        builder.Services.AddSingleton(Substitute.For<IImportFileProvider>());
        builder.Services.AddSingleton(Substitute.For<IImportCommandDispatcher>());
        builder.Services.AddSingleton(Substitute.For<IImportOrchestrator>());
        builder.Services.AddSingleton(Substitute.For<IClock>());
        builder.Services.AddSingleton(Substitute.For<IMappingSuggestionService>());
        builder.Services.AddSingleton(Substitute.For<IImportDefinitionDescriptor>());
        builder.Services.AddSingleton(Substitute.For<IFileParser>());

        // Required by export endpoints (all endpoints are compiled at startup)
        builder.Services.AddSingleton(Substitute.For<IExportOrchestrator>());
        builder.Services.AddSingleton(Substitute.For<IExportPresetStore>());

        return builder.Build();
    }
}
