namespace Granit.Notifications.Email;

/// <summary>Email message to send.</summary>
public sealed record EmailMessage
{
    /// <summary>Recipient email address.</summary>
    public required string To { get; init; }

    /// <summary>Email subject line.</summary>
    public required string Subject { get; init; }

    /// <summary>HTML body.</summary>
    public required string HtmlBody { get; init; }

    /// <summary>Optional plain text fallback body.</summary>
    public string? PlainTextBody { get; init; }

    /// <summary>Optional sender address override.</summary>
    public string? FromOverride { get; init; }
}
