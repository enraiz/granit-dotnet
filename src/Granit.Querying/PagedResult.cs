namespace Granit.Querying;

/// <summary>
/// Paginated result for query endpoints.
/// </summary>
/// <typeparam name="T">The item type (entity or DTO).</typeparam>
/// <param name="Items">The matching items for the current page.</param>
/// <param name="TotalCount">The total number of matching items (before pagination).</param>
/// <param name="NextCursor">
/// Opaque cursor for the next page (keyset pagination only).
/// <c>null</c> when there are no more pages or when using offset pagination.
/// </param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    string? NextCursor = null);
