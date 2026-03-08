namespace Granit.Templating.Store;

/// <summary>
/// Paginated result of <see cref="TemplateSummary"/> items for admin list views.
/// </summary>
/// <param name="Items">The matching template summaries for the current page.</param>
/// <param name="TotalCount">The total number of matching items (before pagination).</param>
public sealed record PagedTemplateResult(
    IReadOnlyList<TemplateSummary> Items,
    int TotalCount);
