using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Granit.DataImport.Domain;
using Granit.DataImport.Endpoints.Dtos;
using Granit.DataImport.Endpoints.Extensions;
using Granit.DataImport.Export;
using Granit.DataImport.Mapping;
using Granit.DataImport.Parsing;
using Granit.DataImport.Pipeline;
using Granit.Timing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Endpoints.Tests;

/// <summary>
/// Integration tests for upload, preview, and mapping confirmation endpoints.
/// Uses a TestServer + NSubstitute mocks.
/// </summary>
public sealed class DataImportUploadEndpointsTests : IAsyncDisposable
{
    private const string AdminRole = "granit-data-import-admin";
    private const string Prefix = "/data-import";

    private readonly IImportJobStore _jobStore = Substitute.For<IImportJobStore>();
    private readonly IImportFileProvider _fileProvider = Substitute.For<IImportFileProvider>();
    private readonly IMappingSuggestionService _mappingService = Substitute.For<IMappingSuggestionService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IImportDefinitionDescriptor _descriptor = Substitute.For<IImportDefinitionDescriptor>();
    private readonly IFileParser _parser = Substitute.For<IFileParser>();
    private readonly WebApplication _app;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _userClient;
    private readonly HttpClient _anonClient;

    public DataImportUploadEndpointsTests()
    {
        _clock.Now.Returns(DateTimeOffset.UtcNow);
        _descriptor.Name.Returns("Test.Import");
        _descriptor.EntityType.Returns(typeof(object));
        _descriptor.MaxFileSizeMb.Returns(10);
        _descriptor.AllowedMimeTypes.Returns(new[] { "text/csv" });
        _descriptor.GetFieldMetadata().Returns([
            new FieldMetadata("Name", "String", "Name", null, true),
        ]);

        _parser.CanParse("text/csv").Returns(true);
        _parser.ExtractHeadersAsync(Arg.Any<Stream>(), Arg.Any<FileParsingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new[] { "Name", "Email" });
        _parser.ReadPreviewAsync(Arg.Any<Stream>(), Arg.Any<FileParsingOptions>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new[] { "Alice", "alice@test.com" } });

        _fileProvider.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("blob-ref-1");
        _fileProvider.OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("Name,Email\nAlice,alice@test.com"))));

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_jobStore);
        builder.Services.AddSingleton(_fileProvider);
        builder.Services.AddSingleton(_mappingService);
        builder.Services.AddSingleton(_clock);
        builder.Services.AddSingleton(_descriptor);
        builder.Services.AddSingleton<IFileParser>(_parser);

        // Required by export endpoints (all endpoints are compiled at startup)
        builder.Services.AddSingleton(Substitute.For<IExportOrchestrator>());
        builder.Services.AddSingleton(Substitute.For<IExportPresetStore>());

        _app = builder.Build();
        _app.MapDataImportEndpoints();
        _app.StartAsync().GetAwaiter().GetResult();

        _adminClient = BuildClient(AdminRole);
        _userClient = BuildClient("regular-user");
        _anonClient = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    // ── POST / (Upload) ─────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_WithValidFile_Returns201()
    {
        // Arrange
        using MultipartFormDataContent content = BuildMultipartContent("test.csv", "text/csv", "Name,Email\nAlice,alice@test.com");
        content.Add(new StringContent("Test.Import"), "definitionName");

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(Prefix, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        ImportJobResponse? result = await response.Content.ReadFromJsonAsync<ImportJobResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result!.DefinitionName.ShouldBe("Test.Import");
        result.Status.ShouldBe(ImportJobStatus.Created);
        await _fileProvider.Received(1).SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _jobStore.Received(1).CreateAsync(Arg.Any<ImportJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_UnknownDefinition_Returns400()
    {
        // Arrange
        using MultipartFormDataContent content = BuildMultipartContent("test.csv", "text/csv", "data");
        content.Add(new StringContent("Unknown.Import"), "definitionName");

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(Prefix, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_FileTooLarge_Returns400()
    {
        // Arrange — descriptor allows 10 MB, we send > 10 MB content type check
        _descriptor.MaxFileSizeMb.Returns(0); // 0 MB = reject everything

        using MultipartFormDataContent content = BuildMultipartContent("test.csv", "text/csv", "data");
        content.Add(new StringContent("Test.Import"), "definitionName");

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(Prefix, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_InvalidMimeType_Returns400()
    {
        // Arrange
        using MultipartFormDataContent content = BuildMultipartContent("test.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "data");
        content.Add(new StringContent("Test.Import"), "definitionName");

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(Prefix, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_WithoutAuth_Returns401()
    {
        // Arrange
        using MultipartFormDataContent content = BuildMultipartContent("test.csv", "text/csv", "data");
        content.Add(new StringContent("Test.Import"), "definitionName");

        // Act
        HttpResponseMessage response = await _anonClient.PostAsync(Prefix, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_WithWrongRole_Returns403()
    {
        // Arrange
        using MultipartFormDataContent content = BuildMultipartContent("test.csv", "text/csv", "data");
        content.Add(new StringContent("Test.Import"), "definitionName");

        // Act
        HttpResponseMessage response = await _userClient.PostAsync(Prefix, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ── POST /{jobId}/preview ───────────────────────────────────────────────

    [Fact]
    public async Task Preview_WhenJobExists_Returns200WithPreview()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        ImportJob job = BuildJob(jobId, ImportJobStatus.Created);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/{jobId}/preview", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ImportPreviewResponse? result = await response.Content.ReadFromJsonAsync<ImportPreviewResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result!.Headers.Count.ShouldBe(2);
        result.PreviewRows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Preview_WhenJobNotFound_Returns404()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/{jobId}/preview", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── PUT /{jobId}/mappings ───────────────────────────────────────────────

    [Fact]
    public async Task ConfirmMappings_WhenJobExists_Returns204()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        ImportJob job = BuildJob(jobId, ImportJobStatus.Previewed);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        ConfirmMappingsRequest request = new([
            new ColumnMapping("Name", "Name", MappingConfidence.Manual),
        ]);

        // Act
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            $"{Prefix}/{jobId}/mappings", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _jobStore.Received(1).UpdateAsync(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Mapped), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmMappings_WhenJobNotFound_Returns404()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((ImportJob?)null);
        ConfirmMappingsRequest request = new([new ColumnMapping("Name", "Name", MappingConfidence.Manual)]);

        // Act
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            $"{Prefix}/{jobId}/mappings", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfirmMappings_EmptyMappings_Returns400()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        ImportJob job = BuildJob(jobId, ImportJobStatus.Previewed);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);
        ConfirmMappingsRequest request = new([]);

        // Act
        HttpResponseMessage response = await _adminClient.PutAsJsonAsync(
            $"{Prefix}/{jobId}/mappings", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private HttpClient BuildClient(string role)
    {
        HttpClient client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, role);
        return client;
    }

    private static MultipartFormDataContent BuildMultipartContent(string fileName, string mimeType, string data)
    {
        MultipartFormDataContent content = [];
        ByteArrayContent fileContent = new(Encoding.UTF8.GetBytes(data));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private static ImportJob BuildJob(Guid id, ImportJobStatus status) =>
        new()
        {
            Id = id,
            DefinitionName = "Test.Import",
            EntityTypeName = "Object",
            OriginalFileName = "test.csv",
            MimeType = "text/csv",
            FileSizeBytes = 100,
            BlobReference = "blob-ref-1",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
