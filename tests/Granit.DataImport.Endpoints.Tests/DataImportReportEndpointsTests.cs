using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Granit.DataImport.Domain;
using Granit.DataImport.Endpoints.Dtos;
using Granit.DataImport.Endpoints.Extensions;
using Granit.DataImport.Mapping;
using Granit.DataImport.Parsing;
using Granit.DataImport.Pipeline;
using Granit.DataImport.Reporting;
using Granit.Timing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Endpoints.Tests;

/// <summary>
/// Integration tests for report and correction file endpoints.
/// </summary>
public sealed class DataImportReportEndpointsTests : IAsyncDisposable
{
    private const string AdminRole = "granit-data-import-admin";
    private const string Prefix = "/data-import";

    private readonly IImportJobStore _jobStore = Substitute.For<IImportJobStore>();
    private readonly IImportFileProvider _fileProvider = Substitute.For<IImportFileProvider>();
    private readonly ICorrectionFileGenerator _correctionGenerator = Substitute.For<ICorrectionFileGenerator>();
    private readonly WebApplication _app;
    private readonly HttpClient _adminClient;

    public DataImportReportEndpointsTests()
    {
        _fileProvider.OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("Name,Email\nAlice,alice@test.com"))));

        _correctionGenerator.GenerateAsync(
                Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<ImportReport>(),
                Arg.Any<FileParsingOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("Name,Email,Error\nAlice,,Missing Email"))));

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_jobStore);
        builder.Services.AddSingleton(_fileProvider);
        builder.Services.AddSingleton<ICorrectionFileGenerator>(_correctionGenerator);

        // Required by upload endpoints (all endpoints are compiled at startup)
        builder.Services.AddSingleton(Substitute.For<IClock>());
        builder.Services.AddSingleton(Substitute.For<IMappingSuggestionService>());
        builder.Services.AddSingleton(Substitute.For<IImportDefinitionDescriptor>());
        builder.Services.AddSingleton(Substitute.For<IFileParser>());
        builder.Services.AddSingleton(Substitute.For<IImportCommandDispatcher>());
        builder.Services.AddSingleton(Substitute.For<IImportOrchestrator>());

        _app = builder.Build();
        _app.MapDataImportEndpoints();
        _app.StartAsync().GetAwaiter().GetResult();

        _adminClient = BuildClient(AdminRole);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    // ── GET /{jobId}/report ─────────────────────────────────────────────────

    [Fact]
    public async Task GetReport_WhenJobHasReport_Returns200()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        ImportReport report = BuildReport();
        ImportJob job = BuildJob(jobId, ImportJobStatus.Completed, JsonSerializer.Serialize(report));
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/{jobId}/report", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ImportReportResponse? result = await response.Content.ReadFromJsonAsync<ImportReportResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result!.TotalRows.ShouldBe(10);
        result.FailedRows.ShouldBe(2);
    }

    [Fact]
    public async Task GetReport_WhenJobNotFound_Returns404()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/{jobId}/report", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReport_WhenNoReportJson_Returns404()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        ImportJob job = BuildJob(jobId, ImportJobStatus.Executing, reportJson: null);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/{jobId}/report", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET /{jobId}/correction-file ────────────────────────────────────────

    [Fact]
    public async Task GetCorrectionFile_WhenErrorsExist_ReturnsFile()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        ImportReport report = BuildReport();
        ImportJob job = BuildJob(jobId, ImportJobStatus.PartiallyCompleted, JsonSerializer.Serialize(report));
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/{jobId}/correction-file", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Error");
    }

    [Fact]
    public async Task GetCorrectionFile_WhenNoErrors_Returns204()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        ImportReport report = new()
        {
            TotalRows = 10,
            SucceededRows = 10,
            FailedRows = 0,
            SkippedRows = 0,
            InsertedRows = 10,
            UpdatedRows = 0,
            Duration = TimeSpan.FromSeconds(1),
            FinalStatus = ImportJobStatus.Completed,
            RowErrors = [],
        };
        ImportJob job = BuildJob(jobId, ImportJobStatus.Completed, JsonSerializer.Serialize(report));
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/{jobId}/correction-file", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetCorrectionFile_WhenJobNotFound_Returns404()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/{jobId}/correction-file", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private HttpClient BuildClient(string role)
    {
        HttpClient client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, role);
        return client;
    }

    private static ImportJob BuildJob(Guid id, ImportJobStatus status, string? reportJson) =>
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
            ReportJson = reportJson,
        };

    private static ImportReport BuildReport() =>
        new()
        {
            TotalRows = 10,
            SucceededRows = 8,
            FailedRows = 2,
            SkippedRows = 0,
            InsertedRows = 6,
            UpdatedRows = 2,
            Duration = TimeSpan.FromSeconds(5),
            FinalStatus = ImportJobStatus.PartiallyCompleted,
            RowErrors =
            [
                new ImportRowError(3, ImportRowErrorKind.Validation, ["Required"], "Name is required"),
                new ImportRowError(7, ImportRowErrorKind.Persistence, ["Duplicate"], "Duplicate entry"),
            ],
        };
}
