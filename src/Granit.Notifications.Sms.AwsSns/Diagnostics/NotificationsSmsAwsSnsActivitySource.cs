using System.Diagnostics;

namespace Granit.Notifications.Sms.AwsSns.Diagnostics;

/// <summary>OpenTelemetry activity source for the AWS SNS SMS provider.</summary>
internal static class NotificationsSmsAwsSnsActivitySource
{
    /// <summary>Activity source name.</summary>
    public const string Name = "Granit.Notifications.Sms.AwsSns";

    /// <summary>Shared activity source instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    /// <summary>Operation names.</summary>
    internal static class Operations
    {
        public const string SendSms = "sns-sms.send";
    }

    /// <summary>Tag names.</summary>
    internal static class Tags
    {
        public const string Recipient = "sns-sms.recipient";
    }
}
