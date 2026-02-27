using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Granit.Templating.Store;
using Microsoft.EntityFrameworkCore;

namespace Granit.Templating.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IDocumentTemplateStore"/>.
/// Manages the Draft → Published → Deprecated lifecycle of template revisions.
/// </summary>
/// <remarks>
/// Each operation creates and disposes its own <see cref="TemplatingDbContext"/>
/// via <see cref="IDbContextFactory{TContext}"/>, making concurrent access safe.
/// <para>
/// <strong>HDS compliance:</strong> only <c>Draft</c> revisions are physically deleted.
/// <c>Published</c> → <c>Deprecated</c> transitions are always preserved for the 3-year audit trail.
/// </para>
/// </remarks>
internal sealed class EfDocumentTemplateStore(
    IDbContextFactory<TemplatingDbContext> contextFactory) : IDocumentTemplateStore
{
    /// <inheritdoc/>
    public async Task<TemplateDescriptor?> TryGetPublishedAsync(
        TemplateKey key, CancellationToken ct = default)
    {
        await using TemplatingDbContext ctx = await contextFactory.CreateDbContextAsync(ct);
        TemplateRevisionEntity? entity = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Published)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            return null;
        }

        return new TemplateDescriptor
        {
            Content = entity.Content,
            MimeType = entity.MimeType,
            RevisionId = entity.RevisionId,
        };
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

        // Deprecate any currently published revision (HDS: row is kept, status changes)
        List<TemplateRevisionEntity> currentlyPublished = await ctx.TemplateRevisions
            .Where(r => r.TemplateName == key.Name
                        && r.Culture == key.Culture
                        && r.Status == TemplateLifecycleStatus.Published)
            .ToListAsync(ct);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (TemplateRevisionEntity published in currentlyPublished)
        {
            published.Status = TemplateLifecycleStatus.Deprecated;
            published.DeprecatedAt = now;
            published.DeprecatedBy = publishedBy;
        }

        draft.Status = TemplateLifecycleStatus.Published;
        draft.PublishedAt = now;
        draft.PublishedBy = publishedBy;

        await ctx.SaveChangesAsync(ct);
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
            return; // Idempotent — nothing published to deprecate
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (TemplateRevisionEntity entity in published)
        {
            entity.Status = TemplateLifecycleStatus.Deprecated;
            entity.DeprecatedAt = now;
            entity.DeprecatedBy = unpublishedBy;
        }

        await ctx.SaveChangesAsync(ct);
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

        // Only drafts are physically deleted. Published/deprecated rows are kept (HDS).
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
}
