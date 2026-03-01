// =============================================================================
// Tests — InMemoryTimelineStore
// =============================================================================
// Verifies CRUD operations, SystemLog immutability guard, and attachment handling.
// =============================================================================

using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Security;
using Granit.Timeline.Domain;
using Granit.Timeline.Internal;
using Granit.Timing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Timeline.Tests;

public sealed class InMemoryTimelineStoreTests
{
    private readonly InMemoryTimelineStore _store;
    private readonly IClock _clock;

    public InMemoryTimelineStoreTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(DateTimeOffset.UtcNow);

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.UserId.Returns("test-user");
        userService.UserName.Returns("Test User");

        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());

        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(false);

        _store = new InMemoryTimelineStore(_clock, userService, guidGenerator, tenant);
    }

    [Fact]
    public async Task PostEntryAsync_Comment_CreatesEntry()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "Hello world",
            ct: TestContext.Current.CancellationToken);

        entry.EntityType.ShouldBe("Patient");
        entry.EntityId.ShouldBe("p-1");
        entry.EntryType.ShouldBe(TimelineEntryType.Comment);
        entry.Body.ShouldBe("Hello world");
        entry.AuthorId.ShouldBe("test-user");
        entry.AuthorName.ShouldBe("Test User");
        entry.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task PostEntryAsync_SystemLog_CreatesImmutableEntry()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Invoice", "inv-42", TimelineEntryType.SystemLog, "{\"change\":\"status\"}",
            ct: TestContext.Current.CancellationToken);

        entry.EntryType.ShouldBe(TimelineEntryType.SystemLog);
        entry.Body.ShouldBe("{\"change\":\"status\"}");
    }

    [Fact]
    public async Task PostEntryAsync_WithParentEntryId_SetsParent()
    {
        TimelineEntry parent = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "Parent",
            ct: TestContext.Current.CancellationToken);

        TimelineEntry reply = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "Reply",
            parentEntryId: parent.Id,
            ct: TestContext.Current.CancellationToken);

        reply.ParentEntryId.ShouldBe(parent.Id);
    }

    [Fact]
    public async Task DeleteEntryAsync_Comment_SoftDeletes()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "To delete",
            ct: TestContext.Current.CancellationToken);

        await _store.DeleteEntryAsync(entry.Id, TestContext.Current.CancellationToken);

        entry.IsDeleted.ShouldBeTrue();
        entry.DeletedAt.ShouldNotBeNull();
        entry.DeletedBy.ShouldBe("test-user");
    }

    [Fact]
    public async Task DeleteEntryAsync_InternalNote_SoftDeletes()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.InternalNote, "Internal note",
            ct: TestContext.Current.CancellationToken);

        await _store.DeleteEntryAsync(entry.Id, TestContext.Current.CancellationToken);

        entry.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteEntryAsync_SystemLog_ThrowsInvalidOperationException()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Invoice", "inv-42", TimelineEntryType.SystemLog, "{\"change\":\"status\"}",
            ct: TestContext.Current.CancellationToken);

        await Should.ThrowAsync<InvalidOperationException>(
            () => _store.DeleteEntryAsync(entry.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteEntryAsync_NonExistentEntry_ThrowsKeyNotFound()
    {
        await Should.ThrowAsync<KeyNotFoundException>(
            () => _store.DeleteEntryAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddAttachmentAsync_CreatesAttachment()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "See attached",
            ct: TestContext.Current.CancellationToken);

        Guid blobId = Guid.NewGuid();
        TimelineAttachment attachment = await _store.AddAttachmentAsync(
            entry.Id, blobId, "report.pdf", "application/pdf", 1024,
            ct: TestContext.Current.CancellationToken);

        attachment.EntryId.ShouldBe(entry.Id);
        attachment.BlobId.ShouldBe(blobId);
        attachment.FileName.ShouldBe("report.pdf");
        attachment.ContentType.ShouldBe("application/pdf");
        attachment.SizeBytes.ShouldBe(1024);
    }

    [Fact]
    public async Task AddAttachmentAsync_NonExistentEntry_ThrowsKeyNotFound()
    {
        await Should.ThrowAsync<KeyNotFoundException>(
            () => _store.AddAttachmentAsync(
                Guid.NewGuid(), Guid.NewGuid(), "file.txt", "text/plain", 100,
                ct: TestContext.Current.CancellationToken));
    }
}
