namespace Granit.ReferenceData.Endpoints.Dtos;

/// <summary>
/// Request body for creating a new reference data entry.
/// </summary>
/// <param name="Code">Unique business key (e.g., "BE", "EUR").</param>
/// <param name="LabelEn">Default display label (English).</param>
/// <param name="SortOrder">Display order (lower values first).</param>
/// <param name="ValidFrom">Optional start of validity period.</param>
/// <param name="ValidTo">Optional end of validity period.</param>
public sealed record ReferenceDataCreateRequest(
    string Code,
    string LabelEn,
    int SortOrder = 0,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null);
