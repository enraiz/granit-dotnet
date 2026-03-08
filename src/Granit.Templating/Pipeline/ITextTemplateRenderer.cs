using Granit.Templating.Keys;

namespace Granit.Templating.Pipeline;

/// <summary>
/// Facade for rendering text-based templates (email, SMS, push notifications).
/// </summary>
/// <remarks>
/// Orchestrates the full pipeline for text output:
/// <list type="number">
///   <item>Data enrichment via <c>ITemplateDataEnricher&lt;TData&gt;</c> (ordered, immutable)</item>
///   <item>Template resolution via <c>ITemplateResolver</c> chain (culture fallback)</item>
///   <item>Rendering via <c>ITemplateEngine</c> (Scriban, sandboxed)</item>
/// </list>
/// Culture is read from <see cref="System.Globalization.CultureInfo.CurrentCulture"/>
/// automatically — no need to pass it explicitly.
/// <para>
/// For binary document generation (PDF, Excel), use <c>IDocumentGenerator</c>
/// from <c>Granit.DocumentGeneration</c>.
/// </para>
/// <example>
/// <code>
/// RenderedTextResult result = await renderer.RenderAsync(
///     AcmeTemplates.WelcomeEmail, new WelcomeEmailData { PatientName = "Dupont" });
///
/// await emailSender.SendAsync(to: patient.Email, subject: result.Subject, html: result.Html);
/// </code>
/// </example>
/// </remarks>
public interface ITextTemplateRenderer
{
    /// <summary>
    /// Renders the template associated with <paramref name="templateType"/> using the supplied data.
    /// </summary>
    /// <typeparam name="TData">Type of the data model. Must be non-null.</typeparam>
    /// <param name="templateType">Strongly-typed template declaration.</param>
    /// <param name="data">Data model to merge into the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="RenderedTextResult"/> containing the HTML body, optional plain text,
    /// and optional subject line.
    /// </returns>
    /// <exception cref="Exceptions.TemplateNotFoundException">
    /// Thrown when no resolver returns a template for the given key and culture.
    /// </exception>
    Task<RenderedTextResult> RenderAsync<TData>(
        TextTemplateType<TData> templateType,
        TData data,
        CancellationToken cancellationToken = default) where TData : notnull;

    /// <summary>
    /// Renders the template, returning a <see cref="RenderedContent"/> that may be text or binary.
    /// </summary>
    /// <remarks>
    /// Used by <c>IDocumentGenerator</c> to support binary engines (e.g. ClosedXML for Excel)
    /// that produce <see cref="BinaryRenderedContent"/> directly, bypassing the HTML→binary step.
    /// </remarks>
    /// <typeparam name="TData">Type of the data model. Must be non-null.</typeparam>
    /// <param name="templateType">Strongly-typed template declaration.</param>
    /// <param name="data">Data model to merge into the template.</param>
    /// <param name="targetFormat">The intended output format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RenderedContent> RenderDocumentAsync<TData>(
        TextTemplateType<TData> templateType,
        TData data,
        DocumentFormat targetFormat,
        CancellationToken cancellationToken = default) where TData : notnull;
}
