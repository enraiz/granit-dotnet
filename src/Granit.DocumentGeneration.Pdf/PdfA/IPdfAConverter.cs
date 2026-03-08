using Granit.DocumentGeneration.Pipeline;

namespace Granit.DocumentGeneration.Pdf.PdfA;

/// <summary>
/// Converts a standard PDF document into a PDF/A compliant document.
/// </summary>
/// <remarks>
/// <para>
/// PDF/A-3b is required for:
/// <list type="bullet">
///   <item>Factur-X electronic invoicing (NF Z 55-140) — mandatory in France from September 2026</item>
///   <item>Long-term archival of medical documents (HDS compliance)</item>
/// </list>
/// </para>
/// <para>
/// Implementations may use libraries such as iText7 or similar tools capable of
/// PDF/A conversion with XMP metadata and embedded attachments.
/// </para>
/// </remarks>
public interface IPdfAConverter
{
    /// <summary>
    /// Converts a standard PDF into a PDF/A-3b compliant document.
    /// </summary>
    /// <param name="pdfResult">The source PDF document to convert.</param>
    /// <param name="options">PDF/A conversion options (attachments, conformance level).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DocumentResult"/> containing the PDF/A-3b compliant bytes.</returns>
    Task<DocumentResult> ConvertToPdfAAsync(
        DocumentResult pdfResult,
        PdfAConversionOptions options,
        CancellationToken cancellationToken = default);
}
