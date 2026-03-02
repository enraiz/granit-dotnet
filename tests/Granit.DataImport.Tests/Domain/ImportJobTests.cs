using Granit.Core.Domain;
using Granit.DataImport.Domain;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Tests.Domain;

public sealed class ImportJobTests
{
    [Fact]
    public void ImportJob_inherits_AuditedEntity()
    {
        typeof(ImportJob).BaseType.ShouldBe(typeof(AuditedEntity));
    }

    [Fact]
    public void Default_status_is_Created()
    {
        // Arrange
        ImportJob job = new()
        {
            DefinitionName = "Test",
            EntityTypeName = "TestEntity",
            OriginalFileName = "test.csv",
            MimeType = "text/csv",
            FileSizeBytes = 1024,
            BlobReference = "blob/test.csv",
        };

        // Assert
        job.Status.ShouldBe(ImportJobStatus.Created);
    }

    [Fact]
    public void ImportJobStatus_has_all_lifecycle_states()
    {
        // Assert — 8 states covering the full lifecycle
        Enum.GetValues<ImportJobStatus>().Length.ShouldBe(8);
    }

    [Fact]
    public void ImportJob_has_optional_tenant()
    {
        // Arrange
        ImportJob job = new()
        {
            DefinitionName = "Test",
            EntityTypeName = "TestEntity",
            OriginalFileName = "test.csv",
            MimeType = "text/csv",
            FileSizeBytes = 1024,
            BlobReference = "blob/test.csv",
        };

        // Assert
        job.TenantId.ShouldBeNull();

        job.TenantId = Guid.NewGuid();
        job.TenantId.ShouldNotBeNull();
    }
}
