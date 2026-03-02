namespace Granit.DataImport.Mapping;

/// <summary>
/// Null-object implementation of <see cref="ISemanticMappingService"/>.
/// Always returns an empty list and reports as unavailable.
/// </summary>
/// <remarks>
/// Registered by default via <c>TryAddSingleton</c>. When an AI provider is configured,
/// the host application replaces this with a real implementation using
/// <c>services.AddSemanticMappingService&lt;T&gt;()</c>.
/// </remarks>
internal sealed class NullSemanticMappingService : ISemanticMappingService
{
    /// <inheritdoc/>
    public bool IsAvailable => false;

    /// <inheritdoc/>
    public Task<IReadOnlyList<SemanticMappingSuggestion>> SuggestSemanticMappingsAsync(
        IReadOnlyList<string> headers,
        IReadOnlyList<FieldMetadata> targetFields,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SemanticMappingSuggestion>>([]);
}
