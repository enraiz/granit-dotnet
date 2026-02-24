// =============================================================================
// Tests - BlobStorage exceptions
// =============================================================================
// Verifies that BlobNotFoundException and BlobNotValidException expose the
// expected properties, implement the correct interfaces, and carry the right
// error codes for the GranitExceptionHandler localization pipeline.
// =============================================================================

using FluentAssertions;
using Granit.BlobStorage.Exceptions;
using Granit.Core.Exceptions;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class BlobStorageExceptionTests
{
    // -------------------------------------------------------------------------
    // BlobNotFoundException
    // -------------------------------------------------------------------------

    [Fact]
    public void BlobNotFoundException_Properties_AreCorrect()
    {
        Guid blobId = Guid.NewGuid();
        const string containerName = "avatars";

        BlobNotFoundException exception = new(blobId, containerName);

        exception.BlobId.Should().Be(blobId);
        exception.ContainerName.Should().Be(containerName);
    }

    [Fact]
    public void BlobNotFoundException_ErrorCode_IsExpected()
    {
        BlobNotFoundException exception = new(Guid.NewGuid(), "avatars");

        exception.ErrorCode.Should().Be("BlobStorage:NotFound");
    }

    [Fact]
    public void BlobNotFoundException_ImplementsNotFoundException()
    {
        BlobNotFoundException exception = new(Guid.NewGuid(), "avatars");

        exception.Should().BeAssignableTo<NotFoundException>();
    }

    [Fact]
    public void BlobNotFoundException_ImplementsIHasErrorCode()
    {
        BlobNotFoundException exception = new(Guid.NewGuid(), "avatars");

        exception.Should().BeAssignableTo<IHasErrorCode>();
    }

    // -------------------------------------------------------------------------
    // BlobNotValidException
    // -------------------------------------------------------------------------

    [Fact]
    public void BlobNotValidException_Properties_AreCorrect()
    {
        Guid blobId = Guid.NewGuid();
        const BlobStatus currentStatus = BlobStatus.Pending;

        BlobNotValidException exception = new(blobId, currentStatus);

        exception.BlobId.Should().Be(blobId);
        exception.CurrentStatus.Should().Be(currentStatus);
    }

    [Fact]
    public void BlobNotValidException_ErrorCode_IsExpected()
    {
        BlobNotValidException exception = new(Guid.NewGuid(), BlobStatus.Rejected);

        exception.ErrorCode.Should().Be("BlobStorage:NotValid");
    }

    [Fact]
    public void BlobNotValidException_ImplementsIHasErrorCode()
    {
        BlobNotValidException exception = new(Guid.NewGuid(), BlobStatus.Deleted);

        exception.Should().BeAssignableTo<IHasErrorCode>();
    }

    [Fact]
    public void BlobNotValidException_DoesNotInheritNotFoundException()
    {
        // BlobNotValidException maps to 400, not 404; it must not extend NotFoundException.
        BlobNotValidException exception = new(Guid.NewGuid(), BlobStatus.Pending);

        exception.Should().NotBeAssignableTo<NotFoundException>();
    }
}
