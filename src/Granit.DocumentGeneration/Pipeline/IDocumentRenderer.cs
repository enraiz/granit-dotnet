using Granit.Templating.Keys;

namespace Granit.DocumentGeneration.Pipeline;

/// <summary>
/// Converts rendered HTML into a binary document of the requested format.
/// </summary>
/// <remarks>
/// Implement this interface to add support for a new output format.
/// Register the implementation via <see cref="ServiceCollectionExtensions.AddDocumentRenderer{TRenderer}"/>.
/// <para>
/// Built-in implementations are provided in separate packages:
/// <list type="bullet">
///   <item><c>Granit.DocumentGeneration.Pdf</c> — PuppeteerSharp (headless Chromium)</item>
///   <item><c>Granit.DocumentGeneration.Excel</c> — ClosedXML (Excel generation from HTML tables)</item>
/// </list>
/// </para>
/// </remarks>
public interface IDocumentRenderer
{
    /// <summary>
    /// Returns <see langword="true"/> if this renderer can produce the requested format.
    /// </summary>
    bool CanRender(DocumentFormat targetFormat);

    /// <summary>
    /// Converts the given <paramref name="html"/> into a binary document.
    /// </summary>
    /// <param name="html">The rendered HTML content to convert.</param>
    /// <param name="targetFormat">The desired output format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="DocumentResult"/> containing the binary content and format.</returns>
    Task<DocumentResult> RenderAsync(
        string html,
        DocumentFormat targetFormat,
        CancellationToken ct = default);
}
