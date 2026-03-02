using Granit.Authentication.Keycloak.BackChannelLogout;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Authentication.Keycloak.Tests.BackChannelLogout;

public sealed class DistributedCacheRevokedSessionStoreTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly DistributedCacheRevokedSessionStore _sut;

    public DistributedCacheRevokedSessionStoreTests()
    {
        ILogger<DistributedCacheRevokedSessionStore> logger =
            Substitute.For<ILogger<DistributedCacheRevokedSessionStore>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _sut = new DistributedCacheRevokedSessionStore(_cache, logger);
    }

    [Fact]
    public async Task RevokeSessionAsync_ValidSessionId_StoresInCache()
    {
        // Act
        await _sut.RevokeSessionAsync("test-session-123", TimeSpan.FromMinutes(30),
            TestContext.Current.CancellationToken);

        // Assert
        await _cache.Received(1).SetAsync(
            "granit:revoked-session:test-session-123",
            Arg.Is<byte[]>(b => b.Length == 1 && b[0] == 1),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(30)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsSessionRevokedAsync_RevokedSession_ReturnsTrue()
    {
        // Arrange
        _cache.GetAsync("granit:revoked-session:revoked-session", Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });

        // Act
        bool result = await _sut.IsSessionRevokedAsync("revoked-session",
            TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsSessionRevokedAsync_UnknownSession_ReturnsFalse()
    {
        // Arrange
        _cache.GetAsync("granit:revoked-session:unknown-session", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        bool result = await _sut.IsSessionRevokedAsync("unknown-session",
            TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task RevokeSessionAsync_UsesConfiguredTtl_SetsCacheExpiration()
    {
        // Act
        await _sut.RevokeSessionAsync("ttl-test", TimeSpan.FromHours(2),
            TestContext.Current.CancellationToken);

        // Assert
        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(2)),
            Arg.Any<CancellationToken>());
    }
}
