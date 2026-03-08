namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Detailed response for a template key, including both draft and published revisions.
/// </summary>
/// <param name="Name">Logical template name.</param>
/// <param name="Culture">BCP 47 culture tag, or <c>null</c> for culture-neutral templates.</param>
/// <param name="Draft">Current draft revision, or <c>null</c> if no draft exists.</param>
/// <param name="Published">Currently published revision, or <c>null</c> if none is published.</param>
public sealed record TemplateDetailResponse(
    string Name,
    string? Culture,
    TemplateRevisionResponse? Draft,
    TemplateRevisionResponse? Published);
