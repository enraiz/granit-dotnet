using Granit.Identity.Internal;
using Shouldly;
using Xunit;

namespace Granit.Identity.Tests.Internal;

public sealed class NullUserCacheStatsTests
{
    private readonly NullUserCacheStats _stats = new();

    [Fact]
    public async Task GetCountAsync_ReturnsZero()
    {
        int result = await _stats.GetCountAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetStaleCountAsync_ReturnsZero()
    {
        int result = await _stats.GetStaleCountAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetSyncRangeAsync_ReturnsBothNull()
    {
        (DateTimeOffset? oldest, DateTimeOffset? newest) = await _stats.GetSyncRangeAsync(
            TestContext.Current.CancellationToken);

        oldest.ShouldBeNull();
        newest.ShouldBeNull();
    }

    [Fact]
    public void ImplementsIUserCacheStats() =>
        _stats.ShouldBeAssignableTo<IUserCacheStats>();
}
