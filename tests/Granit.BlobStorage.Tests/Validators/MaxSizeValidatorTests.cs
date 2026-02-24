using FluentAssertions;
using Granit.BlobStorage.Validators;
using Xunit;

namespace Granit.BlobStorage.Tests.Validators;

public sealed class MaxSizeValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

    private static BlobValidationContext MakeContext(long actualSizeBytes, long maxAllowedBytes) =>
        new()
        {
            Descriptor = BlobDescriptor.Create(
                id: Guid.NewGuid(),
                tenantId: "tenant-abc",
                containerName: "docs",
                objectKey: "tenant-abc/docs/2026/02/some-id",
                request: new BlobUploadRequest("document.pdf", "application/pdf", maxAllowedBytes),
                createdAt: Now),
            ActualSizeBytes = actualSizeBytes,
            OpenPartialStreamAsync = (_, _) => Task.FromResult<Stream>(Stream.Null),
        };

    [Fact]
    public async Task ValidateAsync_SizeBelowLimit_ReturnsSuccess()
    {
        MaxSizeValidator validator = new();
        BlobValidationContext context = MakeContext(actualSizeBytes: 4_999_999L, maxAllowedBytes: 5_000_000L);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_SizeExactlyAtLimit_ReturnsSuccess()
    {
        MaxSizeValidator validator = new();
        BlobValidationContext context = MakeContext(actualSizeBytes: 5_000_000L, maxAllowedBytes: 5_000_000L);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue("exact limit must be accepted");
    }

    [Fact]
    public async Task ValidateAsync_SizeExceedsLimit_ReturnsFailure()
    {
        MaxSizeValidator validator = new();
        BlobValidationContext context = MakeContext(actualSizeBytes: 5_000_001L, maxAllowedBytes: 5_000_000L);

        BlobValidationResult result = await validator.ValidateAsync(
            context, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Contain("5,000,001");
        result.FailureReason.Should().Contain("5,000,000");
    }

    [Fact]
    public async Task ValidateAsync_DoesNotOpenPartialStream()
    {
        MaxSizeValidator validator = new();
        bool streamOpened = false;
        BlobValidationContext context = new()
        {
            Descriptor = BlobDescriptor.Create(
                id: Guid.NewGuid(),
                tenantId: "t",
                containerName: "c",
                objectKey: "t/c/2026/02/id",
                request: new BlobUploadRequest("f.pdf", "application/pdf", 1_000L),
                createdAt: Now),
            ActualSizeBytes = 500L,
            OpenPartialStreamAsync = (_, _) =>
            {
                streamOpened = true;
                return Task.FromResult<Stream>(Stream.Null);
            },
        };

        await validator.ValidateAsync(context, TestContext.Current.CancellationToken);

        streamOpened.Should().BeFalse("MaxSizeValidator uses ActualSizeBytes from S3 HEAD, not the stream");
    }

    [Fact]
    public void Order_Is20() =>
        new MaxSizeValidator().Order.Should().Be(20);
}
