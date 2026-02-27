namespace Granit.Templating.Store;

/// <summary>
/// Represents a historical revision of a template in <see cref="IDocumentTemplateStore"/>.
/// </summary>
/// <remarks>
/// Revisions are immutable after publication and preserved indefinitely to satisfy the
/// HDS 3-year audit trail requirement. The <see cref="RevisionId"/> is propagated through
/// the rendering pipeline into the final document output for traceability.
/// </remarks>
public sealed class TemplateRevision
{
    /// <summary>Unique identifier of this revision (propagated into rendered documents).</summary>
    public required Guid RevisionId { get; init; }

    /// <summary>Raw template content (HTML source).</summary>
    public required string Content { get; init; }

    /// <summary>MIME type of the template content (e.g. <c>"text/html"</c>).</summary>
    public required string MimeType { get; init; }

    /// <summary>Lifecycle status at the time of retrieval.</summary>
    public required TemplateLifecycleStatus Status { get; init; }

    /// <summary>UTC timestamp when this revision was created (draft saved).</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Identity of the user who saved this draft.</summary>
    public required string CreatedBy { get; init; }

    /// <summary>
    /// UTC timestamp when this revision was published.
    /// <c>null</c> for revisions that were never published (draft-only, then discarded).
    /// </summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Identity of the user who published this revision. <c>null</c> if not yet published.</summary>
    public string? PublishedBy { get; init; }
}
