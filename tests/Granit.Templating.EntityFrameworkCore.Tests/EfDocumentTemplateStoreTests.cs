using FluentAssertions;
using Granit.Templating.EntityFrameworkCore.Internal;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Store;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granit.Templating.EntityFrameworkCore.Tests;

public sealed class EfDocumentTemplateStoreTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private sealed class InMemoryContextFactory(string dbName)
        : IDbContextFactory<TemplatingDbContext>
    {
        public TemplatingDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<TemplatingDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);

        public Task<TemplatingDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static EfDocumentTemplateStore CreateStore(string dbName) =>
        new(new InMemoryContextFactory(dbName));

    private static string NewDb() => Guid.NewGuid().ToString();

    // -------------------------------------------------------------------------
    // TryGetPublishedAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TryGetPublishedAsync_WhenNothingPublished_ReturnsNull()
    {
        EfDocumentTemplateStore store = CreateStore(NewDb());
        TemplateDescriptor? result = await store.TryGetPublishedAsync(
            new TemplateKey("Billing.Invoice"),
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetPublishedAsync_AfterPublish_ReturnsDescriptor()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Billing.Invoice");

        await store.SaveDraftAsync(key, "<p>Hello</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(key, "bob",
            TestContext.Current.CancellationToken);

        TemplateDescriptor? result = await store.TryGetPublishedAsync(key,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Content.Should().Be("<p>Hello</p>");
        result.MimeType.Should().Be("text/html");
        result.RevisionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TryGetPublishedAsync_AfterUnpublish_ReturnsNull()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Billing.Invoice");

        await store.SaveDraftAsync(key, "<p>v1</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(key, "bob",
            TestContext.Current.CancellationToken);
        await store.UnpublishAsync(key, "carol",
            TestContext.Current.CancellationToken);

        TemplateDescriptor? result = await store.TryGetPublishedAsync(key,
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetPublishedAsync_WithCulture_ReturnsCultureSpecificTemplate()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey frKey = new("Billing.Invoice", "fr-BE");
        TemplateKey neutralKey = new("Billing.Invoice");

        await store.SaveDraftAsync(frKey, "<p>Français</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(frKey, "bob",
            TestContext.Current.CancellationToken);

        TemplateDescriptor? frResult = await store.TryGetPublishedAsync(frKey,
            TestContext.Current.CancellationToken);
        TemplateDescriptor? neutralResult = await store.TryGetPublishedAsync(neutralKey,
            TestContext.Current.CancellationToken);

        frResult.Should().NotBeNull();
        frResult!.Content.Should().Be("<p>Français</p>");
        neutralResult.Should().BeNull("culture-neutral key is distinct from fr-BE");
    }

    // -------------------------------------------------------------------------
    // SaveDraftAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveDraftAsync_CalledTwice_UpdatesExistingDraft()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Notifications.Welcome");

        await store.SaveDraftAsync(key, "<p>v1</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.SaveDraftAsync(key, "<p>v2</p>", "text/html", "bob",
            TestContext.Current.CancellationToken);

        // Verify only one draft exists (no duplicate rows)
        await using TemplatingDbContext ctx = new InMemoryContextFactory(db).CreateDbContext();
        int count = await ctx.TemplateRevisions.CountAsync(
            r => r.TemplateName == key.Name && r.Status == TemplateLifecycleStatus.Draft,
            TestContext.Current.CancellationToken);

        count.Should().Be(1, "SaveDraftAsync must upsert, not append");

        // Verify the content was updated
        await store.PublishAsync(key, "carol", TestContext.Current.CancellationToken);
        TemplateDescriptor? published = await store.TryGetPublishedAsync(key,
            TestContext.Current.CancellationToken);
        published!.Content.Should().Be("<p>v2</p>", "latest draft content must be published");
    }

    // -------------------------------------------------------------------------
    // PublishAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_DeprecatesPreviousPublishedRevision()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Billing.Invoice");

        // First publication
        await store.SaveDraftAsync(key, "<p>v1</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(key, "bob",
            TestContext.Current.CancellationToken);

        // Second publication
        await store.SaveDraftAsync(key, "<p>v2</p>", "text/html", "carol",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(key, "dave",
            TestContext.Current.CancellationToken);

        await using TemplatingDbContext ctx = new InMemoryContextFactory(db).CreateDbContext();
        int deprecatedCount = await ctx.TemplateRevisions.CountAsync(
            r => r.TemplateName == key.Name && r.Status == TemplateLifecycleStatus.Deprecated,
            TestContext.Current.CancellationToken);
        int publishedCount = await ctx.TemplateRevisions.CountAsync(
            r => r.TemplateName == key.Name && r.Status == TemplateLifecycleStatus.Published,
            TestContext.Current.CancellationToken);

        deprecatedCount.Should().Be(1, "v1 must be deprecated");
        publishedCount.Should().Be(1, "only v2 must be published");
    }

    [Fact]
    public async Task PublishAsync_NoDraftExists_ThrowsInvalidOperationException()
    {
        EfDocumentTemplateStore store = CreateStore(NewDb());
        TemplateKey key = new("Ghost.Template");

        Func<Task> act = () => store.PublishAsync(key, "alice",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no draft*");
    }

    // -------------------------------------------------------------------------
    // UnpublishAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UnpublishAsync_WhenNothingPublished_DoesNotThrow()
    {
        EfDocumentTemplateStore store = CreateStore(NewDb());
        TemplateKey key = new("Ghost.Template");

        Func<Task> act = () => store.UnpublishAsync(key, "alice",
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------------
    // DeleteDraftAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteDraftAsync_RemovesDraftRow()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Billing.Invoice");

        await store.SaveDraftAsync(key, "<p>draft</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.DeleteDraftAsync(key, "alice",
            TestContext.Current.CancellationToken);

        await using TemplatingDbContext ctx = new InMemoryContextFactory(db).CreateDbContext();
        int count = await ctx.TemplateRevisions.CountAsync(
            r => r.TemplateName == key.Name,
            TestContext.Current.CancellationToken);

        count.Should().Be(0, "draft must be physically deleted");
    }

    [Fact]
    public async Task DeleteDraftAsync_NoDraftExists_ThrowsInvalidOperationException()
    {
        EfDocumentTemplateStore store = CreateStore(NewDb());
        TemplateKey key = new("Ghost.Template");

        Func<Task> act = () => store.DeleteDraftAsync(key, "alice",
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no draft*");
    }

    [Fact]
    public async Task DeleteDraftAsync_DeprecatedRowsArePreserved()
    {
        // Ensures published/deprecated revisions survive even after a draft is deleted
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Billing.Invoice");

        // Publish v1, then create a new draft and delete it
        await store.SaveDraftAsync(key, "<p>v1</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(key, "bob",
            TestContext.Current.CancellationToken);
        await store.SaveDraftAsync(key, "<p>v2-draft</p>", "text/html", "carol",
            TestContext.Current.CancellationToken);
        await store.UnpublishAsync(key, "dave",
            TestContext.Current.CancellationToken);
        await store.DeleteDraftAsync(key, "carol",
            TestContext.Current.CancellationToken);

        await using TemplatingDbContext ctx = new InMemoryContextFactory(db).CreateDbContext();
        int deprecatedCount = await ctx.TemplateRevisions.CountAsync(
            r => r.TemplateName == key.Name && r.Status == TemplateLifecycleStatus.Deprecated,
            TestContext.Current.CancellationToken);

        deprecatedCount.Should().Be(1, "deprecated revision must be preserved for HDS audit trail");
    }

    // -------------------------------------------------------------------------
    // GetHistoryAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHistoryAsync_ReturnsAllRevisionsNewestFirst()
    {
        string db = NewDb();
        EfDocumentTemplateStore store = CreateStore(db);
        TemplateKey key = new("Billing.Invoice");

        await store.SaveDraftAsync(key, "<p>v1</p>", "text/html", "alice",
            TestContext.Current.CancellationToken);
        await store.PublishAsync(key, "bob",
            TestContext.Current.CancellationToken);
        await store.SaveDraftAsync(key, "<p>v2</p>", "text/html", "carol",
            TestContext.Current.CancellationToken);

        IReadOnlyList<TemplateRevision> history = await store.GetHistoryAsync(key,
            TestContext.Current.CancellationToken);

        history.Should().HaveCount(2);
        // Draft (v2) was created last → appears first
        history[0].Status.Should().Be(TemplateLifecycleStatus.Draft);
        history[1].Status.Should().Be(TemplateLifecycleStatus.Published);
    }
}
