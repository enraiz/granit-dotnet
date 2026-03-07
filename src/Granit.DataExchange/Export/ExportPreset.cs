namespace Granit.DataExchange.Export;

/// <summary>
/// A saved export configuration preset (Odoo export template pattern).
/// Stores the user's field selection and format preference for reuse.
/// </summary>
/// <param name="DefinitionName">The export definition name (e.g. <c>"Acme.PatientExport"</c>).</param>
/// <param name="PresetName">User-facing preset name (e.g. <c>"Export mensuel"</c>).</param>
/// <param name="SelectedFields">Ordered list of field property paths to include.</param>
/// <param name="Format">Output format (<c>"xlsx"</c> or <c>"csv"</c>).</param>
/// <param name="IncludeIdForImport">Whether to include the entity ID for roundtrip import.</param>
public sealed record ExportPreset(
    string DefinitionName,
    string PresetName,
    IReadOnlyList<string> SelectedFields,
    string Format,
    bool IncludeIdForImport);
