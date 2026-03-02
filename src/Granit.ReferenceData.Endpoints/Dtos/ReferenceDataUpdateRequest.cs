namespace Granit.ReferenceData.Endpoints.Dtos;

/// <summary>
/// Request body for updating an existing reference data entry.
/// </summary>
/// <param name="LabelEn">Updated display label (English).</param>
/// <param name="LabelFr">Updated French display label.</param>
/// <param name="LabelNl">Updated Dutch display label.</param>
/// <param name="LabelDe">Updated German display label.</param>
/// <param name="LabelEs">Updated Spanish display label.</param>
/// <param name="LabelIt">Updated Italian display label.</param>
/// <param name="LabelPt">Updated Portuguese display label.</param>
/// <param name="SortOrder">Updated display order.</param>
/// <param name="IsActive">Updated active status.</param>
/// <param name="ValidFrom">Updated start of validity period.</param>
/// <param name="ValidTo">Updated end of validity period.</param>
public sealed record ReferenceDataUpdateRequest(
    string LabelEn,
    string LabelFr = "",
    string LabelNl = "",
    string LabelDe = "",
    string LabelEs = "",
    string LabelIt = "",
    string LabelPt = "",
    int SortOrder = 0,
    bool IsActive = true,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null);
