using System.Runtime.CompilerServices;
using System.Text.Json;
using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Internal;
using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Pipeline;
using Granit.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Export;

public sealed class ExportOrchestratorTests
{
    private readonly IExportJobStore _jobStore = Substitute.For<IExportJobStore>();
    private readonly IExportCommandDispatcher _dispatcher = Substitute.For<IExportCommandDispatcher>();
    private readonly IImportFileProvider _fileProvider = Substitute.For<IImportFileProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTimeOffset _now = new(2026, 3, 3, 10, 0, 0, TimeSpan.Zero);

    public ExportOrchestratorTests()
    {
        _clock.Now.Returns(_now);

        _fileProvider.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("blob-ref-export");

        _jobStore.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                // Return a job matching the requested ID with Queued status by default
                Guid id = call.Arg<Guid>();
                return new ExportJob
                {
                    Id = id,
                    DefinitionName = "Test.Export",
                    Format = "csv",
                    RequestJson = JsonSerializer.Serialize(new ExportRequest(
                        "Test.Export", "csv", null, false, null)),
                    Status = ExportJobStatus.Queued,
                };
            });
    }

    // ── ExportAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_creates_job_and_dispatches()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        ExportRequest request = new("Test.Export", "csv", null, false, null);

        // Act
        ExportJobResult result = await sut.ExportAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ExportJobStatus.Queued);
        result.JobId.ShouldNotBe(Guid.Empty);
        await _jobStore.Received(1).CreateAsync(
            Arg.Is<ExportJob>(j => j.DefinitionName == "Test.Export" && j.Format == "csv"),
            Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<ExecuteExportCommand>(c => c.ExportJobId == result.JobId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportAsync_unknown_definition_throws()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        ExportRequest request = new("Unknown.Export", "csv", null, false, null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.ExportAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportAsync_unknown_format_throws()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        ExportRequest request = new("Test.Export", "pdf", null, false, null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.ExportAsync(request, TestContext.Current.CancellationToken));
    }

    // ── ExecuteAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_processes_rows_and_completes_job()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        ExportJob job = BuildJob(jobId, ExportJobStatus.Queued);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        await sut.ExecuteAsync(jobId, TestContext.Current.CancellationToken);

        // Assert — job is mutated in-place by the orchestrator
        job.Status.ShouldBe(ExportJobStatus.Completed);
        job.RowCount.ShouldBe(2); // TestDataSource yields 2 entities
        job.BlobReference.ShouldBe("blob-ref-export");
        job.FileName.ShouldNotBeNull();
        job.CompletedAt.ShouldBe(_now);
        // 2 calls: Exporting then Completed (same reference, so check call count)
        await _jobStore.Received(2).UpdateAsync(Arg.Any<ExportJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_unknown_job_returns_silently()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((ExportJob?)null);

        // Act — should not throw
        await sut.ExecuteAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        await _jobStore.DidNotReceive().UpdateAsync(Arg.Any<ExportJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_with_selected_fields_filters_columns()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        ExportRequest request = new("Test.Export", "csv", ["Name"], false, null);
        ExportJob job = new()
        {
            Id = jobId,
            DefinitionName = "Test.Export",
            Format = "csv",
            RequestJson = JsonSerializer.Serialize(request),
            Status = ExportJobStatus.Queued,
        };
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        IExportWriter capturedWriter = Substitute.For<IExportWriter>();
        capturedWriter.CanWrite("csv").Returns(true);
        capturedWriter.FileExtension.Returns(".csv");
        capturedWriter.MimeType.Returns("text/csv");

        IReadOnlyList<ExportFieldDescriptor>? capturedFields = null;
        capturedWriter.WriteAsync(
            Arg.Any<Stream>(),
            Arg.Any<IReadOnlyList<ExportFieldDescriptor>>(),
            Arg.Any<IAsyncEnumerable<IReadOnlyDictionary<string, object?>>>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedFields = call.Arg<IReadOnlyList<ExportFieldDescriptor>>();
                return Task.CompletedTask;
            });

        ExportOrchestrator sutWithCapture = CreateOrchestrator(capturedWriter);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        await sutWithCapture.ExecuteAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        capturedFields.ShouldNotBeNull();
        capturedFields!.Count.ShouldBe(1);
        capturedFields[0].PropertyPath.ShouldBe("Name");
    }

    [Fact]
    public async Task ExecuteAsync_with_navigation_field_resolves_dot_notation()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        ExportRequest request = new("Test.Export", "csv", ["Company.Name"], false, null);
        ExportJob job = new()
        {
            Id = jobId,
            DefinitionName = "Test.Export",
            Format = "csv",
            RequestJson = JsonSerializer.Serialize(request),
            Status = ExportJobStatus.Queued,
        };
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        List<IReadOnlyDictionary<string, object?>> capturedRows = [];
        IExportWriter capturedWriter = Substitute.For<IExportWriter>();
        capturedWriter.CanWrite("csv").Returns(true);
        capturedWriter.FileExtension.Returns(".csv");
        capturedWriter.MimeType.Returns("text/csv");
        capturedWriter.WriteAsync(
            Arg.Any<Stream>(),
            Arg.Any<IReadOnlyList<ExportFieldDescriptor>>(),
            Arg.Any<IAsyncEnumerable<IReadOnlyDictionary<string, object?>>>(),
            Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rows =
                    call.Arg<IAsyncEnumerable<IReadOnlyDictionary<string, object?>>>();
                await foreach (IReadOnlyDictionary<string, object?> row in rows)
                {
                    capturedRows.Add(row);
                }
            });

        ExportOrchestrator sutWithCapture = CreateOrchestrator(capturedWriter);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        await sutWithCapture.ExecuteAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        capturedRows.Count.ShouldBe(2);
        capturedRows[0]["Company.Name"].ShouldBe("Acme Corp");
        capturedRows[1]["Company.Name"].ShouldBeNull(); // Jane has no company
    }

    [Fact]
    public async Task ExecuteAsync_failure_sets_job_to_failed()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();
        ExportJob job = BuildJob(jobId, ExportJobStatus.Queued);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        IExportWriter failingWriter = Substitute.For<IExportWriter>();
        failingWriter.CanWrite("csv").Returns(true);
        failingWriter.FileExtension.Returns(".csv");
        failingWriter.MimeType.Returns("text/csv");
        failingWriter.WriteAsync(
            Arg.Any<Stream>(),
            Arg.Any<IReadOnlyList<ExportFieldDescriptor>>(),
            Arg.Any<IAsyncEnumerable<IReadOnlyDictionary<string, object?>>>(),
            Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new IOException("Disk full"));

        ExportOrchestrator sut = CreateOrchestrator(failingWriter);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act & Assert
        await Should.ThrowAsync<IOException>(
            () => sut.ExecuteAsync(jobId, TestContext.Current.CancellationToken));

        job.Status.ShouldBe(ExportJobStatus.Failed);
        job.ErrorMessage.ShouldBe("Disk full");
        job.CompletedAt.ShouldBe(_now);
    }

    // ── GetJobAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetJobAsync_delegates_to_store()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        ExportJob expected = BuildJob(jobId, ExportJobStatus.Completed);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        ExportJob? result = await sut.GetJobAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expected);
    }

    // ── GetDownloadAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetDownloadAsync_completed_job_returns_download()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        ExportJob job = BuildJob(jobId, ExportJobStatus.Completed);
        job.BlobReference = "blob-ref-export";
        job.FileName = "test_export.csv";
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        MemoryStream blobStream = new([1, 2, 3]);
        _fileProvider.OpenAsync("blob-ref-export", Arg.Any<CancellationToken>())
            .Returns(blobStream);

        // Act
        ExportDownload? download = await sut.GetDownloadAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        download.ShouldNotBeNull();
        download!.MimeType.ShouldBe("text/csv");
        download.FileName.ShouldBe("test_export.csv");
    }

    [Fact]
    public async Task GetDownloadAsync_non_completed_job_returns_null()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        ExportJob job = BuildJob(jobId, ExportJobStatus.Exporting);
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        ExportDownload? download = await sut.GetDownloadAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        download.ShouldBeNull();
    }

    [Fact]
    public async Task GetDownloadAsync_unknown_job_returns_null()
    {
        // Arrange
        ExportOrchestrator sut = CreateOrchestrator();
        Guid jobId = Guid.NewGuid();
        _jobStore.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((ExportJob?)null);

        // Act
        ExportDownload? download = await sut.GetDownloadAsync(jobId, TestContext.Current.CancellationToken);

        // Assert
        download.ShouldBeNull();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private ExportOrchestrator CreateOrchestrator(IExportWriter? writerOverride = null)
    {
        ServiceCollection services = new();
        services.AddSingleton<IExportDefinitionDescriptor>(new TestExportDefinition());
        services.AddSingleton<IExportDataSource<TestEntity, EmptyExportFilter>>(new TestDataSource());

        IExportWriter writer = writerOverride ?? CreateCsvWriter();
        ServiceProvider sp = services.BuildServiceProvider();

        return new ExportOrchestrator(
            sp,
            [writer],
            _jobStore,
            _dispatcher,
            _fileProvider,
            _clock,
            Options.Create(new ExportOptions()),
            NullLogger<ExportOrchestrator>.Instance);
    }

    private static IExportWriter CreateCsvWriter()
    {
        IExportWriter writer = Substitute.For<IExportWriter>();
        writer.CanWrite("csv").Returns(true);
        writer.FileExtension.Returns(".csv");
        writer.MimeType.Returns("text/csv");
        // The writer must consume the async enumerable so the orchestrator can count rows
        writer.WriteAsync(
            Arg.Any<Stream>(),
            Arg.Any<IReadOnlyList<ExportFieldDescriptor>>(),
            Arg.Any<IAsyncEnumerable<IReadOnlyDictionary<string, object?>>>(),
            Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rows =
                    call.Arg<IAsyncEnumerable<IReadOnlyDictionary<string, object?>>>();
                await foreach (IReadOnlyDictionary<string, object?> _ in rows)
                {
                    // consume all rows
                }
            });
        return writer;
    }

    private static ExportJob BuildJob(Guid id, ExportJobStatus status) =>
        new()
        {
            Id = id,
            DefinitionName = "Test.Export",
            Format = "csv",
            RequestJson = JsonSerializer.Serialize(new ExportRequest(
                "Test.Export", "csv", null, false, null)),
            Status = status,
        };

    // ── Test types ──────────────────────────────────────────────────────

    private sealed class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public TestCompany? Company { get; set; }
    }

    private sealed class TestCompany
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestExportDefinition : ExportDefinition<TestEntity>
    {
        public override string Name => "Test.Export";

        protected override void Configure(ExportDefinitionBuilder<TestEntity> builder) =>
            builder
                .Field(e => e.Name, f => f.Header("Nom"))
                .Field(e => e.Email)
                .Field(e => e.Company, c => c.Name, f => f.Header("Société"));
    }

    private sealed class TestDataSource : IExportDataSource<TestEntity, EmptyExportFilter>
    {
        public async IAsyncEnumerable<TestEntity> GetDataAsync(
            EmptyExportFilter filter,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            yield return new TestEntity
            {
                Name = "Alice",
                Email = "alice@test.com",
                Company = new TestCompany { Name = "Acme Corp" },
            };
            yield return new TestEntity
            {
                Name = "Jane",
                Email = "jane@test.com",
                Company = null,
            };
        }
    }
}
