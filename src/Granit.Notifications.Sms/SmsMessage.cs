namespace Granit.Notifications.Sms;

/// <summary>SMS message to send.</summary>
public sealed record SmsMessage
{
    /// <summary>Recipient phone number in E.164 format.</summary>
    public required string To { get; init; }

    /// <summary>SMS body text.</summary>
    public required string Body { get; init; }

    /// <summary>Optional sender ID displayed on the phone.</summary>
    public string? SenderId { get; init; }
}
