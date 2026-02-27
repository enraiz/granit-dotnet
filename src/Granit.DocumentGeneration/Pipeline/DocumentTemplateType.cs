using Granit.Templating.Keys;

namespace Granit.DocumentGeneration.Pipeline;

/// <summary>
/// Base class for document template types (PDF, Excel, etc.).
/// </summary>
/// <remarks>
/// Document templates use HTML as their source format (rendered by <c>ITextTemplateRenderer</c>)
/// and then convert the HTML output to the requested binary format via an <c>IDocumentRenderer</c>.
/// <para>
/// Extend this class to define a strongly-typed document template:
/// <code>
/// public sealed class InvoiceTemplateType : DocumentTemplateType&lt;InvoiceData&gt;
/// {
///     public override string Name => "Billing.Invoice";
///     public override DocumentFormat DefaultFormat => DocumentFormat.Pdf;
/// }
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TData">The data model type used to render the template.</typeparam>
public abstract class DocumentTemplateType<TData> : TextTemplateType<TData>
    where TData : notnull
{
    /// <summary>
    /// Default output format when no format is specified per call.
    /// </summary>
    public virtual DocumentFormat DefaultFormat => DocumentFormat.Pdf;
}
