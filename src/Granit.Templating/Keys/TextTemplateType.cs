namespace Granit.Templating.Keys;

/// <summary>
/// Marker base class for templates whose output is text (HTML, plain text, subject line).
/// </summary>
/// <typeparam name="TData">The data model merged with the template.</typeparam>
/// <remarks>
/// Use this class for templates rendered via <see cref="Pipeline.ITextTemplateRenderer"/>
/// — typically email bodies, SMS text, push notification payloads, etc.
/// <para>
/// For binary document generation (PDF, Excel), use <c>DocumentTemplateType&lt;TData&gt;</c>
/// from <c>Granit.DocumentGeneration</c>.
/// </para>
/// </remarks>
public abstract class TextTemplateType<TData> : TemplateType<TData> where TData : notnull;
