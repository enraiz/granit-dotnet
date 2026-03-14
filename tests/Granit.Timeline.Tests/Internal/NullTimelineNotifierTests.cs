using Granit.Timeline.Domain;
using Granit.Timeline.Internal;
using Shouldly;
using Xunit;

namespace Granit.Timeline.Tests.Internal;

public sealed class NullTimelineNotifierTests
{
    private readonly NullTimelineNotifier _notifier = new();

    [Fact]
    public async Task NotifyEntryPostedAsync_CompletesWithoutThrowing()
    {
        TimelineEntry entry = new()
        {
            EntityType = "Patient",
            EntityId = "123",
            EntryType = TimelineEntryType.Comment,
            Body = "Test comment",
            AuthorId = "user-1",
            AuthorName = "Alice",
        };
        List<string> followerIds = ["user-2", "user-3"];

        Func<Task> act = () => _notifier.NotifyEntryPostedAsync(
            entry, followerIds, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task NotifyEntryPostedAsync_WithEmptyFollowers_CompletesWithoutThrowing()
    {
        TimelineEntry entry = new()
        {
            EntityType = "Invoice",
            EntityId = "456",
            EntryType = TimelineEntryType.SystemLog,
            Body = "{}",
            AuthorId = "system",
            AuthorName = "System",
        };
        List<string> followerIds = [];

        Func<Task> act = () => _notifier.NotifyEntryPostedAsync(
            entry, followerIds, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task NotifyEntryPostedAsync_ReturnsCompletedTask()
    {
        TimelineEntry entry = new()
        {
            EntityType = "Patient",
            EntityId = "1",
            EntryType = TimelineEntryType.Comment,
            Body = "text",
            AuthorId = "user-1",
            AuthorName = "Bob",
        };

        Task result = _notifier.NotifyEntryPostedAsync(entry, [], TestContext.Current.CancellationToken);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }

    [Fact]
    public async Task NotifyMentionedUsersAsync_CompletesWithoutThrowing()
    {
        TimelineEntry entry = new()
        {
            EntityType = "Patient",
            EntityId = "789",
            EntryType = TimelineEntryType.Comment,
            Body = "Hey @user-2",
            AuthorId = "user-1",
            AuthorName = "Alice",
        };
        List<string> mentionedUserIds = ["user-2"];

        Func<Task> act = () => _notifier.NotifyMentionedUsersAsync(
            entry, mentionedUserIds, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task NotifyMentionedUsersAsync_WithEmptyMentions_CompletesWithoutThrowing()
    {
        TimelineEntry entry = new()
        {
            EntityType = "Invoice",
            EntityId = "1",
            EntryType = TimelineEntryType.InternalNote,
            Body = "No mentions here",
            AuthorId = "user-1",
            AuthorName = "Alice",
        };
        List<string> mentionedUserIds = [];

        Func<Task> act = () => _notifier.NotifyMentionedUsersAsync(
            entry, mentionedUserIds, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task NotifyMentionedUsersAsync_ReturnsCompletedTask()
    {
        TimelineEntry entry = new()
        {
            EntityType = "Patient",
            EntityId = "1",
            EntryType = TimelineEntryType.Comment,
            Body = "text",
            AuthorId = "user-1",
            AuthorName = "Bob",
        };

        Task result = _notifier.NotifyMentionedUsersAsync(entry, [], TestContext.Current.CancellationToken);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }
}
