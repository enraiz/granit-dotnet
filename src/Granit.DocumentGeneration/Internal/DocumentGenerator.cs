using Granit.DocumentGeneration.Exceptions;
using Granit.DocumentGeneration.Pipeline;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;

namespace Granit.DocumentGeneration.Internal;

/// <summary>
/// Internal implementation of <see cref="IDocumentGenerator"/>.
/// Coordinates the text rendering pipeline with the binary conversion step.
/// </summary>
internal sealed class DocumentGenerator(
    ITextTemplateRenderer textRenderer,
    IEnumerable<IDocumentRenderer> documentRenderers) : IDocumentGenerator
{
    private readonly ITextTemplateRenderer _textRenderer = textRenderer;
    private readonly IEnumerable<IDocumentRenderer> _documentRenderers = documentRenderers;

    /// <inheritdoc/>
    public async Task<DocumentResult> GenerateAsync<TData>(
        DocumentTemplateType<TData> templateType,
        TData data,
        DocumentFormat? targetFormat = null,
        CancellationToken ct = default) where TData : notnull
    {
        DocumentFormat format = targetFormat ?? templateType.DefaultFormat;

        // 1. Render via the shared text pipeline (enrichers + resolver + engine).
        //    Binary engines (e.g. ClosedXML for Excel) return BinaryRenderedContent directly.
        RenderedContent content = await _textRenderer.RenderDocumentAsync(templateType, data, format, ct);

        // 2a. Binary engine result: return directly, no IDocumentRenderer step needed.
        if (content is BinaryRenderedContent binary)
        {
            return new DocumentResult(binary.Bytes, binary.Format);
        }

        // 2b. Text engine result: find a renderer that converts HTML → target format.
        TextRenderedContent text = (TextRenderedContent)content;
        IDocumentRenderer? renderer = _documentRenderers.FirstOrDefault(r => r.CanRender(format));

        if (renderer is null)
        {
            throw new DocumentRendererNotFoundException(format);
        }

        // 3. Convert HTML → binary document.
        return await renderer.RenderAsync(text.Html, format, ct);
    }
}
