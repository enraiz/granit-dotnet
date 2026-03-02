// =============================================================================
// Tests — InMemoryTimelineQuery
// =============================================================================
// Verifies pagination, ordering, and soft-delete filtering.
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

public sealed class InMemoryTimelineQueryTests
{
    private readonly InMemoryTimelineStore _store;
    private readonly InMemoryTimelineQuery _query;
    private readonly IClock _clock;
    private int _timeOffset;

    public InMemoryTimelineQueryTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(_ => DateTimeOffset.UtcNow.AddMinutes(_timeOffset++));

        ICurrentUserService userService = Substitute.For<ICurrentUserService>();
        userService.UserId.Returns("test-user");
        userService.UserName.Returns("Test User");

        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());

        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(false);

        _store = new InMemoryTimelineStore(_clock, userService, guidGenerator, tenant);
        _query = new InMemoryTimelineQuery(_store);
    }

    [Fact]
    public async Task GetStreamAsync_EmptyStream_ReturnsEmptyPage()
    {
        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", ct: TestContext.Current.CancellationToken);

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetStreamAsync_ReturnsEntriesOrderedByDateDescending()
    {
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, "First", ct: TestContext.Current.CancellationToken);
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, "Second", ct: TestContext.Current.CancellationToken);
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, "Third", ct: TestContext.Current.CancellationToken);

        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", ct: TestContext.Current.CancellationToken);

        page.TotalCount.ShouldBe(3);
        page.Items[0].Body.ShouldBe("Third");
        page.Items[1].Body.ShouldBe("Second");
        page.Items[2].Body.ShouldBe("First");
    }

    [Fact]
    public async Task GetStreamAsync_Pagination_SkipAndTake()
    {
        for (int i = 0; i < 10; i++)
        {
            await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, $"Entry {i}", ct: TestContext.Current.CancellationToken);
        }

        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", skip: 3, take: 2, ct: TestContext.Current.CancellationToken);

        page.TotalCount.ShouldBe(10);
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetStreamAsync_ExcludesSoftDeletedEntries()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "To delete", ct: TestContext.Current.CancellationToken);
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, "Visible", ct: TestContext.Current.CancellationToken);
        await _store.DeleteEntryAsync(entry.Id, TestContext.Current.CancellationToken);

        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", ct: TestContext.Current.CancellationToken);

        page.TotalCount.ShouldBe(1);
        page.Items[0].Body.ShouldBe("Visible");
    }

    [Fact]
    public async Task GetStreamAsync_FiltersbyEntityTypeAndId()
    {
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, "Patient entry", ct: TestContext.Current.CancellationToken);
        await _store.PostEntryAsync("Invoice", "inv-1", TimelineEntryType.Comment, "Invoice entry", ct: TestContext.Current.CancellationToken);

        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", ct: TestContext.Current.CancellationToken);

        page.TotalCount.ShouldBe(1);
        page.Items[0].Body.ShouldBe("Patient entry");
    }

    [Fact]
    public async Task GetStreamAsync_IncludesAttachments()
    {
        TimelineEntry entry = await _store.PostEntryAsync(
            "Patient", "p-1", TimelineEntryType.Comment, "With attachment", ct: TestContext.Current.CancellationToken);
        var blobId = Guid.NewGuid();
        await _store.AddAttachmentAsync(entry.Id, blobId, "report.pdf", "application/pdf", 2048, ct: TestContext.Current.CancellationToken);

        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", ct: TestContext.Current.CancellationToken);

        page.Items[0].Attachments.Count.ShouldBe(1);
        page.Items[0].Attachments[0].FileName.ShouldBe("report.pdf");
        page.Items[0].Attachments[0].BlobId.ShouldBe(blobId);
    }

    [Fact]
    public async Task GetStreamAsync_MapsEntryTypes()
    {
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.Comment, "Comment", ct: TestContext.Current.CancellationToken);
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.SystemLog, "Log", ct: TestContext.Current.CancellationToken);
        await _store.PostEntryAsync("Patient", "p-1", TimelineEntryType.InternalNote, "Note", ct: TestContext.Current.CancellationToken);

        TimelineStreamPage page = await _query.GetStreamAsync(
            "Patient", "p-1", ct: TestContext.Current.CancellationToken);

        page.Items.ShouldContain(e => e.EntryType == TimelineStreamEntryType.Comment);
        page.Items.ShouldContain(e => e.EntryType == TimelineStreamEntryType.SystemLog);
        page.Items.ShouldContain(e => e.EntryType == TimelineStreamEntryType.InternalNote);
    }
}
