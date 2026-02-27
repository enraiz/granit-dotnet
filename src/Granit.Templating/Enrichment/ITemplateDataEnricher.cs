namespace Granit.Templating.Enrichment;

/// <summary>
/// Enriches a <typeparamref name="TData"/> instance with computed values before template rendering.
/// </summary>
/// <typeparam name="TData">The data model type. Must be non-null.</typeparam>
/// <remarks>
/// Enrichers are executed in ascending <see cref="Order"/> before the data model is passed to
/// <see cref="Pipeline.ITemplateEngine"/>. They enable injecting computed data (QR codes,
/// remote blobs, aggregated values) without coupling that logic to the domain layer.
/// <para>
/// Implementations <strong>must</strong> return a new immutable instance (use record <c>with</c>
/// expressions) rather than mutating the input. This guarantees that the original
/// <typeparamref name="TData"/> is never modified, which is critical for request-scoped
/// scenarios where the same data is rendered multiple times (e.g. multi-recipient emails).
/// </para>
/// <example>
/// <code>
/// public sealed class PaymentQrCodeEnricher : ITemplateDataEnricher&lt;InvoiceDocumentData&gt;
/// {
///     public int Order => 10;
///
///     public Task&lt;InvoiceDocumentData&gt; EnrichAsync(
///         InvoiceDocumentData data, CancellationToken ct = default)
///     {
///         using QRCodeGenerator generator = new();
///         QRCodeData qrData = generator.CreateQrCode(data.PaymentUrl, QRCodeGenerator.ECCLevel.M);
///         SvgQRCode svg = new(qrData);
///         return Task.FromResult(data with { PaymentQrCodeSvg = svg.GetGraphic(5) });
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface ITemplateDataEnricher<TData> where TData : notnull
{
    /// <summary>
    /// Execution order (ascending). Use multiples of 10 by convention to allow
    /// future insertion without renumbering.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Returns an enriched copy of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The data model to enrich. Never mutate this instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A new <typeparamref name="TData"/> instance with the enriched fields populated.
    /// Return the original <paramref name="data"/> unchanged when no enrichment is needed.
    /// </returns>
    Task<TData> EnrichAsync(TData data, CancellationToken ct = default);
}
