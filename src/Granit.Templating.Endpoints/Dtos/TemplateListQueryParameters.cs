using Granit.Templating.Store;

namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Query string parameters for the GET template list endpoint.
/// </summary>
internal sealed record TemplateListQueryParameters(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    TemplateLifecycleStatus? Status = null,
    string? Culture = null,
    Guid? CategoryId = null);
