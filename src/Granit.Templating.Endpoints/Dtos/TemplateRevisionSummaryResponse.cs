using Granit.Templating.Store;

namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Summary of a template revision (without content, for list views).
/// </summary>
/// <param name="RevisionId">Unique identifier of this revision.</param>
/// <param name="Status">Lifecycle status at the time of retrieval.</param>
/// <param name="CreatedAt">UTC timestamp when this revision was created.</param>
/// <param name="CreatedBy">Identity of the user who created this revision.</param>
/// <param name="PublishedAt">UTC timestamp when this revision was published, or <c>null</c>.</param>
/// <param name="PublishedBy">Identity of the user who published this revision, or <c>null</c>.</param>
/// <param name="ContentLength">Length of the template content in characters.</param>
public sealed record TemplateRevisionSummaryResponse(
    Guid RevisionId,
    TemplateLifecycleStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? PublishedAt,
    string? PublishedBy,
    int ContentLength);
