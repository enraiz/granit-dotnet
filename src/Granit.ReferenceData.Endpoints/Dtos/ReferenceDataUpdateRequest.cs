namespace Granit.ReferenceData.Endpoints.Dtos;

/// <summary>
/// Request body for updating an existing reference data entry.
/// </summary>
/// <param name="LabelEn">Updated display label (English).</param>
/// <param name="SortOrder">Updated display order.</param>
/// <param name="IsActive">Updated active status.</param>
/// <param name="ValidFrom">Updated start of validity period.</param>
/// <param name="ValidTo">Updated end of validity period.</param>
public sealed record ReferenceDataUpdateRequest(
    string LabelEn,
    int SortOrder = 0,
    bool IsActive = true,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null);
