namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Response representing a template category.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Icon">Optional Lucide icon name.</param>
/// <param name="SortOrder">Display order.</param>
/// <param name="TemplateCount">Number of templates associated with this category.</param>
public sealed record TemplateCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    int SortOrder,
    int TemplateCount);
