namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// AI-assisted semantic mapping service for matching file columns to entity properties.
/// </summary>
/// <remarks>
/// <para>
/// <b>RGPD/ISO 27001 compliance</b>: this interface receives <b>only</b> column header names
/// and <see cref="ImportFieldMetadata"/> (property names, CLR types, display names).
/// It <b>never</b> receives <c>RawImportRow.Values</c> or any business data.
/// </para>
/// <para>
/// Default registration is <see cref="NullSemanticMappingService"/> (null-object pattern).
/// Replace with an AI-backed implementation via <c>services.AddSemanticMappingService&lt;T&gt;()</c>.
/// </para>
/// </remarks>
public interface ISemanticMappingService
{
    /// <summary>
    /// Indicates whether the service is available (i.e. an AI provider is configured).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Suggests column-to-property mappings using semantic analysis of headers and field metadata.
    /// </summary>
    /// <param name="headers">Column headers from the imported file (no row data).</param>
    /// <param name="targetFields">Schema metadata for the target entity (no business data).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of semantic mapping suggestions ordered by confidence score.</returns>
    Task<IReadOnlyList<SemanticMappingSuggestion>> SuggestSemanticMappingsAsync(
        IReadOnlyList<string> headers,
        IReadOnlyList<ImportFieldMetadata> targetFields,
        CancellationToken cancellationToken = default);
}
