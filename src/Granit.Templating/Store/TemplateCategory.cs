namespace Granit.Templating.Store;

/// <summary>
/// Domain model representing a template category for organizing templates by domain.
/// </summary>
public sealed class TemplateCategory
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Display name (e.g. "Patient letters", "Invoices").</summary>
    public required string Name { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional Lucide icon name (e.g. "file-text").</summary>
    public string? Icon { get; init; }

    /// <summary>Display order (ascending). Categories with the same order are sorted by name.</summary>
    public required int SortOrder { get; init; }

    /// <summary>Number of templates currently associated with this category.</summary>
    public required int TemplateCount { get; init; }
}
