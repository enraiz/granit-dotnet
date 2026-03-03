namespace Granit.DataExchange.Import.Validation;

/// <summary>
/// Result of validating a single mapped entity row.
/// </summary>
public sealed record RowValidationResult
{
    /// <summary>
    /// Indicates whether the entity passed all validation rules.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Field-level validation errors. Empty when <see cref="IsValid"/> is <c>true</c>.
    /// </summary>
    public IReadOnlyList<RowFieldError> Errors { get; init; } = [];
}
