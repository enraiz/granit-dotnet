namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Paginated response for the template list endpoint.
/// </summary>
/// <param name="Items">Template summaries for the current page.</param>
/// <param name="TotalCount">Total number of matching templates.</param>
public sealed record TemplateListResponse(
    IReadOnlyList<TemplateListItemResponse> Items,
    int TotalCount);
