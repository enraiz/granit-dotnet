namespace Granit.ReferenceData.Endpoints.Dtos;

/// <summary>
/// Query string parameters for the GET reference data list endpoint.
/// </summary>
internal sealed record ReferenceDataQueryParameters(
    bool ActiveOnly = true,
    string? Search = null,
    string? SortBy = null,
    bool Descending = false,
    int? Skip = null,
    int? Take = null);
