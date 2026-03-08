using Granit.Templating.Keys;

namespace Granit.DocumentGeneration.Pipeline;

/// <summary>
/// High-level facade for generating binary documents from typed data and templates.
/// </summary>
/// <remarks>
/// The generation pipeline:
/// <list type="number">
///   <item>Enrich <c>TData</c> via registered <c>ITemplateDataEnricher&lt;TData&gt;</c> instances.</item>
///   <item>Resolve the template via the <c>ITemplateResolver</c> chain (culture fallback).</item>
///   <item>Render HTML via the registered <c>ITemplateEngine</c> (e.g. Scriban).</item>
///   <item>Convert HTML to binary via the matching <see cref="IDocumentRenderer"/>.</item>
/// </list>
/// </remarks>
public interface IDocumentGenerator
{
    /// <summary>
    /// Generates a document from the specified template type and data.
    /// </summary>
    /// <typeparam name="TData">The data model type.</typeparam>
    /// <param name="templateType">The document template type descriptor.</param>
    /// <param name="data">The data used to render the template.</param>
    /// <param name="targetFormat">
    /// Override the output format. If <see langword="null"/>, uses
    /// <see cref="DocumentTemplateType{TData}.DefaultFormat"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated document.</returns>
    Task<DocumentResult> GenerateAsync<TData>(
        DocumentTemplateType<TData> templateType,
        TData data,
        DocumentFormat? targetFormat = null,
        CancellationToken cancellationToken = default) where TData : notnull;
}
