namespace Granit.Notifications.WhatsApp;

/// <summary>WhatsApp Business API message (template-based).</summary>
public sealed record WhatsAppMessage
{
    /// <summary>Recipient phone number in E.164 format.</summary>
    public required string To { get; init; }

    /// <summary>Meta-approved template name.</summary>
    public required string TemplateName { get; init; }

    /// <summary>Template parameter values.</summary>
    public IReadOnlyList<string> TemplateParameters { get; init; } = [];

    /// <summary>Message language (BCP 47, e.g. "fr").</summary>
    public string? Language { get; init; }
}
