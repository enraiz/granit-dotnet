namespace Granit.Querying.Meta;

/// <summary>
/// Pagination metadata for frontend auto-configuration.
/// </summary>
/// <param name="DefaultPageSize">The default page size.</param>
/// <param name="MaxPageSize">The maximum allowed page size.</param>
/// <param name="SupportsCursor">Whether keyset/cursor pagination is supported.</param>
public sealed record PaginationMetaDto(
    int DefaultPageSize,
    int MaxPageSize,
    bool SupportsCursor);
