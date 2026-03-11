using Granit.BlobStorage.Domain;
using Granit.BlobStorage.Exceptions;
using Granit.BlobStorage.Internal;
using Granit.BlobStorage.Options;
using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Timing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class DefaultBlobStorageTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IBlobDescriptorReader _reader = Substitute.For<IBlobDescriptorReader>();
    private readonly IBlobDescriptorWriter _writer = Substitute.For<IBlobDescriptorWriter>();
    private readonly IBlobKeyStrategy _keyStrategy = Substitute.For<IBlobKeyStrategy>();
    private readonly IBlobStorageClient _storageClient = Substitute.For<IBlobStorageClient>();
    private readonly IGuidGenerator _guidGenerator = Substitute.For<IGuidGenerator>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly DefaultBlobStorage _sut;

    public DefaultBlobStorageTests()
    {
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(TenantId);
        _clock.Now.Returns(Now);

        _sut = new DefaultBlobStorage(
            _reader,
            _writer,
            _keyStrategy,
            _storageClient,
            _guidGenerator,
            _clock,
            _currentTenant,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions()));
    }

    // ── InitiateUploadAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task InitiateUploadAsync_ShouldSavePendingDescriptorAndReturnTicket()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        _guidGenerator.Create().Returns(blobId);
        _keyStrategy.BuildObjectKey("medical-images", blobId)
            .Returns($"{TenantId}/medical-images/2026/02/{blobId}");
        _keyStrategy.ResolveBucketName("medical-images").Returns("granit-blobs");

        PresignedUploadTicket expectedTicket = new(
            blobId,
            new Uri("https://s3.example.com/presigned"),
            "PUT",
            Now.AddMinutes(15),
            new Dictionary<string, string> { ["Content-Type"] = "image/jpeg" });

        _storageClient.GenerateUploadTicketAsync(
            "granit-blobs",
            Arg.Any<string>(),
            blobId,
            Arg.Any<BlobUploadRequest>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>()).Returns(expectedTicket);

        BlobUploadRequest request = new("radio.jpg", "image/jpeg", 10_000_000);

        // Act
        PresignedUploadTicket ticket = await _sut.InitiateUploadAsync("medical-images", request, TestContext.Current.CancellationToken);

        // Assert
        ticket.ShouldBe(expectedTicket);
    }

    [Fact]
    public async Task InitiateUploadAsync_ShouldSavePendingDescriptorWithCorrectFields()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        _guidGenerator.Create().Returns(blobId);
        string expectedKey = $"{TenantId}/medical-images/2026/02/{blobId}";
        _keyStrategy.BuildObjectKey("medical-images", blobId).Returns(expectedKey);
        _keyStrategy.ResolveBucketName("medical-images").Returns("granit-blobs");

        PresignedUploadTicket ticket = new(blobId, new Uri("https://s3.example.com/up"), "PUT", Now.AddMinutes(15), new Dictionary<string, string>());
        _storageClient.GenerateUploadTicketAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<BlobUploadRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        BlobUploadRequest request = new("ordonnance.pdf", "application/pdf", 5_000_000);

        // Act
        await _sut.InitiateUploadAsync("medical-images", request, TestContext.Current.CancellationToken);

        // Assert
        await _writer.Received(1).SaveAsync(
            Arg.Is<BlobDescriptor>(d =>
                d.Status == BlobStatus.Pending &&
                d.Id == blobId &&
                d.TenantId == TenantId &&
                d.ContainerName == "medical-images" &&
                d.ObjectKey == expectedKey &&
                d.OriginalFileName == "ordonnance.pdf" &&
                d.DeclaredContentType == "application/pdf" &&
                d.CreatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateUploadAsync_ShouldPassCorrectExpiryToUrlGenerator()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        _guidGenerator.Create().Returns(blobId);
        _keyStrategy.BuildObjectKey(Arg.Any<string>(), Arg.Any<Guid>()).Returns("key");
        _keyStrategy.ResolveBucketName(Arg.Any<string>()).Returns("bucket");
        _storageClient.GenerateUploadTicketAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<BlobUploadRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new PresignedUploadTicket(blobId, new Uri("https://s3.example.com/up"), "PUT", Now.AddMinutes(15), new Dictionary<string, string>()));

        BlobStorageOptions customOptions = new() { UploadUrlExpiry = TimeSpan.FromMinutes(30) };
        DefaultBlobStorage sutWithCustomOptions = new(
            _reader, _writer, _keyStrategy, _storageClient,
            _guidGenerator, _clock, _currentTenant,
            Microsoft.Extensions.Options.Options.Create(customOptions));

        // Act
        await sutWithCustomOptions.InitiateUploadAsync("docs", new BlobUploadRequest("file.pdf", "application/pdf", 1_000_000), TestContext.Current.CancellationToken);

        // Assert
        await _storageClient.Received(1).GenerateUploadTicketAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<BlobUploadRequest>(),
            TimeSpan.FromMinutes(30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateUploadAsync_WhenNoActiveTenant_ShouldSucceedWithNullTenantId()
    {
        // Arrange
        _currentTenant.IsAvailable.Returns(false);
        _currentTenant.Id.Returns((Guid?)null);

        // Act
        Func<Task> act = async () =>
            await _sut.InitiateUploadAsync("docs", new BlobUploadRequest("f.pdf", "application/pdf", 1_000));

        // Assert — single-tenant apps must not be blocked
        await Should.NotThrowAsync(act);
        await _writer.Received(1).SaveAsync(
            Arg.Is<BlobDescriptor>(d => d.TenantId == null),
            Arg.Any<CancellationToken>());
    }

    // ── CreateDownloadUrlAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateDownloadUrlAsync_WhenBlobIsValid_ShouldReturnPresignedUrl()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);
        _keyStrategy.ResolveBucketName("medical-images").Returns("granit-blobs");

        PresignedDownloadUrl expectedUrl = new(new Uri("https://s3.example.com/download"), Now.AddMinutes(5));
        _storageClient.GenerateDownloadUrlAsync(
            "granit-blobs", Arg.Any<string>(), Arg.Any<DownloadUrlOptions?>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        // Act
        PresignedDownloadUrl result = await _sut.CreateDownloadUrlAsync("medical-images", blobId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedUrl);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_WhenBlobIsUploading_ShouldThrowBlobNotValidException()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildDescriptorInStatus(blobId, BlobStatus.Uploading);
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);

        // Act
        Func<Task> act = async () => await _sut.CreateDownloadUrlAsync("medical-images", blobId);

        // Assert
        await Should.ThrowAsync<BlobNotValidException>(act);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_WhenBlobNotFound_ShouldThrowBlobNotFoundException()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns((BlobDescriptor?)null);

        // Act
        Func<Task> act = async () => await _sut.CreateDownloadUrlAsync("medical-images", blobId);

        // Assert
        await Should.ThrowAsync<BlobNotFoundException>(act);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenBlobIsValid_ShouldDeleteS3ObjectAndTransitionToDeleted()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);
        _keyStrategy.ResolveBucketName("medical-images").Returns("granit-blobs");

        // Act
        await _sut.DeleteAsync("medical-images", blobId, "RGPD Art. 17", TestContext.Current.CancellationToken);

        // Assert — S3 physically deleted
        await _storageClient.Received(1).DeleteObjectAsync("granit-blobs", descriptor.ObjectKey, Arg.Any<CancellationToken>());
        // Assert — descriptor updated in store with Deleted status
        await _writer.Received(1).UpdateAsync(
            Arg.Is<BlobDescriptor>(d =>
                d.Status == BlobStatus.Deleted &&
                d.DeletionReason == "RGPD Art. 17" &&
                d.DeletedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenBlobAlreadyDeleted_ShouldBeIdempotentAndNotCallS3()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildDescriptorInStatus(blobId, BlobStatus.Deleted);
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);

        // Act
        await _sut.DeleteAsync("medical-images", blobId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert — no S3 call, no store update
        await _storageClient.DidNotReceive().DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _writer.DidNotReceive().UpdateAsync(Arg.Any<BlobDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenBlobNotFound_ShouldThrowBlobNotFoundException()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns((BlobDescriptor?)null);

        // Act
        Func<Task> act = async () => await _sut.DeleteAsync("medical-images", blobId);

        // Assert
        await Should.ThrowAsync<BlobNotFoundException>(act);
        await _storageClient.DidNotReceive().DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldPreserveAuditRecord_AfterS3Delete()
    {
        // Arrange — validates the RGPD/HDS constraint: the DB row must survive deletion
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);
        _keyStrategy.ResolveBucketName(Arg.Any<string>()).Returns("granit-blobs");

        // Act
        await _sut.DeleteAsync("medical-images", blobId, "RGPD erasure", TestContext.Current.CancellationToken);

        // Assert — UpdateAsync called (not a delete from DB)
        await _writer.Received(1).UpdateAsync(Arg.Any<BlobDescriptor>(), Arg.Any<CancellationToken>());
        await _writer.DidNotReceive().SaveAsync(Arg.Any<BlobDescriptor>(), Arg.Any<CancellationToken>());
        // No "hard delete" method should exist on IBlobDescriptorStore — there is none by design.
    }

    // ── GetDescriptorAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDescriptorAsync_WhenBlobExists_ShouldReturnDescriptor()
    {
        // Arrange
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _reader.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);

        // Act
        BlobDescriptor? result = await _sut.GetDescriptorAsync("medical-images", blobId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(descriptor);
    }

    [Fact]
    public async Task GetDescriptorAsync_WhenBlobNotFound_ShouldReturnNull()
    {
        // Arrange
        _reader.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((BlobDescriptor?)null);

        // Act
        BlobDescriptor? result = await _sut.GetDescriptorAsync("medical-images", Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BlobDescriptor BuildValidDescriptor(Guid blobId)
    {
        var descriptor = BlobDescriptor.Create(
            id: blobId,
            tenantId: TenantId,
            containerName: "medical-images",
            objectKey: $"{TenantId}/medical-images/2026/02/{blobId}",
            request: new BlobUploadRequest("radio.jpg", "image/jpeg", 10_000_000L),
            createdAt: Now);
        descriptor.MarkAsUploading();
        descriptor.MarkAsValid("image/jpeg", 512_000, Now.AddSeconds(5));
        return descriptor;
    }

    private static BlobDescriptor BuildDescriptorInStatus(Guid blobId, BlobStatus target)
    {
        var descriptor = BlobDescriptor.Create(
            id: blobId,
            tenantId: TenantId,
            containerName: "medical-images",
            objectKey: $"{TenantId}/medical-images/2026/02/{blobId}",
            request: new BlobUploadRequest("file.jpg", "image/jpeg", 10_000_000L),
            createdAt: Now);

        switch (target)
        {
            case BlobStatus.Uploading:
                descriptor.MarkAsUploading();
                break;
            case BlobStatus.Valid:
                descriptor.MarkAsUploading();
                descriptor.MarkAsValid("image/jpeg", 512_000, Now);
                break;
            case BlobStatus.Rejected:
                descriptor.MarkAsUploading();
                descriptor.MarkAsRejected("test");
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
