namespace Granit.Notifications.Zulip;

/// <summary>Zulip message to send.</summary>
public sealed record ZulipMessage
{
    /// <summary>Message type: "stream" for channel messages, "direct" for DMs.</summary>
    public required string Type { get; init; }

    /// <summary>Target stream name (required when <see cref="Type"/> is "stream").</summary>
    public string? Stream { get; init; }

    /// <summary>Topic within the stream (required when <see cref="Type"/> is "stream").</summary>
    public string? Topic { get; init; }

    /// <summary>Recipient email addresses (required when <see cref="Type"/> is "direct").</summary>
    public IReadOnlyList<string>? To { get; init; }

    /// <summary>Message content in Markdown format.</summary>
    public required string Content { get; init; }
}
