using Granit.Core.Domain;
using Granit.DataExchange.Export;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Export;

public sealed class ExportJobTests
{
    [Fact]
    public void Inherits_AuditedEntity() =>
        typeof(ExportJob).IsAssignableTo(typeof(AuditedEntity)).ShouldBeTrue();

    [Fact]
    public void DefaultStatus_IsQueued()
    {
        ExportJob job = new()
        {
            DefinitionName = "Test",
            Format = "xlsx",
            RequestJson = "{}",
        };

        job.Status.ShouldBe(ExportJobStatus.Queued);
    }

    [Fact]
    public void NullableProperties_AreNullByDefault()
    {
        ExportJob job = new()
        {
            DefinitionName = "Test",
            Format = "csv",
            RequestJson = "{}",
        };

        job.BlobReference.ShouldBeNull();
        job.FileName.ShouldBeNull();
        job.RowCount.ShouldBeNull();
        job.ErrorMessage.ShouldBeNull();
        job.CompletedAt.ShouldBeNull();
        job.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        Guid tenantId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ExportJob job = new()
        {
            DefinitionName = "Guava.PatientExport",
            Format = "xlsx",
            RequestJson = """{"filter":"active"}""",
            Status = ExportJobStatus.Completed,
            BlobReference = "exports/abc.xlsx",
            FileName = "patients_2026-03-03.xlsx",
            RowCount = 42,
            CompletedAt = now,
            TenantId = tenantId,
        };

        job.DefinitionName.ShouldBe("Guava.PatientExport");
        job.Format.ShouldBe("xlsx");
        job.RequestJson.ShouldBe("""{"filter":"active"}""");
        job.Status.ShouldBe(ExportJobStatus.Completed);
        job.BlobReference.ShouldBe("exports/abc.xlsx");
        job.FileName.ShouldBe("patients_2026-03-03.xlsx");
        job.RowCount.ShouldBe(42);
        job.CompletedAt.ShouldBe(now);
        job.TenantId.ShouldBe(tenantId);
    }
}
