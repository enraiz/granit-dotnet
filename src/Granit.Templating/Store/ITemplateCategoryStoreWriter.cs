namespace Granit.Templating.Store;

/// <summary>
/// Write-side contract for managing template categories.
/// </summary>
public interface ITemplateCategoryStoreWriter
{
    /// <summary>
    /// Creates a new category. Throws if a category with the same name already exists.
    /// </summary>
    /// <param name="name">Category name (must be unique).</param>
    /// <param name="description">Optional description.</param>
    /// <param name="icon">Optional Lucide icon name.</param>
    /// <param name="sortOrder">Display order.</param>
    /// <param name="createdBy">Identity of the user creating the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category.</returns>
    /// <exception cref="InvalidOperationException">A category with the same name already exists.</exception>
    Task<TemplateCategory> CreateCategoryAsync(
        string name,
        string? description,
        string? icon,
        int sortOrder,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="name">New name (must remain unique).</param>
    /// <param name="description">New description.</param>
    /// <param name="icon">New icon name.</param>
    /// <param name="sortOrder">New display order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    /// <exception cref="InvalidOperationException">
    /// Category not found, or another category with the same name already exists.
    /// </exception>
    Task<TemplateCategory> UpdateCategoryAsync(
        Guid id,
        string name,
        string? description,
        string? icon,
        int sortOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category. Throws if templates are still associated with it.
    /// </summary>
    /// <param name="id">Category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Category not found, or templates are still associated with this category.
    /// </exception>
    Task DeleteCategoryAsync(
        Guid id, CancellationToken cancellationToken = default);
}
