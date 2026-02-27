namespace Granit.Templating.Exceptions;

/// <summary>
/// Exception thrown when no <see cref="Pipeline.ITemplateResolver"/> can locate a template
/// for the requested name and culture.
/// </summary>
public sealed class TemplateNotFoundException : Exception
{
    /// <summary>The logical template name that could not be resolved.</summary>
    public string TemplateName { get; }

    /// <summary>The requested culture (BCP 47), or <c>null</c> if culture-neutral.</summary>
    public string? Culture { get; }

    /// <summary>
    /// Initializes a new <see cref="TemplateNotFoundException"/>.
    /// </summary>
    /// <param name="templateName">The name of the template that was not found.</param>
    /// <param name="culture">The culture that was requested, or <c>null</c>.</param>
    public TemplateNotFoundException(string templateName, string? culture = null)
        : base(BuildMessage(templateName, culture))
    {
        TemplateName = templateName;
        Culture = culture;
    }

    private static string BuildMessage(string templateName, string? culture) =>
        culture is null
            ? $"No template found for '{templateName}'. Ensure an ITemplateResolver is registered and the template is published or embedded."
            : $"No template found for '{templateName}' (culture: '{culture}') and no culture-neutral fallback exists. Ensure the template is published or embedded for this culture.";
}
