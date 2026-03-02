using Granit.DataImport.Domain;
using Granit.DataImport.EntityFrameworkCore.Internal;
using Granit.DataImport.EntityFrameworkCore.Internal.Stores;
using Granit.DataImport.EntityFrameworkCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Granit.DataImport.EntityFrameworkCore.Tests.Stores;

public sealed class EfImportJobStoreTests
{
    private static string NewDb() => Guid.NewGuid().ToString();

    private static EfImportJobStore CreateStore(string dbName) =>
        new(new InMemoryDataImportContextFactory(dbName));

    private static ImportJob CreateJob(Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            DefinitionName = "Test.Import",
            EntityTypeName = "TestEntity",
            OriginalFileName = "test.csv",
            MimeType = "text/csv",
            FileSizeBytes = 1024,
            BlobReference = "imports/test.csv",
            Status = ImportJobStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
        };

    [Fact]
    public async Task GetAsync_returns_null_when_not_found()
    {
        // Arrange
        EfImportJobStore store = CreateStore(NewDb());

        // Act
        ImportJob? result = await store.GetAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_persists_job()
    {
        // Arrange
        string dbName = NewDb();
        EfImportJobStore store = CreateStore(dbName);
        ImportJob job = CreateJob();

        // Act
        await store.CreateAsync(job, TestContext.Current.CancellationToken);

        // Assert
        await using DataImportDbContext context = new InMemoryDataImportContextFactory(dbName).CreateDbContext();
        ImportJob? persisted = await context.ImportJobs
            .FirstOrDefaultAsync(j => j.Id == job.Id, TestContext.Current.CancellationToken);
        persisted.ShouldNotBeNull();
        persisted.DefinitionName.ShouldBe("Test.Import");
    }

    [Fact]
    public async Task GetAsync_returns_persisted_job()
    {
        // Arrange
        string dbName = NewDb();
        EfImportJobStore store = CreateStore(dbName);
        ImportJob job = CreateJob();
        await store.CreateAsync(job, TestContext.Current.CancellationToken);

        // Act
        ImportJob? result = await store.GetAsync(
            job.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(job.Id);
        result.EntityTypeName.ShouldBe("TestEntity");
        result.Status.ShouldBe(ImportJobStatus.Created);
    }

    [Fact]
    public async Task UpdateAsync_modifies_existing_job()
    {
        // Arrange
        string dbName = NewDb();
        EfImportJobStore store = CreateStore(dbName);
        ImportJob job = CreateJob();
        await store.CreateAsync(job, TestContext.Current.CancellationToken);

        // Act
        job.Status = ImportJobStatus.Executing;
        job.ModifiedAt = DateTimeOffset.UtcNow;
        job.ModifiedBy = "system";
        await store.UpdateAsync(job, TestContext.Current.CancellationToken);

        // Assert
        ImportJob? result = await store.GetAsync(
            job.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ImportJobStatus.Executing);
    }

    [Fact]
    public async Task CreateAsync_and_GetAsync_roundtrip_all_fields()
    {
        // Arrange
        string dbName = NewDb();
        EfImportJobStore store = CreateStore(dbName);
        Guid tenantId = Guid.NewGuid();
        ImportJob job = CreateJob();
        job.TenantId = tenantId;
        job.MappingsJson = "[{\"sourceColumn\":\"A\"}]";
        job.ReportJson = "{\"totalRows\":100}";
        job.CompletedAt = DateTimeOffset.UtcNow;

        // Act
        await store.CreateAsync(job, TestContext.Current.CancellationToken);
        ImportJob? result = await store.GetAsync(
            job.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.MappingsJson.ShouldBe("[{\"sourceColumn\":\"A\"}]");
        result.ReportJson.ShouldBe("{\"totalRows\":100}");
        result.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_lifecycle_status_transitions()
    {
        // Arrange
        string dbName = NewDb();
        EfImportJobStore store = CreateStore(dbName);
        ImportJob job = CreateJob();
        await store.CreateAsync(job, TestContext.Current.CancellationToken);

        // Act — simulate full lifecycle
        job.Status = ImportJobStatus.Previewed;
        await store.UpdateAsync(job, TestContext.Current.CancellationToken);

        job.Status = ImportJobStatus.Mapped;
        await store.UpdateAsync(job, TestContext.Current.CancellationToken);

        job.Status = ImportJobStatus.Executing;
        await store.UpdateAsync(job, TestContext.Current.CancellationToken);

        job.Status = ImportJobStatus.Completed;
        job.CompletedAt = DateTimeOffset.UtcNow;
        await store.UpdateAsync(job, TestContext.Current.CancellationToken);

        // Assert
        ImportJob? result = await store.GetAsync(
            job.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Status.ShouldBe(ImportJobStatus.Completed);
        result.CompletedAt.ShouldNotBeNull();
    }
}
