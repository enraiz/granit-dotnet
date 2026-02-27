using Granit.Templating.Keys;

namespace Granit.DocumentGeneration.Pipeline;

/// <summary>
/// The result of a document generation operation.
/// </summary>
/// <param name="Content">The raw binary content of the generated document.</param>
/// <param name="Format">The format of the generated document.</param>
/// <param name="FileName">Optional suggested file name (without path) for download or storage.</param>
public sealed record DocumentResult(
    ReadOnlyMemory<byte> Content,
    DocumentFormat Format,
    string? FileName = null);
