namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Paginated response for the template revision history endpoint.
/// </summary>
/// <param name="Revisions">Revision summaries for the current page (without content).</param>
/// <param name="TotalCount">Total number of revisions for this template key.</param>
/// <param name="Page">Current page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record TemplateHistoryResponse(
    IReadOnlyList<TemplateRevisionSummaryResponse> Revisions,
    int TotalCount,
    int Page,
    int PageSize);
