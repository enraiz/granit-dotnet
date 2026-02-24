using FluentAssertions;
using Granit.BlobStorage.Exceptions;
using Granit.BlobStorage.Internal;
using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Timing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class DefaultBlobStorageTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IBlobDescriptorStore _store = Substitute.For<IBlobDescriptorStore>();
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
            _store,
            _keyStrategy,
            _storageClient,
            _guidGenerator,
            _clock,
            _currentTenant,
            Options.Create(new BlobStorageOptions()));
    }

    // ── InitiateUploadAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task InitiateUploadAsync_ShouldSavePendingDescriptorAndReturnTicket()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
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
        ticket.Should().Be(expectedTicket);
    }

    [Fact]
    public async Task InitiateUploadAsync_ShouldSavePendingDescriptorWithCorrectFields()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
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
        await _store.Received(1).SaveAsync(
            Arg.Is<BlobDescriptor>(d =>
                d.Status == BlobStatus.Pending &&
                d.Id == blobId &&
                d.TenantId == TenantId.ToString() &&
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
        Guid blobId = Guid.NewGuid();
        _guidGenerator.Create().Returns(blobId);
        _keyStrategy.BuildObjectKey(Arg.Any<string>(), Arg.Any<Guid>()).Returns("key");
        _keyStrategy.ResolveBucketName(Arg.Any<string>()).Returns("bucket");
        _storageClient.GenerateUploadTicketAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<BlobUploadRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new PresignedUploadTicket(blobId, new Uri("https://s3.example.com/up"), "PUT", Now.AddMinutes(15), new Dictionary<string, string>()));

        BlobStorageOptions customOptions = new() { UploadUrlExpiry = TimeSpan.FromMinutes(30) };
        DefaultBlobStorage sutWithCustomOptions = new(
            _store, _keyStrategy, _storageClient,
            _guidGenerator, _clock, _currentTenant,
            Options.Create(customOptions));

        // Act
        await sutWithCustomOptions.InitiateUploadAsync("docs", new BlobUploadRequest("file.pdf", "application/pdf", 1_000_000), TestContext.Current.CancellationToken);

        // Assert
        await _storageClient.Received(1).GenerateUploadTicketAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<BlobUploadRequest>(),
            TimeSpan.FromMinutes(30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateUploadAsync_WhenNoActiveTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _currentTenant.IsAvailable.Returns(false);
        _currentTenant.Id.Returns((Guid?)null);

        // Act
        Func<Task> act = async () =>
            await _sut.InitiateUploadAsync("medical-images", new BlobUploadRequest("f.jpg", "image/jpeg", 1_000));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tenant*");
    }

    // ── CreateDownloadUrlAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateDownloadUrlAsync_WhenBlobIsValid_ShouldReturnPresignedUrl()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);
        _keyStrategy.ResolveBucketName("medical-images").Returns("granit-blobs");

        PresignedDownloadUrl expectedUrl = new(new Uri("https://s3.example.com/download"), Now.AddMinutes(5));
        _storageClient.GenerateDownloadUrlAsync(
            "granit-blobs", Arg.Any<string>(), Arg.Any<DownloadUrlOptions?>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        // Act
        PresignedDownloadUrl result = await _sut.CreateDownloadUrlAsync("medical-images", blobId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_WhenBlobIsUploading_ShouldThrowBlobNotValidException()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildDescriptorInStatus(blobId, BlobStatus.Uploading);
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);

        // Act
        Func<Task> act = async () => await _sut.CreateDownloadUrlAsync("medical-images", blobId);

        // Assert
        await act.Should().ThrowAsync<BlobNotValidException>();
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_WhenBlobNotFound_ShouldThrowBlobNotFoundException()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns((BlobDescriptor?)null);

        // Act
        Func<Task> act = async () => await _sut.CreateDownloadUrlAsync("medical-images", blobId);

        // Assert
        await act.Should().ThrowAsync<BlobNotFoundException>();
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenBlobIsValid_ShouldDeleteS3ObjectAndTransitionToDeleted()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);
        _keyStrategy.ResolveBucketName("medical-images").Returns("granit-blobs");

        // Act
        await _sut.DeleteAsync("medical-images", blobId, "RGPD Art. 17", TestContext.Current.CancellationToken);

        // Assert — S3 physically deleted
        await _storageClient.Received(1).DeleteObjectAsync("granit-blobs", descriptor.ObjectKey, Arg.Any<CancellationToken>());
        // Assert — descriptor updated in store with Deleted status
        await _store.Received(1).UpdateAsync(
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
        Guid blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildDescriptorInStatus(blobId, BlobStatus.Deleted);
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);

        // Act
        await _sut.DeleteAsync("medical-images", blobId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert — no S3 call, no store update
        await _storageClient.DidNotReceive().DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().UpdateAsync(Arg.Any<BlobDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenBlobNotFound_ShouldThrowBlobNotFoundException()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns((BlobDescriptor?)null);

        // Act
        Func<Task> act = async () => await _sut.DeleteAsync("medical-images", blobId);

        // Assert
        await act.Should().ThrowAsync<BlobNotFoundException>();
        await _storageClient.DidNotReceive().DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldPreserveAuditRecord_AfterS3Delete()
    {
        // Arrange — validates the RGPD/HDS constraint: the DB row must survive deletion
        Guid blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);
        _keyStrategy.ResolveBucketName(Arg.Any<string>()).Returns("granit-blobs");

        // Act
        await _sut.DeleteAsync("medical-images", blobId, "RGPD erasure", TestContext.Current.CancellationToken);

        // Assert — UpdateAsync called (not a delete from DB)
        await _store.Received(1).UpdateAsync(Arg.Any<BlobDescriptor>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveAsync(Arg.Any<BlobDescriptor>(), Arg.Any<CancellationToken>());
        // No "hard delete" method should exist on IBlobDescriptorStore — there is none by design.
    }

    // ── GetDescriptorAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDescriptorAsync_WhenBlobExists_ShouldReturnDescriptor()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        BlobDescriptor descriptor = BuildValidDescriptor(blobId);
        _store.FindAsync(blobId, Arg.Any<CancellationToken>()).Returns(descriptor);

        // Act
        BlobDescriptor? result = await _sut.GetDescriptorAsync("medical-images", blobId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(descriptor);
    }

    [Fact]
    public async Task GetDescriptorAsync_WhenBlobNotFound_ShouldReturnNull()
    {
        // Arrange
        _store.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((BlobDescriptor?)null);

        // Act
        BlobDescriptor? result = await _sut.GetDescriptorAsync("medical-images", Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BlobDescriptor BuildValidDescriptor(Guid blobId)
    {
        BlobDescriptor descriptor = BlobDescriptor.Create(
            id: blobId,
            tenantId: TenantId.ToString(),
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
        BlobDescriptor descriptor = BlobDescriptor.Create(
            id: blobId,
            tenantId: TenantId.ToString(),
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
