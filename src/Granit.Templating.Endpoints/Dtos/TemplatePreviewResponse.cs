namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Response from the template preview endpoint.
/// </summary>
/// <param name="Html">The rendered HTML output.</param>
/// <param name="RevisionId">The revision identifier of the rendered template.</param>
/// <param name="RenderTimeMs">Time taken to render the template, in milliseconds.</param>
public sealed record TemplatePreviewResponse(
    string Html,
    Guid? RevisionId,
    long RenderTimeMs);
