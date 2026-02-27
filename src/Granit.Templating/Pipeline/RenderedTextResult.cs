namespace Granit.Templating.Pipeline;

/// <summary>
/// Result of <see cref="ITextTemplateRenderer.RenderAsync{TData}"/> for text-based output
/// (email, SMS, push notification).
/// </summary>
/// <param name="Html">
/// Full HTML body. Always present — serves as the rich-text version for email clients
/// that support HTML.
/// </param>
/// <param name="PlainText">
/// Optional plain-text fallback. If <c>null</c>, callers may strip HTML tags from
/// <paramref name="Html"/> as a fallback.
/// </param>
/// <param name="Subject">
/// Optional subject line or notification title, rendered from the same template.
/// </param>
public sealed record RenderedTextResult(
    string Html,
    string? PlainText = null,
    string? Subject = null);
