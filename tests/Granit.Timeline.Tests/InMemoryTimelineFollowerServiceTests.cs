// =============================================================================
// Tests — InMemoryTimelineFollowerService
// =============================================================================
// Verifies follow, unfollow, listing, and query operations.
// =============================================================================

using Granit.Timeline.Internal;
using Shouldly;
using Xunit;

namespace Granit.Timeline.Tests;

public sealed class InMemoryTimelineFollowerServiceTests
{
    private readonly InMemoryTimelineFollowerService _service = new();

    [Fact]
    public async Task FollowAsync_AddsFollower()
    {
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers = await _service.GetFollowerIdsAsync("Patient", "p-1", TestContext.Current.CancellationToken);
        followers.ShouldContain("user-1");
    }

    [Fact]
    public async Task FollowAsync_DuplicateFollow_DoesNotDuplicate()
    {
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers = await _service.GetFollowerIdsAsync("Patient", "p-1", TestContext.Current.CancellationToken);
        followers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UnfollowAsync_RemovesFollower()
    {
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);
        await _service.UnfollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers = await _service.GetFollowerIdsAsync("Patient", "p-1", TestContext.Current.CancellationToken);
        followers.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnfollowAsync_NonExistentFollower_DoesNotThrow()
    {
        await Should.NotThrowAsync(
            () => _service.UnfollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IsFollowingAsync_WhenFollowing_ReturnsTrue()
    {
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);

        bool result = await _service.IsFollowingAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsFollowingAsync_WhenNotFollowing_ReturnsFalse()
    {
        bool result = await _service.IsFollowingAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetFollowerIdsAsync_MultipleFollowers_ReturnsAll()
    {
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);
        await _service.FollowAsync("user-2", "Patient", "p-1", TestContext.Current.CancellationToken);
        await _service.FollowAsync("user-3", "Patient", "p-1", TestContext.Current.CancellationToken);

        IReadOnlyList<string> followers = await _service.GetFollowerIdsAsync("Patient", "p-1", TestContext.Current.CancellationToken);
        followers.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Followers_IsolatedByEntity()
    {
        await _service.FollowAsync("user-1", "Patient", "p-1", TestContext.Current.CancellationToken);
        await _service.FollowAsync("user-2", "Patient", "p-2", TestContext.Current.CancellationToken);

        IReadOnlyList<string> followersP1 = await _service.GetFollowerIdsAsync("Patient", "p-1", TestContext.Current.CancellationToken);
        IReadOnlyList<string> followersP2 = await _service.GetFollowerIdsAsync("Patient", "p-2", TestContext.Current.CancellationToken);

        followersP1.ShouldContain("user-1");
        followersP1.ShouldNotContain("user-2");
        followersP2.ShouldContain("user-2");
        followersP2.ShouldNotContain("user-1");
    }
}
