using Granit.Templating.Exceptions;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Granit.Templating.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IDocumentTemplateStore"/>.
/// Manages the Draft → Published → Archived lifecycle of template revisions.
/// </summary>
/// <remarks>
/// Each operation creates and disposes its own <see cref="TemplatingDbContext"/>
/// via <see cref="IDbContextFactory{TContext}"/>, making concurrent access safe.
/// <para>
/// <strong>HybridCache:</strong> <c>TryGetPublishedAsync</c> caches results in L1 (in-memory)
/// and optional L2 (distributed). Cache entries are invalidated on <c>PublishAsync</c>
/// and <c>UnpublishAsync</c> to prevent stale reads after lifecycle transitions.
/// </para>
/// <para>
/// <strong>HDS compliance:</strong> only <c>Draft</c> revisions are physically deleted.
/// <c>Published</c> → <c>Archived</c> transitions are always preserved for the 3-year audit trail.
/// </para>
/// </remarks>
internal sealed class EfDocumentTemplateStore(
    IDbContextFactory<TemplatingDbContext> contextFactory,
    HybridCache cache,
    ITemplateTransitionHook transitionHook) : IDocumentTemplateStore
{
    /// <inheritdoc/>
    public async Task<TemplateDescriptor?> TryGetPublishedAsync(
        TemplateKey key, CancellationToken ct = default)
    {
        TemplateCacheEntry entry = await cache.GetOrCreateAsync(
            CacheKey(key),
            async innerCt =>
            {
                await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(innerCt);
                TemplateRevisionEntity? entity = await ctx.TemplateRevisions
                    .Where(r => r.TemplateName == key.Name
                                && r.Culture == key.Culture
                                && r.Status == TemplateLifecycleStatus.Published)
                    .FirstOrDefaultAsync(innerCt);

                return entity is null
                    ? TemplateCacheEntry.NotFound
                    : TemplateCacheEntry.From(entity.Content, entity.MimeType, entity.RevisionId);
            },
            cancellationToken: ct);

        return entry.ToDescriptor();
    }

    /// <inheritdoc/>
    public async Task SaveDraftAsync(
        TemplateKey key,
        string content,
        string mimeType,
        string updatedBy,
        CancellationToken ct = default)
    {
        await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(ct);
        TemplateRevisionEntity? existing = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Draft)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            // Update existing draft in place (only one draft per key)
            existing.Content = content;
            existing.MimeType = mimeType;
            existing.CreatedBy = updatedBy;
            existing.CreatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            ctx.TemplateRevisions.Add(new TemplateRevisionEntity
            {
                RevisionId = Guid.NewGuid(),
                TemplateName = key.Name,
                Culture = key.Culture,
                Content = content,
                MimeType = mimeType,
                Status = TemplateLifecycleStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = updatedBy,
            });
        }

        await ctx.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task PublishAsync(
        TemplateKey key,
        string publishedBy,
        CancellationToken ct = default)
    {
        if (!await transitionHook.CanTransitionAsync(TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, ct))
        {
            throw new TemplateTransitionDeniedException(TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published);
        }

        await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(ct);
        TemplateRevisionEntity? draft = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Draft)
            .FirstOrDefaultAsync(ct);

        if (draft is null)
        {
            throw new InvalidOperationException(
                $"Cannot publish template '{key.Name}' (culture: {key.Culture ?? "neutral"}): no draft exists.");
        }

        // Archive any currently published revision (HDS: row is kept, status changes)
        List<TemplateRevisionEntity> currentlyPublished = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Published)
            .ToListAsync(ct);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (TemplateRevisionEntity published in currentlyPublished)
        {
            published.Status = TemplateLifecycleStatus.Archived;
            published.ArchivedAt = now;
            published.ArchivedBy = publishedBy;
        }

        draft.Status = TemplateLifecycleStatus.Published;
        draft.PublishedAt = now;
        draft.PublishedBy = publishedBy;

        await ctx.SaveChangesAsync(ct);

        // Notify hook after persistence (archival of previous + promotion of draft)
        foreach (TemplateRevisionEntity archived in currentlyPublished)
        {
            await transitionHook.OnTransitionedAsync(
                archived.RevisionId, TemplateLifecycleStatus.Published, TemplateLifecycleStatus.Archived, publishedBy, ct);
        }

        await transitionHook.OnTransitionedAsync(
            draft.RevisionId, TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, publishedBy, ct);

        await cache.RemoveAsync(CacheKey(key), ct);
    }

    /// <inheritdoc/>
    public async Task UnpublishAsync(
        TemplateKey key,
        string unpublishedBy,
        CancellationToken ct = default)
    {
        await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(ct);
        List<TemplateRevisionEntity> published = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Published)
            .ToListAsync(ct);

        if (published.Count == 0)
        {
            return; // Idempotent — nothing published to archive
        }

        if (!await transitionHook.CanTransitionAsync(TemplateLifecycleStatus.Published, TemplateLifecycleStatus.Archived, ct))
        {
            throw new TemplateTransitionDeniedException(TemplateLifecycleStatus.Published, TemplateLifecycleStatus.Archived);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (TemplateRevisionEntity entity in published)
        {
            entity.Status = TemplateLifecycleStatus.Archived;
            entity.ArchivedAt = now;
            entity.ArchivedBy = unpublishedBy;
        }

        await ctx.SaveChangesAsync(ct);

        foreach (TemplateRevisionEntity entity in published)
        {
            await transitionHook.OnTransitionedAsync(
                entity.RevisionId, TemplateLifecycleStatus.Published, TemplateLifecycleStatus.Archived, unpublishedBy, ct);
        }

        await cache.RemoveAsync(CacheKey(key), ct);
    }

    /// <inheritdoc/>
    public async Task DeleteDraftAsync(
        TemplateKey key,
        string deletedBy,
        CancellationToken ct = default)
    {
        await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(ct);
        TemplateRevisionEntity? draft = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Draft)
            .FirstOrDefaultAsync(ct);

        if (draft is null)
        {
            throw new InvalidOperationException(
                $"Cannot delete draft for template '{key.Name}' (culture: {key.Culture ?? "neutral"}): no draft exists.");
        }

        // Only drafts are physically deleted. Published/archived rows are kept (HDS).
        ctx.TemplateRevisions.Remove(draft);
        await ctx.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TemplateRevision>> GetHistoryAsync(
        TemplateKey key, CancellationToken ct = default)
    {
        await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(ct);
        return await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name && r.Culture == key.Culture)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new TemplateRevision
            {
                RevisionId = r.RevisionId,
                Content = r.Content,
                MimeType = r.MimeType,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                CreatedBy = r.CreatedBy,
                PublishedAt = r.PublishedAt,
                PublishedBy = r.PublishedBy,
            })
            .ToListAsync(ct);
    }

    private static string CacheKey(TemplateKey key) =>
        $"granit:tmpl:{key.Name}|{key.Culture ?? string.Empty}";
}
