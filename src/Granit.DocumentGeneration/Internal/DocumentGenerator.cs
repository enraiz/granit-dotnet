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

        // 1. Render HTML via the shared text pipeline (enrichers + resolver + engine)
        RenderedTextResult textResult = await _textRenderer.RenderAsync(templateType, data, ct);

        // 2. Find a document renderer that supports the target format
        IDocumentRenderer? renderer = null;
        foreach (IDocumentRenderer candidate in _documentRenderers)
        {
            if (candidate.CanRender(format))
            {
                renderer = candidate;
                break;
            }
        }

        if (renderer is null)
        {
            throw new DocumentRendererNotFoundException(format);
        }

        // 3. Convert HTML → binary document
        return await renderer.RenderAsync(textResult.Html, format, ct);
    }
}
