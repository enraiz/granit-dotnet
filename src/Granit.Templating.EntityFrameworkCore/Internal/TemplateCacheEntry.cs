using Granit.Templating.Pipeline;

namespace Granit.Templating.EntityFrameworkCore.Internal;

/// <summary>
/// Cache entry wrapper that allows storing absent (null) templates in <c>HybridCache</c>.
/// </summary>
/// <param name="IsFound">Whether a published template was found in the store.</param>
/// <param name="Content">Template content (null when <paramref name="IsFound"/> is false).</param>
/// <param name="MimeType">Template MIME type (null when <paramref name="IsFound"/> is false).</param>
/// <param name="RevisionId">Revision identifier for ISO 27001 traceability.</param>
internal sealed record TemplateCacheEntry(
    bool IsFound,
    string? Content,
    string? MimeType,
    Guid? RevisionId)
{
    internal static TemplateCacheEntry NotFound { get; } = new(false, null, null, null);

    internal static TemplateCacheEntry From(string content, string mimeType, Guid? revisionId) =>
        new(true, content, mimeType, revisionId);

    internal TemplateDescriptor? ToDescriptor() =>
        IsFound
            ? new TemplateDescriptor { Content = Content!, MimeType = MimeType!, RevisionId = RevisionId }
            : null;
}
