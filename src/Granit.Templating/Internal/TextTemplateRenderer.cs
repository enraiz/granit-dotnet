using System.Globalization;
using Granit.Templating.Enrichment;
using Granit.Templating.Exceptions;
using Granit.Templating.GlobalContext;
using Granit.Templating.Keys;
using Granit.Templating.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Templating.Internal;

/// <summary>
/// Internal implementation of <see cref="ITextTemplateRenderer"/>.
/// Orchestrates the full text rendering pipeline:
/// enrichment → resolution (with culture fallback) → engine render.
/// </summary>
internal sealed class TextTemplateRenderer(
    IEnumerable<ITemplateResolver> resolvers,
    IEnumerable<ITemplateEngine> engines,
    IEnumerable<ITemplateGlobalContext> globalContexts,
    IServiceProvider serviceProvider) : ITextTemplateRenderer
{
    private readonly IOrderedEnumerable<ITemplateResolver> _resolvers =
        resolvers.OrderByDescending(r => r.Priority);

    private readonly IReadOnlyList<ITemplateEngine> _engines = engines.ToList();

    private readonly IReadOnlyList<ITemplateGlobalContext> _globalContexts =
        globalContexts.ToList();

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc/>
    public async Task<RenderedTextResult> RenderAsync<TData>(
        TextTemplateType<TData> templateType,
        TData data,
        CancellationToken ct = default) where TData : notnull
    {
        RenderedContent rendered = await RenderCoreAsync(templateType, data, DocumentFormat.Html, ct);

        if (rendered is not TextRenderedContent text)
        {
            throw new InvalidOperationException(
                $"ITemplateEngine returned {rendered.GetType().Name} for a text template. " +
                "Ensure the registered ITemplateEngine supports 'text/html' templates.");
        }

        return new RenderedTextResult(text.Html);
    }

    /// <inheritdoc/>
    public Task<RenderedContent> RenderDocumentAsync<TData>(
        TextTemplateType<TData> templateType,
        TData data,
        DocumentFormat targetFormat,
        CancellationToken ct = default) where TData : notnull =>
        RenderCoreAsync(templateType, data, targetFormat, ct);

    private async Task<RenderedContent> RenderCoreAsync<TData>(
        TextTemplateType<TData> templateType,
        TData data,
        DocumentFormat targetFormat,
        CancellationToken ct) where TData : notnull
    {
        // 1. Enrich data (ordered, immutable)
        TData enrichedData = await EnrichAsync(data, ct);

        // 2. Resolve template (culture-specific, then neutral fallback)
        string culture = CultureInfo.CurrentCulture.Name;
        TemplateDescriptor descriptor =
            await ResolveAsync(templateType.Name, culture, ct)
            ?? throw new TemplateNotFoundException(templateType.Name, culture);

        // 3. Select engine by MIME type and render
        ITemplateEngine? engine = _engines.FirstOrDefault(e => e.CanRender(descriptor));

        if (engine is null)
        {
            throw new InvalidOperationException(
                $"No ITemplateEngine can render MIME type '{descriptor.MimeType}'. " +
                "Register a compatible engine (e.g. AddGranitTemplatingWithScriban or AddGranitDocumentGenerationExcel).");
        }

        return await engine.RenderAsync(descriptor, enrichedData, targetFormat, _globalContexts, ct);
    }

    private async Task<TData> EnrichAsync<TData>(TData data, CancellationToken ct)
        where TData : notnull
    {
        IEnumerable<ITemplateDataEnricher<TData>> enrichers =
            _serviceProvider.GetServices<ITemplateDataEnricher<TData>>();

        TData current = data;
        foreach (ITemplateDataEnricher<TData> enricher in enrichers.OrderBy(e => e.Order))
        {
            current = await enricher.EnrichAsync(current, ct);
        }

        return current;
    }

    private async Task<TemplateDescriptor?> ResolveAsync(
        string name, string culture, CancellationToken ct)
    {
        // Culture-specific pass
        TemplateKey culturalKey = new(name, culture);
        foreach (ITemplateResolver resolver in _resolvers)
        {
            TemplateDescriptor? descriptor = await resolver.TryResolveAsync(culturalKey, ct);
            if (descriptor is not null)
            {
                return descriptor;
            }
        }

        // Culture-neutral fallback
        TemplateKey neutralKey = new(name);
        foreach (ITemplateResolver resolver in _resolvers)
        {
            TemplateDescriptor? descriptor = await resolver.TryResolveAsync(neutralKey, ct);
            if (descriptor is not null)
            {
                return descriptor;
            }
        }

        return null;
    }
}
