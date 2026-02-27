using Granit.Templating.Keys;
using Granit.Templating.Pipeline;

namespace Granit.Templating.Store;

/// <summary>
/// Contract for reading and writing template content with draft/published/deprecated lifecycle.
/// </summary>
/// <remarks>
/// Implemented by <c>EfCoreDocumentTemplateStore</c> in <c>Granit.Templating.EntityFrameworkCore</c>.
/// A <c>CachedDocumentTemplateStore</c> decorator wraps it with a hybrid memory/Redis cache.
/// <para>
/// Lifecycle:
/// <list type="number">
///   <item><see cref="SaveDraftAsync"/> — create or update the editable draft for a key.</item>
///   <item><see cref="PublishAsync"/> — promote the current draft to <c>Published</c>,
///     deprecating any previous published revision.</item>
///   <item><see cref="UnpublishAsync"/> — deprecate the published revision without promoting a new one.</item>
///   <item><see cref="DeleteAsync"/> — physically delete a draft (only allowed for drafts).</item>
/// </list>
/// Published and deprecated revisions are never physically deleted (HDS requirement).
/// </para>
/// </remarks>
public interface IDocumentTemplateStore
{
    /// <summary>
    /// Returns the currently published template for the given key, or <c>null</c> if none exists.
    /// </summary>
    /// <param name="key">Template key (name + optional culture).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<TemplateDescriptor?> TryGetPublishedAsync(
        TemplateKey key, CancellationToken ct = default);

    /// <summary>
    /// Creates or replaces the draft for the given key.
    /// </summary>
    /// <remarks>
    /// Only one draft per key is maintained. Calling this method multiple times
    /// replaces the previous draft content.
    /// </remarks>
    /// <param name="key">Template key.</param>
    /// <param name="content">Template source (HTML).</param>
    /// <param name="mimeType">MIME type (e.g. <c>"text/html"</c>).</param>
    /// <param name="updatedBy">Identity of the user saving the draft.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveDraftAsync(
        TemplateKey key,
        string content,
        string mimeType,
        string updatedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Promotes the current draft to <c>Published</c>, deprecating any previous published revision.
    /// </summary>
    /// <param name="key">Template key.</param>
    /// <param name="publishedBy">Identity of the user publishing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">No draft exists for this key.</exception>
    Task PublishAsync(
        TemplateKey key,
        string publishedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Deprecates the currently published revision without promoting a new one.
    /// After this call, <see cref="TryGetPublishedAsync"/> returns <c>null</c> for this key.
    /// </summary>
    /// <param name="key">Template key.</param>
    /// <param name="unpublishedBy">Identity of the user unpublishing.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UnpublishAsync(
        TemplateKey key,
        string unpublishedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Physically deletes the draft for the given key.
    /// </summary>
    /// <remarks>
    /// Only drafts may be deleted. Published and deprecated revisions are preserved
    /// permanently for HDS compliance.
    /// </remarks>
    /// <param name="key">Template key.</param>
    /// <param name="deletedBy">Identity of the user performing the deletion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">No draft exists, or the revision is not a draft.</exception>
    Task DeleteDraftAsync(
        TemplateKey key,
        string deletedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full revision history for the given key, ordered by creation date (newest first).
    /// </summary>
    /// <param name="key">Template key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<TemplateRevision>> GetHistoryAsync(
        TemplateKey key, CancellationToken ct = default);
}
