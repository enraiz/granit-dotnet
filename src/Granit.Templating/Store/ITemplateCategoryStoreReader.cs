namespace Granit.Templating.Store;

/// <summary>
/// Read-side contract for accessing template categories.
/// </summary>
public interface ITemplateCategoryStoreReader
{
    /// <summary>
    /// Returns all categories ordered by <c>SortOrder</c> then <c>Name</c>.
    /// </summary>
    Task<IReadOnlyList<TemplateCategory>> ListCategoriesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single category by its identifier, or <c>null</c> if not found.
    /// </summary>
    Task<TemplateCategory?> GetCategoryAsync(
        Guid id, CancellationToken ct = default);
}
