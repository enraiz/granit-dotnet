using Granit.Templating.GlobalContext;
using Granit.Templating.Keys;

namespace Granit.Templating.Pipeline;

/// <summary>
/// Merges a resolved template with a data model to produce <see cref="RenderedContent"/>.
/// </summary>
/// <remarks>
/// The concrete implementation provided by <c>Granit.Templating.Scriban</c> runs
/// Scriban in sandboxed mode (no I/O, no reflection, no network access from templates)
/// — critical for multi-tenant medical SaaS.
/// <para>
/// A second implementation in <c>Granit.DocumentGeneration.Excel</c> (ClosedXML)
/// returns a <see cref="BinaryRenderedContent"/> directly, bypassing any HTML rendering.
/// </para>
/// </remarks>
public interface ITemplateEngine
{
    /// <summary>
    /// Returns <c>true</c> when this engine can process the given descriptor.
    /// </summary>
    /// <remarks>
    /// Typically checks <see cref="TemplateDescriptor.MimeType"/>
    /// (e.g. <c>"text/html"</c> for Scriban, <c>"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"</c> for ClosedXML).
    /// </remarks>
    bool CanRender(TemplateDescriptor descriptor);

    /// <summary>
    /// Renders the template with the supplied data and global contexts.
    /// </summary>
    /// <typeparam name="TData">Type of the data model. Must be non-null.</typeparam>
    /// <param name="descriptor">Resolved template descriptor (source + MIME type + revision).</param>
    /// <param name="data">Data model to merge into the template.</param>
    /// <param name="targetFormat">
    /// Final output format requested by the caller. Embedded into <see cref="TextRenderedContent"/>
    /// so downstream renderers can select the correct implementation.
    /// </param>
    /// <param name="globalContexts">
    /// Ambient context objects injected into every template (date/time, tenant, culture, …).
    /// Each context is exposed under its <see cref="ITemplateGlobalContext.Namespace"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TextRenderedContent"/> for Scriban/HTML engines or a
    /// <see cref="BinaryRenderedContent"/> for native binary engines (ClosedXML).
    /// </returns>
    Task<RenderedContent> RenderAsync<TData>(
        TemplateDescriptor descriptor,
        TData data,
        DocumentFormat targetFormat,
        IReadOnlyList<ITemplateGlobalContext> globalContexts,
        CancellationToken cancellationToken = default) where TData : notnull;
}
