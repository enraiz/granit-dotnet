namespace Granit.Templating.Pipeline;

/// <summary>
/// Holds the resolved template source returned by an <see cref="ITemplateResolver"/>.
/// </summary>
/// <remarks>
/// Passed as-is to <see cref="ITemplateEngine.RenderAsync{TData}"/> for merging with data.
/// The <see cref="RevisionId"/> is propagated through the pipeline into the final output
/// to satisfy the ISO 27001 audit trail requirement (template traceability).
/// </remarks>
public sealed class TemplateDescriptor
{
    /// <summary>Raw template source (typically HTML with Scriban expressions).</summary>
    public required string Content { get; init; }

    /// <summary>
    /// MIME type of the template source (e.g. <c>"text/html"</c>).
    /// Used by <see cref="ITemplateEngine.CanRender"/> to select the right engine.
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// Identifier of the published revision this descriptor was resolved from.
    /// <c>null</c> for embedded (code-level) templates with no revision tracking.
    /// </summary>
    public Guid? RevisionId { get; init; }
}
