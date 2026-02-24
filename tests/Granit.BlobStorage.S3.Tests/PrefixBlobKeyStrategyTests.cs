using FluentAssertions;
using Granit.BlobStorage.S3;
using Granit.BlobStorage.S3.Internal;
using Granit.Core.MultiTenancy;
using Granit.Timing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.BlobStorage.S3.Tests;

public sealed class PrefixBlobKeyStrategyTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly PrefixBlobKeyStrategy _sut;

    public PrefixBlobKeyStrategyTests()
    {
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(TenantId);
        _clock.Now.Returns(Now);

        _sut = new PrefixBlobKeyStrategy(
            _currentTenant,
            _clock,
            Options.Create(new S3BlobOptions { DefaultBucket = "granit-blobs" }));
    }

    // ── BuildObjectKey ────────────────────────────────────────────────────────

    [Fact]
    public void BuildObjectKey_ShouldReturnTenantPrefixedKeyWithDateComponents()
    {
        // Arrange
        Guid blobId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        string key = _sut.BuildObjectKey("medical-images", blobId);

        // Assert
        key.Should().Be($"{TenantId}/medical-images/2026/02/{blobId}");
    }

    [Fact]
    public void BuildObjectKey_ShouldPadMonthWithLeadingZero()
    {
        // Arrange
        _clock.Now.Returns(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        Guid blobId = Guid.NewGuid();

        // Act
        string key = _sut.BuildObjectKey("prescriptions", blobId);

        // Assert — month 3 → "03"
        key.Should().Contain("/2026/03/");
    }

    [Fact]
    public void BuildObjectKey_TwoTenantsWithSameFileName_ShouldProduceDifferentKeys()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        Guid tenantB = Guid.Parse("33333333-3333-3333-3333-333333333333");

        PrefixBlobKeyStrategy strategyB = new(
            BuildTenantSubstitute(tenantB), _clock,
            Options.Create(new S3BlobOptions { DefaultBucket = "granit-blobs" }));

        // Act
        string keyA = _sut.BuildObjectKey("medical-images", blobId);
        string keyB = strategyB.BuildObjectKey("medical-images", blobId);

        // Assert — same blobId, different tenants → different keys
        keyA.Should().NotBe(keyB);
        keyA.Should().StartWith(TenantId.ToString());
        keyB.Should().StartWith(tenantB.ToString());
    }

    [Fact]
    public void BuildObjectKey_WhenNoActiveTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _currentTenant.IsAvailable.Returns(false);
        _currentTenant.Id.Returns((Guid?)null);

        // Act
        Action act = () => _sut.BuildObjectKey("medical-images", Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tenant*");
    }

    // ── ResolveBucketName ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveBucketName_ShouldReturnConfiguredDefaultBucket()
    {
        // Act
        string bucket = _sut.ResolveBucketName("medical-images");

        // Assert
        bucket.Should().Be("granit-blobs");
    }

    [Fact]
    public void ResolveBucketName_ShouldReturnSameBucketRegardlessOfContainer()
    {
        // Act + Assert
        _sut.ResolveBucketName("medical-images").Should().Be("granit-blobs");
        _sut.ResolveBucketName("prescriptions").Should().Be("granit-blobs");
        _sut.ResolveBucketName("avatars").Should().Be("granit-blobs");
    }

    // ── TryExtractTenantId ────────────────────────────────────────────────────

    [Fact]
    public void TryExtractTenantId_WithValidKey_ShouldExtractFirstSegment()
    {
        // Arrange
        string objectKey = $"{TenantId}/medical-images/2026/02/some-blob-id";

        // Act
        bool result = _sut.TryExtractTenantId(objectKey, out string? tenantId);

        // Assert
        result.Should().BeTrue();
        tenantId.Should().Be(TenantId.ToString());
    }

    [Fact]
    public void TryExtractTenantId_WithKeyBuiltByStrategy_ShouldRoundtrip()
    {
        // Arrange
        Guid blobId = Guid.NewGuid();
        string objectKey = _sut.BuildObjectKey("medical-images", blobId);

        // Act
        bool result = _sut.TryExtractTenantId(objectKey, out string? extractedTenantId);

        // Assert
        result.Should().BeTrue();
        extractedTenantId.Should().Be(TenantId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("nokeyslash")]
    public void TryExtractTenantId_WithMalformedKey_ShouldReturnFalse(string malformedKey)
    {
        // Act
        bool result = _sut.TryExtractTenantId(malformedKey, out string? tenantId);

        // Assert
        result.Should().BeFalse();
        tenantId.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ICurrentTenant BuildTenantSubstitute(Guid id)
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(true);
        tenant.Id.Returns(id);
        return tenant;
    }
}
