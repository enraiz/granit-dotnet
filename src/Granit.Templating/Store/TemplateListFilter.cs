using Granit.Templating.Keys;

namespace Granit.Templating.Store;

/// <summary>
/// Filter and pagination parameters for listing templates in the admin store.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="Search">Optional search term applied to <see cref="TemplateKey.Name"/>.</param>
/// <param name="Status">Optional lifecycle status filter.</param>
/// <param name="Culture">Optional BCP 47 culture filter.</param>
public sealed record TemplateListFilter(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    TemplateLifecycleStatus? Status = null,
    string? Culture = null);
