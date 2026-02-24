using FluentAssertions;
using Granit.BlobStorage;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class BlobDescriptorTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

    private static BlobDescriptor CreatePending() => BlobDescriptor.Create(
        id: Guid.NewGuid(),
        tenantId: "tenant-abc",
        containerName: "medical-images",
        objectKey: "tenant-abc/medical-images/2026/02/some-guid",
        originalFileName: "radio.jpg",
        declaredContentType: "image/jpeg",
        maxAllowedBytes: 10_000_000L,
        createdAt: Now);

    // ── Factory ─────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldReturnPendingDescriptorWithCorrectFields()
    {
        Guid id = Guid.NewGuid();

        BlobDescriptor descriptor = BlobDescriptor.Create(
            id: id,
            tenantId: "tenant-abc",
            containerName: "medical-images",
            objectKey: "tenant-abc/medical-images/2026/02/some-guid",
            originalFileName: "radio.jpg",
            declaredContentType: "image/jpeg",
            maxAllowedBytes: 10_000_000L,
            createdAt: Now);

        descriptor.Id.Should().Be(id);
        descriptor.TenantId.Should().Be("tenant-abc");
        descriptor.ContainerName.Should().Be("medical-images");
        descriptor.Status.Should().Be(BlobStatus.Pending);
        descriptor.OriginalFileName.Should().Be("radio.jpg");
        descriptor.DeclaredContentType.Should().Be("image/jpeg");
        descriptor.CreatedAt.Should().Be(Now);
        descriptor.VerifiedContentType.Should().BeNull();
        descriptor.SizeBytes.Should().BeNull();
        descriptor.ValidatedAt.Should().BeNull();
        descriptor.DeletedAt.Should().BeNull();
        descriptor.RejectionReason.Should().BeNull();
        descriptor.DeletionReason.Should().BeNull();
    }

    // ── Pending → Uploading ──────────────────────────────────────────────────

    [Fact]
    public void MarkAsUploading_FromPending_ShouldTransitionToUploading()
    {
        BlobDescriptor descriptor = CreatePending();

        descriptor.MarkAsUploading();

        descriptor.Status.Should().Be(BlobStatus.Uploading);
    }

    [Theory]
    [InlineData(BlobStatus.Uploading)]
    [InlineData(BlobStatus.Valid)]
    [InlineData(BlobStatus.Rejected)]
    [InlineData(BlobStatus.Deleted)]
    public void MarkAsUploading_FromIllegalStatus_ShouldThrow(BlobStatus illegalStatus)
    {
        BlobDescriptor descriptor = BuildDescriptorInStatus(illegalStatus);

        Action act = () => descriptor.MarkAsUploading();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{illegalStatus}*");
    }

    // ── Uploading → Valid ────────────────────────────────────────────────────

    [Fact]
    public void MarkAsValid_FromUploading_ShouldTransitionToValid()
    {
        BlobDescriptor descriptor = CreatePending();
        descriptor.MarkAsUploading();
        DateTimeOffset validatedAt = Now.AddMinutes(2);

        descriptor.MarkAsValid("image/jpeg", 512_000, validatedAt);

        descriptor.Status.Should().Be(BlobStatus.Valid);
        descriptor.VerifiedContentType.Should().Be("image/jpeg");
        descriptor.SizeBytes.Should().Be(512_000);
        descriptor.ValidatedAt.Should().Be(validatedAt);
    }

    [Theory]
    [InlineData(BlobStatus.Pending)]
    [InlineData(BlobStatus.Valid)]
    [InlineData(BlobStatus.Rejected)]
    [InlineData(BlobStatus.Deleted)]
    public void MarkAsValid_FromIllegalStatus_ShouldThrow(BlobStatus illegalStatus)
    {
        BlobDescriptor descriptor = BuildDescriptorInStatus(illegalStatus);

        Action act = () => descriptor.MarkAsValid("image/jpeg", 512_000, Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{illegalStatus}*");
    }

    // ── Uploading → Rejected ─────────────────────────────────────────────────

    [Fact]
    public void MarkAsRejected_FromUploading_ShouldTransitionToRejected()
    {
        BlobDescriptor descriptor = CreatePending();
        descriptor.MarkAsUploading();

        descriptor.MarkAsRejected("MIME mismatch: declared image/jpeg but magic bytes are MZ (executable).");

        descriptor.Status.Should().Be(BlobStatus.Rejected);
        descriptor.RejectionReason.Should().Contain("MZ");
    }

    [Theory]
    [InlineData(BlobStatus.Pending)]
    [InlineData(BlobStatus.Valid)]
    [InlineData(BlobStatus.Rejected)]
    [InlineData(BlobStatus.Deleted)]
    public void MarkAsRejected_FromIllegalStatus_ShouldThrow(BlobStatus illegalStatus)
    {
        BlobDescriptor descriptor = BuildDescriptorInStatus(illegalStatus);

        Action act = () => descriptor.MarkAsRejected("some reason");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{illegalStatus}*");
    }

    // ── Valid → Deleted (Crypto-Shredding) ───────────────────────────────────

    [Fact]
    public void MarkAsDeleted_FromValid_ShouldTransitionToDeletedAndPreserveAuditFields()
    {
        BlobDescriptor descriptor = CreatePending();
        descriptor.MarkAsUploading();
        descriptor.MarkAsValid("image/jpeg", 512_000, Now.AddMinutes(1));
        DateTimeOffset deletedAt = Now.AddDays(30);

        descriptor.MarkAsDeleted(deletedAt, "RGPD Art. 17 erasure request");

        descriptor.Status.Should().Be(BlobStatus.Deleted);
        descriptor.DeletedAt.Should().Be(deletedAt);
        descriptor.DeletionReason.Should().Be("RGPD Art. 17 erasure request");
        // Audit fields must be preserved — the DB record is never removed.
        descriptor.Id.Should().NotBeEmpty();
        descriptor.TenantId.Should().Be("tenant-abc");
        descriptor.OriginalFileName.Should().Be("radio.jpg");
        descriptor.ValidatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsDeleted_WithNoReason_ShouldSucceed()
    {
        BlobDescriptor descriptor = CreatePending();
        descriptor.MarkAsUploading();
        descriptor.MarkAsValid("image/jpeg", 512_000, Now);

        descriptor.MarkAsDeleted(Now.AddDays(1));

        descriptor.Status.Should().Be(BlobStatus.Deleted);
        descriptor.DeletionReason.Should().BeNull();
    }

    [Theory]
    [InlineData(BlobStatus.Pending)]
    [InlineData(BlobStatus.Uploading)]
    [InlineData(BlobStatus.Rejected)]
    [InlineData(BlobStatus.Deleted)]
    public void MarkAsDeleted_FromIllegalStatus_ShouldThrow(BlobStatus illegalStatus)
    {
        BlobDescriptor descriptor = BuildDescriptorInStatus(illegalStatus);

        Action act = () => descriptor.MarkAsDeleted(Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{illegalStatus}*");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static BlobDescriptor BuildDescriptorInStatus(BlobStatus target)
    {
        BlobDescriptor descriptor = CreatePending();
        switch (target)
        {
            case BlobStatus.Pending:
                break;
            case BlobStatus.Uploading:
                descriptor.MarkAsUploading();
                break;
            case BlobStatus.Valid:
                descriptor.MarkAsUploading();
                descriptor.MarkAsValid("image/jpeg", 512_000, Now);
                break;
            case BlobStatus.Rejected:
                descriptor.MarkAsUploading();
                descriptor.MarkAsRejected("test rejection");
                break;
            case BlobStatus.Deleted:
                descriptor.MarkAsUploading();
                descriptor.MarkAsValid("image/jpeg", 512_000, Now);
                descriptor.MarkAsDeleted(Now.AddDays(1));
                break;
        }
        return descriptor;
    }
}
