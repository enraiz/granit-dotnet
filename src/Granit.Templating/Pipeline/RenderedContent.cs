using Granit.Templating.Keys;

namespace Granit.Templating.Pipeline;

/// <summary>
/// Discriminated union representing the output of <see cref="ITemplateEngine.RenderAsync{TData}"/>.
/// </summary>
/// <remarks>
/// Two concrete variants:
/// <list type="bullet">
///   <item>
///     <see cref="TextRenderedContent"/> — HTML text produced by a markup engine (Scriban).
///     A downstream binary renderer (PDF, …) converts it to the final format.
///   </item>
///   <item>
///     <see cref="BinaryRenderedContent"/> — Raw bytes produced directly by a native engine
///     (e.g. ClosedXML for Excel). No additional rendering step required.
///   </item>
/// </list>
/// </remarks>
public abstract record RenderedContent
{
    /// <summary>
    /// Identifier of the template revision that produced this content.
    /// Propagated from <see cref="TemplateDescriptor.RevisionId"/> for HDS traceability.
    /// </summary>
    public Guid? RevisionId { get; init; }
}

/// <summary>
/// HTML text output from a markup template engine (e.g. Scriban).
/// Consumed by a binary renderer (<c>IDocumentRenderer</c>) that converts it to
/// the <see cref="TargetFormat"/>.
/// </summary>
/// <param name="Html">The rendered HTML string.</param>
/// <param name="TargetFormat">The final output format requested by the caller.</param>
public sealed record TextRenderedContent(string Html, DocumentFormat TargetFormat) : RenderedContent;

/// <summary>
/// Binary output produced directly by a native template engine (e.g. ClosedXML for Excel).
/// Requires no additional rendering step.
/// </summary>
/// <param name="Bytes">The raw document bytes.</param>
/// <param name="Format">The format of the bytes.</param>
public sealed record BinaryRenderedContent(ReadOnlyMemory<byte> Bytes, DocumentFormat Format) : RenderedContent;
